using System.Collections.Generic;
using Verse;
using RimWorld;

namespace BuildProductive
{
    public struct BuildingChecklist
    {
        public IntVec3 Location;
        public BuildableDef Def;

        public bool WantSwitchOn;
        public float TargetTemperature;
        public bool HoldFire;
        public bool AutoRearm;
    }

    class BuildingWatchdog : Thing
    {
        private List<BuildingChecklist> _watchList = new List<BuildingChecklist>();

        public override void Tick()
        {
            for (int i = 0; i < _watchList.Count; i++)
            {
                var p = _watchList[i];
                var isBeingBuilt = false;

                foreach (var thing in Find.ThingGrid.ThingsAt(p.Location))
                {
                    // If there's an unfinished building in the cell, keep waiting
                    if (thing.def == p.Def.blueprintDef || thing.def == p.Def.frameDef)
                    {
                        isBeingBuilt = true;
                        break;
                    }
                    // Otherwise apply settings and stop watching
                    else if (thing.def == p.Def)
                    {
                        UnwrapChecklist(thing as Building, p);
                        break;
                    }
                }

                if (!isBeingBuilt)
                {
                    _watchList.RemoveAt(i);
                    i--;
                }
            }
        }

        public void TryAddBuilding(IntVec3 loc, Building building)
        {
            var p = new BuildingChecklist { Location = loc, Def = building.def };

            if (WrapChecklist(building, ref p))
            {
                _watchList.Add(p);
            }
        }

        public bool WrapChecklist(Building building, ref BuildingChecklist p)
        {
            bool hasSettings = false;

            // Designate power switch
            var flickable = building.GetComp<CompFlickable>();
            if (flickable != null)
            {
                p.WantSwitchOn = (bool)Bootstrapper.WantSwitchOn.GetValue(flickable);
                hasSettings = true;
            }

            // Target temperature
            var tempControl = building.GetComp<CompTempControl>();
            if (tempControl != null)
            {
                p.TargetTemperature = tempControl.targetTemperature;
                hasSettings = true;
            }

            // Auto rearm
            var rearmable = building as Building_TrapRearmable;
            if (rearmable != null)
            {
                p.AutoRearm = (bool)Bootstrapper.AutoRearmField.GetValue(rearmable);
                hasSettings = true;
            }

            // Hold fire
            var turret = building as Building_TurretGun;
            if (turret != null)
            {
                p.HoldFire = (bool)Bootstrapper.HoldFireField.GetValue(turret);
                hasSettings = true;
            }

            return hasSettings;
        }

        public void UnwrapChecklist(Building building, BuildingChecklist p)
        {
            // Designate power switch
            var flickable = building.GetComp<CompFlickable>();
            // Check if we need switching it off to avoid unnecessary tutorial prompt
            if (!p.WantSwitchOn && flickable != null)
            {
                Bootstrapper.WantSwitchOn.SetValue(flickable, p.WantSwitchOn);
                FlickUtility.UpdateFlickDesignation(building);
            }

            // Target temperature
            var tempControl = building.GetComp<CompTempControl>();
            if (tempControl != null)
            {
               tempControl.targetTemperature = p.TargetTemperature;
            }

            // Auto rearm
            var rearmable = building as Building_TrapRearmable;
            if (rearmable != null)
            {
                Bootstrapper.AutoRearmField.SetValue(rearmable, p.AutoRearm);
            }

            // Hold fire
            var turret = building as Building_TurretGun;
            if (turret != null)
            {
                Bootstrapper.HoldFireField.SetValue(turret, p.HoldFire);
            }
        }
    }
}
