using RimWorld;
using System.Linq;
using Verse;

namespace Carnivale
{
    public class PlaceWorker_CarnBuilding : PlaceWorker_NotUnderRoof
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Thing thingToIgnore = null)
        {
            foreach (var cell in GenAdj.OccupiedRect(loc, rot, checkingDef.Size))
            {
                if (base.Map.thingGrid.ThingsAt(cell).Any(t => t is Blueprint_StuffHacked || t is Frame_StuffHacked || t is Building_Carn))
                {
                    return new AcceptanceReport("ExistingCarnThingThere".Translate(checkingDef.label));
                }
            }

            return base.AllowsPlacing(checkingDef, loc, rot, thingToIgnore);
        }
    }
}
