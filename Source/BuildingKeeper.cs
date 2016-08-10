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

        public bool RegisterBlueprint(Thing thing, Blueprint_Build blueprint, bool isCopy = false)
        {
            BuildingInfo bi = new BuildingInfo();
            var id = thing.thingIDNumber;
            var isFound = false;

            if (thing is Frame && _frames.TryGetValue(id, out bi))
            {
                isFound = true;
                if (!isCopy) _frames.Remove(id);
            }
            else if (thing is Building && WrapInfo(thing as Building, out bi))
            {
                isCopy = true;
                isFound = true;
            }
            else if (thing is Blueprint_Build && _blueprints.TryGetValue(id, out bi))
            {
                isFound = true;
                if (!isCopy) _blueprints.Remove(id);
            }

            if (isFound)
            {
                _blueprints[blueprint.thingIDNumber] = bi;
                LogTransfer(thing, blueprint, isCopy);
                return true;
            }

            return false;
        }

        public void UnregisterBlueprint(Blueprint_Build blueprint)
        {
            if (_blueprints.Remove(blueprint.thingIDNumber))
            {
                LogUnregister(blueprint);
            }
        }

        public void RegisterFrame(Blueprint_Build blueprint, Frame frame)
        {
            var id = blueprint.thingIDNumber;

            BuildingInfo bi;
            if (_blueprints.TryGetValue(id, out bi))
            {
                _frames[frame.thingIDNumber] = bi;
                _blueprints.Remove(id);
                LogTransfer(blueprint, frame);
            }
        }

        public void UnregisterFrame(Frame frame)
        {
            if (_frames.Remove(frame.thingIDNumber))
            {
                LogUnregister(frame);
            }
        }

        public void RegisterBuilding(Frame frame, Building building)
        {
            var id = frame.thingIDNumber;

            BuildingInfo bi;
            if (_frames.TryGetValue(id, out bi))
            {
                UnwrapInfo(building, bi);
                _frames.Remove(id);
                LogTransfer(frame, building);
            }
        }

        public void RegisterBuilding(Building source, Building destination)
        {
            BuildingInfo bi;
            if (WrapInfo(source, out bi))
            {
                UnwrapInfo(destination, bi);
                LogTransfer(source, destination, true);
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
                bi.WantSwitchOn = (bool)Privates.WantSwitchOn.GetValue(flickable);
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
                bi.AutoRearm = (bool)Privates.AutoRearmField.GetValue(rearmable);
                hasSettings = true;
            }

            // Hold fire
            var turret = building as Building_TurretGun;
            if (turret != null)
            {
                bi.HoldFire = (bool)Privates.HoldFireField.GetValue(turret);
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
                Privates.WantSwitchOn.SetValue(flickable, bi.WantSwitchOn);
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
                Privates.AutoRearmField.SetValue(rearmable, bi.AutoRearm);
            }

            // Hold fire
            var turret = building as Building_TurretGun;
            if (turret != null)
            {
                Privates.HoldFireField.SetValue(turret, bi.HoldFire);
            }
        }

        private void LogTransfer(Thing source, Thing destination, bool isCopy = false)
        {
            Globals.Logger.Debug("{0} from {1} to {2}", isCopy ? "Copied" : "Transferred", source.ThingID, destination.ThingID);
        }

        private void LogRegister(Thing thing)
        {
            Globals.Logger.Debug("Registered {0}", thing.ThingID);
        }

        private void LogUnregister(Thing thing)
        {
            Globals.Logger.Debug("Unregistered {0}", thing.ThingID);
        }
    }
}
