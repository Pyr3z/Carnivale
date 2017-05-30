using Carnivale.Enums;
using System.Collections.Generic;
using Verse;

namespace Carnivale
{
    public class CompProperties_CarnBuilding : CompProperties
    {
        public CarnBuildingType type = 0;

        public List<IntVec3> interiorBuildingOffsets;

        public CompProperties_CarnBuilding()
        {
            this.compClass = typeof(CompCarnBuilding);
        }
    }
}
