using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BuildProductive
{
    internal static class VerseExtensions
    {
        internal static Color Command_get_IconDrawColor(this Command command)
        {
            var des = Bootstrapper.CopyDesignator;

            if (command.icon == des.icon && command.hotKey == des.hotKey)
            {
                return des.PublicIconDrawColor;
            }

            // FIXME If Command.IconDrawColor becomes public, return command.IconDrawColor
            return Color.white;
        }

        internal static void GizmoGridDrawer_DrawGizmoGrid(IEnumerable<Gizmo> gizmos, float startX, out Gizmo mouseoverGizmo)
        {
            Command_Action command_Action = new Command_Action();
            command_Action.defaultLabel = "Test Icon";
            command_Action.action = delegate
            {
            };
            (gizmos as List<Gizmo>).Add(command_Action);

            GizmoGridDrawer.DrawGizmoGrid(gizmos, startX, out mouseoverGizmo);
        }

        internal static void PostLoadInitter_DoAllPostLoadInits()
        {
            PostLoadInitter.DoAllPostLoadInits();
            Log.Message("!!! PostLoad!");
        }

        internal static void PreLoadUtility_CheckVersionAndLoad(string path, ScribeMetaHeaderUtility.ScribeHeaderMode mode, Action loadAct)
        {
            Log.Message("!!! CheckVersionAndLoad");
            PreLoadUtility.CheckVersionAndLoad(path, mode, loadAct);
        }

        internal static void Building_SetFaction(this Building building, Faction newFaction, Pawn recruiter = null)
        {
            Log.Message("SetFaction");
            building.SetFaction(newFaction, recruiter);
        }
    }
}
