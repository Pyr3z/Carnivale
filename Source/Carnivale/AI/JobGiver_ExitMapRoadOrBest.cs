using RimWorld;
using Verse;
using Verse.AI;
using Xnope;
using System.Linq;

namespace Carnivale
{
    public class JobGiver_ExitMapRoadOrBest : JobGiver_ExitMap
    {
        // ? cache road exit cells in CarnivalInfo? Or is efficiency not key here?

        protected override bool TryFindGoodExitDest(Pawn pawn, bool canDig, out IntVec3 dest)
        {
            var mode = (!canDig) ? TraverseMode.ByPawn : TraverseMode.PassAllDestroyableThings;

            var closestRoadEdgeTiles = pawn.Position.TryFindNearestRoadEdgeCells(pawn.MapHeld);

            if (closestRoadEdgeTiles
                .Where(c => pawn.CanReach(c, PathEndMode.OnCell, Danger.Deadly, canDig, mode))
                .TryRandomElement(out dest))
            {
                // Found closest edge tile
                return true;
            }

            // else, revert to JobGiver_ExitMapBest
            return RCellFinder.TryFindBestExitSpot(pawn, out dest, mode);
        }
    }
}
