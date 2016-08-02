using System.Collections.Generic;
using Verse;
using RimWorld;

namespace BuildProductive
{
    public struct BuildingProperties
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
        private List<BuildingProperties> _watchList = new List<BuildingProperties>();

        public override void Tick()
        {
            for (int i = 0; i < _watchList.Count; i++)
            {
                var p = _watchList[i];

                Blueprint_Build blueprint = null;
                Frame frame = null;
                Building building = null;
                
                foreach (var thing in Find.ThingGrid.ThingsAt(p.Location))
                {
                    if (thing.def == p.Def.blueprintDef) blueprint = thing as Blueprint_Build;
                    else if (thing.def == p.Def.frameDef) frame = thing as Frame;
                    else if (thing.def == p.Def) building = thing as Building;
                }

                if (building != null)
                {
                    UnwrapProperties(building, p);
                    building = null;
                }

                if (blueprint == null && frame == null && building == null)
                {
                    _watchList.Remove(p);
                    i--;
                }
            }
        }

        public void TryAddBuilding(IntVec3 loc, Building building)
        {
            var p = new BuildingProperties { Location = loc, Def = building.def };

            if (WrapProperties(building, ref p))
            {
                _watchList.Add(p);
            }
        }

        public bool WrapProperties(Building building, ref BuildingProperties p)
        {
            bool hasProperties = false;

            var flickable = building.GetComp<CompFlickable>();
            if (flickable != null)
            {
                p.WantSwitchOn = (bool)Bootstrapper.WantSwitchOn.GetValue(flickable);
                hasProperties = true;
            }

            var tempControl = building.GetComp<CompTempControl>();
            if (tempControl != null)
            {
                p.TargetTemperature = tempControl.targetTemperature;
                hasProperties = true;
            }

            var rearmable = building as Building_TrapRearmable;
            if (rearmable != null)
            {
                p.AutoRearm = (bool)Bootstrapper.AutoRearmField.GetValue(rearmable);
                hasProperties = true;
            }

            var turret = building as Building_TurretGun;
            if (turret != null)
            {
                p.HoldFire = (bool)Bootstrapper.HoldFireField.GetValue(turret);
                hasProperties = true;
            }

            return hasProperties;
        }

        public void UnwrapProperties(Building building, BuildingProperties p)
        {
            var flickable = building.GetComp<CompFlickable>();
            if (flickable != null)
            {
                Bootstrapper.WantSwitchOn.SetValue(flickable, p.WantSwitchOn);
                FlickUtility.UpdateFlickDesignation(building);
            }

            var tempControl = building.GetComp<CompTempControl>();
            if (tempControl != null)
            {
               tempControl.targetTemperature = p.TargetTemperature;
            }

            var rearmable = building as Building_TrapRearmable;
            if (rearmable != null)
            {
                Bootstrapper.AutoRearmField.SetValue(rearmable, p.AutoRearm);
            }

            var turret = building as Building_TurretGun;
            if (turret != null)
            {
                Bootstrapper.HoldFireField.SetValue(turret, p.HoldFire);
            }
        }
    }
}
