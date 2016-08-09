using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using RimWorld;

namespace BuildProductive
{
    public static class Privates
    {
        public static Type InspectGizmoGrid;
        public static FieldInfo StuffDefField, WriteStuffField, GizmoListField, ObjListField, WantSwitchOn, AutoRearmField, HoldFireField;
        public static MethodInfo IconDrawColor, MakeSolidThing;

        public static List<Gizmo> InspectGizmoGrid_gizmoList;
        public static List<object> InspectGizmoGrid_objList;

        public static void Resolve()

        {
            // Designator_Build
            StuffDefField = GetInstancePrivateField(typeof(Designator_Build), "stuffDef");
            WriteStuffField = GetInstancePrivateField(typeof(Designator_Build), "writeStuff");

            // InspectGizmoGrid
            InspectGizmoGrid = typeof(GizmoGridDrawer).Assembly.GetType("RimWorld.InspectGizmoGrid");
            GizmoListField = GetStaticPrivateField(InspectGizmoGrid, "gizmoList");
            ObjListField = GetStaticPrivateField(InspectGizmoGrid, "objList");

            // Command
            IconDrawColor = typeof(Command).GetMethod("get_IconDrawColor", BindingFlags.Instance | BindingFlags.NonPublic);

            // Blueprint
            MakeSolidThing = typeof(Blueprint_Build).GetMethod("MakeSolidThing", BindingFlags.Instance | BindingFlags.NonPublic);

            // Building and ThingComp
            WantSwitchOn = GetInstancePrivateField(typeof(CompFlickable), "wantSwitchOn");
            AutoRearmField = GetInstancePrivateField(typeof(Building_TrapRearmable), "autoRearm");
            HoldFireField = GetInstancePrivateField(typeof(Building_TurretGun), "holdFire");
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

