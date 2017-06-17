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
                    return new AcceptanceReport("ExistingCarnThingThere".Translate(checkingDef.label));
                }
            }

            foreach (var wallCell in rect.EdgeCells)
            {
                // Edge cells are more exclusive
                if (wallCell.GetThingList(base.Map)
                   .Any(t => t is Blueprint
                          || t is Frame
                          || t is Building))
                {
                    return new AcceptanceReport("ExistingCarnThingThere".Translate(checkingDef.label));
                }
            }

            // No blocking interaction cell
            ThingDef def = checkingDef as ThingDef;
            if (def != null && def.hasInteractionCell)
            {
                var intCell = Thing.InteractionCellWhenAt(def, loc, rot, base.Map);
                if (intCell.GetThingList(base.Map).Any(t => t is Blueprint_StuffHacked || t is Frame_StuffHacked || t is Building_Carn))
                {
                    return new AcceptanceReport("ExistingCarnThingThere".Translate(checkingDef.label));
                }
            }


            return base.AllowsPlacing(checkingDef, loc, rot, thingToIgnore);
        }
    }
}
