using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BuildProductive
{
    internal static class VerseExtensions
    {
        internal static Color Command_get_IconDrawColor(this Command command)
        {
            var des = Globals.CopyDesignator;

            if (command.icon == des.icon && command.hotKey == des.hotKey)
            {
                return des.PublicIconDrawColor;
            }

            // FIXME should Invoke get_IconDrawColor() when the injector will be able to take care of that
            return Color.white;
        }

        internal static void GizmoGridDrawer_DrawGizmoGrid(IEnumerable<Gizmo> gizmos, float startX, out Gizmo mouseoverGizmo)
        {
            var gizmoList = Privates.InspectGizmoGrid_gizmoList;

            // gizmos are actually null for main tab
            if (gizmos != null && gizmos == gizmoList)
            {
                /*
                var objList = Privates.InspectGizmoGrid_objList;
                var des = Globals.CopyDesignator;

                foreach (var o in objList)
                {
                    var thing = o as Thing;
                    if (thing == null) continue;
                    if (des.CanDesignateThing(thing).Accepted)
                    {
                        gizmoList.Add(Globals.CopyDesignator);
                    }
                }
                */
            }
            
            GizmoGridDrawer.DrawGizmoGrid(gizmos, startX, out mouseoverGizmo);
        }

        internal static Blueprint_Build GenConstruct_PlaceBlueprintForBuild(BuildableDef sourceDef, IntVec3 center, Rot4 rotation, Faction faction, ThingDef stuff)
        {
            var des = Globals.CopyDesignator;
            var blueprint = GenConstruct.PlaceBlueprintForBuild(sourceDef, center, rotation, faction, stuff);

            if (des.LastBuilding != null && des.PlacingDef == sourceDef && des.CurrentCell == center)
            {
                des.Keeper.RegisterBlueprint(des.LastBuilding, blueprint);
            }

            return blueprint;
        }

        internal static Thing Blueprint_Build_MakeSolidThing(this Blueprint_Build blueprint)
        {
            // FIXME should Invoke MakeSolidThing() when the injector will be able to take care of that
            var thing = ThingMaker.MakeThing(blueprint.def.entityDefToBuild.frameDef, blueprint.stuffToUse);
            Globals.Keeper.RegisterFrame(blueprint, thing as Frame);
            return thing;
        }

        internal static void Frame_CompleteConstruction(this Frame frame, Pawn worker)
        {
            var pos = frame.Position;
            frame.CompleteConstruction(worker);

            var building = Find.ThingGrid.ThingAt<Building>(pos);
            Globals.Keeper.UpdateBuilding(frame, building);
        }

        internal static void Frame_FailConstruction(this Frame frame, Pawn worker)
        {
            var pos = frame.Position;
            frame.FailConstruction(worker);

            var blueprint = Find.ThingGrid.ThingAt<Blueprint_Build>(pos);
            Globals.Keeper.RegisterBlueprint(frame, blueprint);
        }

        internal static void Designator_Cancel_DesignateThing(this Designator_Cancel des, Thing t)
        {
            des.DesignateThing(t);

            var blueprint = t as Blueprint;
            if (blueprint != null)
            {
                Globals.Keeper.UnregisterBlueprint(blueprint);
                return;
            }

            var frame = t as Frame;
            if (frame != null)
            {
                Globals.Keeper.UnregisterFrame(frame);
            }
        }
    }
}
