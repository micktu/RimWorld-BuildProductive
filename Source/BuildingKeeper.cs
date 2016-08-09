using System.Collections.Generic;
using Verse;
using RimWorld;

namespace BuildProductive
{
    public class BuildingKeeper
    {
        public struct BuildingInfo
        {
            public bool WantSwitchOn;
            public float TargetTemperature;
            public bool HoldFire;
            public bool AutoRearm;
        }

        public Dictionary<int, BuildingInfo> _blueprints { get; private set; }
        public Dictionary<int, BuildingInfo> _frames { get; private set; }

        public BuildingKeeper()
        {
            _blueprints = new Dictionary<int, BuildingInfo>();
            _frames = new Dictionary<int, BuildingInfo>();
        }

        public void RegisterBlueprint(Building building, Blueprint_Build blueprint)
        {
            BuildingInfo bi;
            if (WrapInfo(building, out bi))
            {
                _blueprints[blueprint.thingIDNumber] = bi;
                Log.Message("Blueprint " + blueprint.thingIDNumber);
            }
        }

        public void RegisterBlueprint(Frame frame, Blueprint_Build blueprint)
        {
            Log.Message("Reregister blueprint");
            var id = frame.thingIDNumber;

            BuildingInfo bi;
            if (_frames.TryGetValue(id, out bi))
            {
                _blueprints[blueprint.thingIDNumber] = bi;
                _frames.Remove(id);
            }
        }

        public void UnregisterBlueprint(Blueprint blueprint)
        {
            Log.Message("Unregister blueprint");
            _blueprints.Remove(blueprint.thingIDNumber);
        }

        public void RegisterFrame(Blueprint_Build blueprint, Frame frame)
        {
            var id = blueprint.thingIDNumber;

            BuildingInfo bi;
            if (_blueprints.TryGetValue(id, out bi))
            {
                Log.Message("Blueprint " + id + " frame " + frame.thingIDNumber);
                _frames[frame.thingIDNumber] = bi;
                _blueprints.Remove(id);
            }
        }

        public void UnregisterFrame(Frame frame)
        {
            Log.Message("Unregister frame");
            _frames.Remove(frame.thingIDNumber);
        }

        public void RegisterBuilding(Frame frame, Building building)
        {
            var id = frame.thingIDNumber;

            BuildingInfo bi;
            if (_frames.TryGetValue(id, out bi))
            {
                Log.Message("frame " + id);
                UnwrapInfo(building, bi);
                _frames.Remove(id);
            }
        }

        public bool WrapInfo(Building building, out BuildingInfo bi)
        {
            var hasSettings = false;
            bi = new BuildingInfo();

            // Designate power switch
            var flickable = building.GetComp<CompFlickable>();
            if (flickable != null)
            {
                bi.WantSwitchOn = (bool)Bootstrapper.WantSwitchOn.GetValue(flickable);
                hasSettings = true;
            }

            // Target temperature
            var tempControl = building.GetComp<CompTempControl>();
            if (tempControl != null)
            {
                bi.TargetTemperature = tempControl.targetTemperature;
                hasSettings = true;
            }

            // Auto rearm
            var rearmable = building as Building_TrapRearmable;
            if (rearmable != null)
            {
                bi.AutoRearm = (bool)Bootstrapper.AutoRearmField.GetValue(rearmable);
                hasSettings = true;
            }

            // Hold fire
            var turret = building as Building_TurretGun;
            if (turret != null)
            {
                bi.HoldFire = (bool)Bootstrapper.HoldFireField.GetValue(turret);
                hasSettings = true;
            }

            return hasSettings;
        }

        public void UnwrapInfo(Building building, BuildingInfo bi)
        {
            // Designate power switch
            var flickable = building.GetComp<CompFlickable>();
            // Check if we need switching it off to avoid unnecessary tutorial prompt
            if (!bi.WantSwitchOn && flickable != null)
            {
                Bootstrapper.WantSwitchOn.SetValue(flickable, bi.WantSwitchOn);
                FlickUtility.UpdateFlickDesignation(building);
            }

            // Target temperature
            var tempControl = building.GetComp<CompTempControl>();
            if (tempControl != null)
            {
                tempControl.targetTemperature = bi.TargetTemperature;
            }

            // Auto rearm
            var rearmable = building as Building_TrapRearmable;
            if (rearmable != null)
            {
                Bootstrapper.AutoRearmField.SetValue(rearmable, bi.AutoRearm);
            }

            // Hold fire
            var turret = building as Building_TurretGun;
            if (turret != null)
            {
                Bootstrapper.HoldFireField.SetValue(turret, bi.HoldFire);
            }
        }

    }
}
