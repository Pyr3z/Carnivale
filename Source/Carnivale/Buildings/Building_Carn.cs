using Verse;

namespace Carnivale
{
    public abstract class Building_Carn : Building
    {
        [Unsaved]
        private CompProperties_CarnBuilding propsInt = null;

        public CompProperties_CarnBuilding Props
        {
            get
            {
                if (propsInt == null)
                {
                    propsInt = GetComp<CompCarnBuilding>().Props;
                }
                return propsInt;
            }
        }

        public CarnBuildingType Type
        {
            get
            {
                return Props.type;
            }
        }

        [Unsaved]
        private CellRect occupiedRectInt;

        public CellRect OccupiedRect
        {
            get
            {
                if (this.occupiedRectInt == default(CellRect))
                    occupiedRectInt = this.OccupiedRect();
                return occupiedRectInt;
            }
        }
    }
}
