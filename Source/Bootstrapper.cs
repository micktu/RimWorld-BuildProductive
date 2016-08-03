﻿using System;
using System.Reflection;
using Verse;
using RimWorld;
using CommunityCoreLibrary;

namespace BuildProductive
{
    class Bootstrapper : SpecialInjector
    {
        public static FieldInfo StuffDefField, WriteStuffField, GizmoListField, ObjListField, WantSwitchOn, AutoRearmField, HoldFireField;

        public static Designator_BuildCopy CopyDesignator;

        public static BuildingWatchdog Watchdog;
        public static MethodCallPatcher Patcher;

        public override bool Inject()
        {
            // Designator_Build private access
            StuffDefField = GetInstancePrivateField(typeof(Designator_Build), "stuffDef");
            WriteStuffField = GetInstancePrivateField(typeof(Designator_Build), "writeStuff");

            //var inspectGizmoGrid = typeof(GizmoGridDrawer).Assembly.GetType("RimWorld.InspectGizmoGrid");
            //GizmoListField = GetStaticPrivateField(inspectGizmoGrid, "gizmoList");
            //ObjListField = GetStaticPrivateField(inspectGizmoGrid, "objList");

            // Buildings and comps private access
            WantSwitchOn = GetInstancePrivateField(typeof(CompFlickable), "wantSwitchOn");
            AutoRearmField = GetInstancePrivateField(typeof(Building_TrapRearmable), "autoRearm");
            HoldFireField = GetInstancePrivateField(typeof(Building_TurretGun), "holdFire");

            // Hook into Command.IconDrawColor
            Detour(typeof(Command), "get_IconDrawColor", BindingFlags.Instance | BindingFlags.NonPublic,
                   typeof(VerseExtensions), "Command_get_IconDrawColor", BindingFlags.Static | BindingFlags.NonPublic);

            /*
            if (Patcher == null)
            {
                Patcher = new MethodCallPatcher();
                Patcher.def = new ThingDef { tickerType = TickerType.Normal };

                Patcher.AddPatch(inspectGizmoGrid, "DrawInspectGizmoGridFor",
                                 typeof(GizmoGridDrawer), "DrawGizmoGrid",
                                 typeof(VerseExtensions), "GizmoGridDrawer_DrawGizmoGrid");
            }
            Find.TickManager.RegisterAllTickabilityFor(Patcher);
            */
            
            // Initialize Designator
            if (CopyDesignator == null)
            {
                CopyDesignator = new Designator_BuildCopy();
            }
            ReverseDesignatorDatabase.AllDesignators.Add(CopyDesignator);
            
            // Initialize ticker
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

        public static FieldInfo GetInstancePrivateField(Type type, string name)
        {
            return type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static FieldInfo GetStaticPrivateField(Type type, string name)
        {
            return type.GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
        }
    }
}
