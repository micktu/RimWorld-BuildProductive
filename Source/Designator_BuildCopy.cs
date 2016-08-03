using UnityEngine;
using Verse;
using RimWorld;

namespace BuildProductive
{
    public class Designator_BuildCopy : Designator_Build
    {
        public static readonly ThingDef PlaceholderDef = DefDatabase<ThingDef>.GetNamed("Wall");

        private Building _building;

        public Designator_BuildCopy() : base(PlaceholderDef)
        {
            defaultLabel = "BuildProductive.DesignatorBuildCopy".Translate();
            defaultDesc = "BuildProductive.DesignatorBuildCopyDesc".Translate();
            icon = ContentFinder<Texture2D>.Get("BuildProductive/BuildCopy", true);
            hotKey = KeyBindingDefOf.Misc8;

            WriteStuff = true;
        }

        public override string Label
        {
            get
            {
                return string.Format(defaultLabel, base.Label);
            }
        }

        protected ThingDef StuffDef
        {
            set
            {
                Bootstrapper.StuffDefField.SetValue(this, value);
            }
        }

        protected bool WriteStuff
        {
            set
            {
                Bootstrapper.WriteStuffField.SetValue(this, value);
            }
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            return base.CanDesignateCell(c);
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            base.DesignateSingleCell(c);
            Bootstrapper.Watchdog.TryAddBuilding(c, _building);
        }

        public override AcceptanceReport CanDesignateThing(Thing t)
        {
            var thing = Find.Selector.SingleSelectedThing;

            var building = thing as Building;

            if (building == null) return false;
            if (building.def.category != ThingCategory.Building) return false;
            if (building.def.frameDef == null) return false;
            if (building.Faction != Faction.OfPlayer)
            {
                if (building.Faction != null) return false;
                if (!building.ClaimableBy(Faction.OfPlayer)) return false;
            }

            entDef = building.def;
            StuffDef = building.Stuff;

            if (!Visible) return false;

            icon = entDef.uiIcon;

            if (building.def.uiIconPath.NullOrEmpty())
            {
                iconProportions = building.def.graphicData.drawSize;
                iconDrawScale = GenUI.IconDrawScale(building.def);
            }
            else
            {
                iconProportions = new Vector2(1f, 1f);
                iconDrawScale = 1f;
            }

            return true;
        }

        public override void DesignateThing(Thing t)
        {
            _building = t as Building;
            DesignatorManager.Select(this);
            placingRot = _building.Rotation;
        }
    }
}
