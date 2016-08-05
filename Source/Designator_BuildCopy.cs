using UnityEngine;
using Verse;
using RimWorld;

namespace BuildProductive
{
    public class Designator_BuildCopy : Designator_Build
    {
        public static readonly ThingDef PlaceholderDef = DefDatabase<ThingDef>.GetNamed("Wall");

        private Building _building;

        private Rot4 buildingRot;

        public Designator_BuildCopy() : base(PlaceholderDef)
        {
            defaultLabel = "BuildProductive.DesignatorBuildCopy".Translate();
            defaultDesc = "BuildProductive.DesignatorBuildCopyDesc".Translate();
            icon = ContentFinder<Texture2D>.Get("BuildProductive/BuildCopy", true);
            hotKey = KeyBindingDefOf.Misc8;

            WriteStuff = true;
        }

        // FIXME If IconDrawColor ever becomes public
        public Color PublicIconDrawColor
        {
            get { return IconDrawColor; }
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
            soundSucceeded = SoundDefOf.DesignatePlaceBuilding;

            base.DesignateSingleCell(c);

            if (_building != null)
            {
                Bootstrapper.Watchdog.TryAddBuilding(c, _building);
            }
        }

        public override AcceptanceReport CanDesignateThing(Thing t)
        {
            var thing = Find.Selector.SingleSelectedThing;

            var blueprint = thing as Blueprint_Build;
            var frame = thing as Frame;
            var building = thing as Building;

            if (frame != null)
            {
                entDef = frame.def.entityDefToBuild;
                StuffDef = frame.Stuff;
            }
            else if (building != null)
            {
                if (building.def.frameDef == null) return false;

                entDef = building.def;
                StuffDef = building.Stuff;
            }
            else if (blueprint != null)
            {
                entDef = blueprint.def.entityDefToBuild;
                StuffDef = blueprint.stuffToUse;
            }
            else return false;

            if (!Visible) return false;

            var thingDef = entDef as ThingDef;

            icon = thingDef.uiIcon;

            if (thingDef.uiIconPath.NullOrEmpty())
            {
                iconProportions = thingDef.graphicData.drawSize;
                iconDrawScale = GenUI.IconDrawScale(thingDef);
            }
            else
            {
                iconProportions = new Vector2(1f, 1f);
                iconDrawScale = 1f;
            }

            buildingRot = thing.Rotation;

            soundSucceeded = activateSound;

            return true;
        }

        public override void DesignateThing(Thing t)
        {
            DesignatorManager.Select(this);
            placingRot = buildingRot;

            _building = t as Building;
            if (_building is Frame) _building = null;
        }
    }
}
