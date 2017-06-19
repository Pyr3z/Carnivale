using RimWorld;
using System.Linq;
using Verse;

namespace Carnivale
{
    public class PlaceWorker_CarnBuilding : PlaceWorker_NotUnderRoof
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Thing thingToIgnore = null)
        {
            var rect = GenAdj.OccupiedRect(loc, rot, checkingDef.Size);

            foreach (var innerCell in rect.ContractedBy(1))
            {
                // Interior only excludes other carn buildings
                if (innerCell.GetThingList(base.Map)
                   .Any(t => t is Blueprint_StuffHacked
                          || t is Frame_StuffHacked
                          || t is Building_Carn))
                {
                    return new AcceptanceReport("CarnTentInvalidInterior".Translate(checkingDef.label));
                }
            }

            foreach (var wallCell in rect.EdgeCells.Concat(rect.ExpandedBy(1).EdgeCells))
            {
                // Edge cells are more exclusive
                if (wallCell.GetThingList(base.Map)
                   .Any(t => t is Blueprint
                          || t is Frame
                          || t is Building))
                {
                    return new AcceptanceReport("CarnTentInvalidWalls".Translate(checkingDef.label));
                }
            }


            return base.AllowsPlacing(checkingDef, loc, rot, thingToIgnore);
        }

    }
}
