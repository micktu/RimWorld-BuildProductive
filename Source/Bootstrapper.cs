﻿using System;
using System.Reflection;
using UnityEngine;
using Verse;
using RimWorld;
using CommunityCoreLibrary;
using CommunityCoreLibrary.Detour;

namespace BuildProductive
{
    [StaticConstructorOnStartup]
    class Bootstrapper : MonoBehaviour
    {
        public static Type InspectGizmoGrid;
        public static FieldInfo StuffDefField, WriteStuffField, GizmoListField, ObjListField, WantSwitchOn, AutoRearmField, HoldFireField;

        public static Designator_BuildCopy CopyDesignator;
        public static BuildingWatchdog Watchdog;

		public static MethodCallPatcher Patcher;
		public static GameObject PatcherContainer;

        public static HookInjector2 HookPatcher;
        public static GameObject HookPatcherContainer;

        public static readonly bool InjectTestPatcher = true;

        static Bootstrapper()
        {
            Log.Message("BuildProductive has arrived.");

            // Designator_Build private access
            StuffDefField = GetInstancePrivateField(typeof(Designator_Build), "stuffDef");
            WriteStuffField = GetInstancePrivateField(typeof(Designator_Build), "writeStuff");

            InspectGizmoGrid = typeof(GizmoGridDrawer).Assembly.GetType("RimWorld.InspectGizmoGrid");

            //GizmoListField = GetStaticPrivateField(inspectGizmoGrid, "gizmoList");
            //ObjListField = GetStaticPrivateField(inspectGizmoGrid, "objList");

            // Buildings and comps private access
            WantSwitchOn = GetInstancePrivateField(typeof(CompFlickable), "wantSwitchOn");
            AutoRearmField = GetInstancePrivateField(typeof(Building_TrapRearmable), "autoRearm");
            HoldFireField = GetInstancePrivateField(typeof(Building_TurretGun), "holdFire");

            Log.Message("Detouring.");
            // Hook into Command.IconDrawColor
            /*Detour(typeof(Command), "get_IconDrawColor", BindingFlags.Instance | BindingFlags.NonPublic,
                   typeof(VerseExtensions), "Command_get_IconDrawColor", BindingFlags.Static | BindingFlags.NonPublic);*/


            var p1 = typeof(Command).GetMethod("ProcessInput").MethodHandle.GetFunctionPointer().ToInt32();
            var p2 = typeof(Command).GetMethod("get_IconDrawColor", BindingFlags.Instance | BindingFlags.NonPublic).MethodHandle.GetFunctionPointer().ToInt32();
            var p3 = typeof(GizmoGridDrawer).GetMethod("DrawGizmoGrid", BindingFlags.Static | BindingFlags.Public).MethodHandle.GetFunctionPointer().ToInt32();
            var p4 = typeof(PostLoadInitter).GetMethod("DoAllPostLoadInits", BindingFlags.Static | BindingFlags.Public).MethodHandle.GetFunctionPointer().ToInt32();
            var p5 = InspectGizmoGrid.GetMethod("DrawInspectGizmoGridFor", BindingFlags.Static | BindingFlags.Public).MethodHandle.GetFunctionPointer().ToInt32();

            Log.Message(String.Format("ProcessInput: {0:X8}, IconDrawColor: {1:X8}, DrawGizmoGrid: {2:X8}, DoAllPostLoadInits: {3:X8}, DrawInspectGizmoGridFor: {4:X8}", p1, p2, p3, p4, p5));

            /*
            Log.Message("Initializing patcher.");
            if (InjectTestPatcher && PatcherContainer == null)
            {
                PatcherContainer = new GameObject();
                GameObject.DontDestroyOnLoad(PatcherContainer);
                Patcher = PatcherContainer.AddComponent<MethodCallPatcher>();

                var preLoadUtility = typeof(StatWorker_Extensions).Assembly.GetType("CommunityCoreLibrary.Detour._PreLoadUtility", true);

                Patcher.AddPatch(preLoadUtility, "_CheckVersionAndLoad",
                                 typeof(PostLoadInitter), "DoAllPostLoadInits",
                                 typeof(VerseExtensions), "PostLoadInitted_DoAllPostLoadInits");

                Patcher.AddPatch(typeof(PreLoadUtility), "CheckVersionAndLoad",
                                 typeof(PostLoadInitter), "DoAllPostLoadInits",
                                 typeof(VerseExtensions), "PostLoadInitted_DoAllPostLoadInits");
                
                Patcher.AddPatch(InspectGizmoGrid, "DrawInspectGizmoGridFor",
                                 typeof(GizmoGridDrawer), "DrawGizmoGrid",
                                 typeof(VerseExtensions), "GizmoGridDrawer_DrawGizmoGrid");
            }
            */
            if (InjectTestPatcher && HookPatcherContainer == null)
                {
                HookPatcherContainer = new GameObject();
                GameObject.DontDestroyOnLoad(HookPatcherContainer);
                HookPatcher = HookPatcherContainer.AddComponent<HookInjector2>();

                HookPatcher.AddPatch(typeof(PostLoadInitter), "DoAllPostLoadInits",
                                 typeof(VerseExtensions), "PostLoadInitted_DoAllPostLoadInits");

                HookPatcher.AddPatch(typeof(GizmoGridDrawer), "DrawGizmoGrid",
                                typeof(VerseExtensions), "GizmoGridDrawer_DrawGizmoGrid");
            }

            /*
            Log.Message("Initializing designator.");
            // Initialize Designator
            if (CopyDesignator == null)
            {
                CopyDesignator = new Designator_BuildCopy();
            }
            ReverseDesignatorDatabase.AllDesignators.Add(CopyDesignator);

            Log.Message("Initializing watchdog.");
            // Initialize ticker
            if (Watchdog == null)
            {
                Watchdog = new BuildingWatchdog();
                Watchdog.def = new ThingDef { tickerType = TickerType.Normal };
            }
            Find.TickManager.RegisterAllTickabilityFor(Watchdog);
            */

            Log.Message("BuildProductive initalized.");
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
