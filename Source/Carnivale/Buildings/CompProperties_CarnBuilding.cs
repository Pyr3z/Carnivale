using System.Collections.Generic;
using Verse;

namespace Carnivale
{
    public class CompProperties_CarnBuilding : CompProperties
    {
        public CarnBuildingType type = 0;

        public List<ThingPlacement> interiorThings = new List<ThingPlacement>();

        public CompProperties_CarnBuilding()
        {
            this.compClass = typeof(CompCarnBuilding);
        }
    }

    public class ThingPlacement
    {
        public ThingDef thingDef;

        public List<IntVec3> placementOffsets;
    }
}
