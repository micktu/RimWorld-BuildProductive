using UnityEngine;
using Verse;
using RimWorld;

namespace BuildProductive
{
    public class Designator_BuildCopy : Designator_Build
    {
        public static readonly ThingDef PlaceholderDef = DefDatabase<ThingDef>.GetNamed("Wall");

        public Building LastBuilding { get; private set; }

        public IntVec3 CurrentCell { get; private set; }

        public BuildingKeeper Keeper { get; private set; }

        private Rot4 _buildingRot;

        public Designator_BuildCopy() : base(PlaceholderDef)
        {
            defaultLabel = "BuildProductive.DesignatorBuildCopy".Translate();
            defaultDesc = "BuildProductive.DesignatorBuildCopyDesc".Translate();
            icon = ContentFinder<Texture2D>.Get("BuildProductive/BuildCopy", true);
            hotKey = KeyBindingDefOf.Misc8;

            WriteStuff = true;

            Keeper = new BuildingKeeper();
            Globals.Keeper = Keeper;
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
                Privates.StuffDefField.SetValue(this, value);
            }
            get
            {
                return Privates.StuffDefField.GetValue(this) as ThingDef;
            }
        }

        protected bool WriteStuff
        {
            set
            {
                Privates.WriteStuffField.SetValue(this, value);
            }
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            return base.CanDesignateCell(c);
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            soundSucceeded = SoundDefOf.DesignatePlaceBuilding;

            CurrentCell = c;
            base.DesignateSingleCell(c);
            
            if (DebugSettings.godMode || entDef.GetStatValueAbstract(StatDefOf.WorkToMake, StuffDef) == 0f)
            {
                var building = Find.ThingGrid.ThingAt<Building>(c);
                Keeper.UpdateBuilding(LastBuilding, building);
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

            _buildingRot = thing.Rotation;

            soundSucceeded = activateSound;

            return true;
        }

        public override void DesignateThing(Thing t)
        {
            DesignatorManager.Select(this);
            placingRot = _buildingRot;

            LastBuilding = t as Building;
            if (LastBuilding is Frame) LastBuilding = null;
        }
    }
}
