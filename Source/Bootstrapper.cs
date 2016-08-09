using System;
using System.Reflection;
using Verse;
using RimWorld;
using UnityEngine;

namespace BuildProductive
{
    [StaticConstructorOnStartup]
    class Bootstrapper
    {
        public static Type InspectGizmoGrid;
        public static FieldInfo StuffDefField, WriteStuffField, GizmoListField, ObjListField, WantSwitchOn, AutoRearmField, HoldFireField;
        public static MethodInfo IconDrawColor, MakeSolidThing;


        public static Designator_BuildCopy CopyDesignator;

        public static HookInjector HookPatcher;

        static Bootstrapper()
        {
            // Designator_Build private access
            StuffDefField = GetInstancePrivateField(typeof(Designator_Build), "stuffDef");
            WriteStuffField = GetInstancePrivateField(typeof(Designator_Build), "writeStuff");

            InspectGizmoGrid = typeof(GizmoGridDrawer).Assembly.GetType("RimWorld.InspectGizmoGrid");

            //GizmoListField = GetStaticPrivateField(inspectGizmoGrid, "gizmoList");
            //ObjListField = GetStaticPrivateField(inspectGizmoGrid, "objList");

            IconDrawColor = typeof(Command).GetMethod("get_IconDrawColor", BindingFlags.Instance | BindingFlags.NonPublic);
            MakeSolidThing = typeof(Blueprint_Build).GetMethod("MakeSolidThing", BindingFlags.Instance | BindingFlags.NonPublic);

            Log.Message(MakeSolidThing.Name);

            // Buildings and comps private access
            WantSwitchOn = GetInstancePrivateField(typeof(CompFlickable), "wantSwitchOn");
            AutoRearmField = GetInstancePrivateField(typeof(Building_TrapRearmable), "autoRearm");
            HoldFireField = GetInstancePrivateField(typeof(Building_TurretGun), "holdFire");

            if (HookPatcher == null)
            { 
                HookPatcher = new HookInjector();
                //HookPatcher.Inject(typeof(PreLoadUtility), "CheckVersionAndLoad", typeof(Bootstrapper), "OnLoad");
                HookPatcher.Inject(typeof(MapIniterUtility), "FinalizeMapInit", typeof(InitScript), "OnFinalizeMapInit");
                HookPatcher.Inject(typeof(Command), "get_IconDrawColor", typeof(VerseExtensions));
                HookPatcher.Inject(typeof(GenConstruct), "PlaceBlueprintForBuild", typeof(VerseExtensions));
                HookPatcher.Inject(typeof(Blueprint_Build), "MakeSolidThing", typeof(VerseExtensions));
                HookPatcher.Inject(typeof(Frame), "CompleteConstruction", typeof(VerseExtensions));
                HookPatcher.Inject(typeof(Frame), "FailConstruction", typeof(VerseExtensions));
                HookPatcher.Inject(typeof(Designator_Cancel), "DesignateThing", typeof(VerseExtensions));
            }
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
