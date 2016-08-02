using System;
using System.Reflection;
using Verse;
using RimWorld;
using CommunityCoreLibrary;

namespace BuildProductive
{
    class Bootstrapper : SpecialInjector
    {
        public static FieldInfo StuffDefField, WriteStuffField, WantSwitchOn, AutoRearmField, HoldFireField;

        public static Designator_BuildCopy CopyDesignator;

        public static BuildingWatchdog Watchdog;

        public override bool Inject()
        {
            StuffDefField = GetPrivateField(typeof(Designator_Build), "stuffDef");
            WriteStuffField = GetPrivateField(typeof(Designator_Build), "writeStuff");

            WantSwitchOn = GetPrivateField(typeof(CompFlickable), "wantSwitchOn");
            AutoRearmField = GetPrivateField(typeof(Building_TrapRearmable), "autoRearm");
            HoldFireField = GetPrivateField(typeof(Building_TurretGun), "holdFire");

            if (CopyDesignator == null)
            {
                CopyDesignator = new Designator_BuildCopy();
            }
            ReverseDesignatorDatabase.AllDesignators.Add(CopyDesignator);

            if (Watchdog == null)
            {
                Watchdog = new BuildingWatchdog();
                Watchdog.def = new ThingDef { tickerType = TickerType.Normal };
            }
            Find.TickManager.RegisterAllTickabilityFor(Watchdog);

            return true;
        }

        public static bool Detour(Type srcType, string srcName, BindingFlags srcFlags, Type dstType, string dstName, BindingFlags dstFlags)
        {
            var src = srcType.GetMethod(srcName, srcFlags);
            var dst = dstType.GetMethod(dstName, dstFlags);
            return Detours.TryDetourFromTo(src, dst);
        }

        public static FieldInfo GetPrivateField(Type type, string name)
        {
            return type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }
}
