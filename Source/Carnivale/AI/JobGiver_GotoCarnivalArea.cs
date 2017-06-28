using RimWorld;
using System;
using Verse;
using Verse.AI;

namespace Carnivale
{
    public class JobGiver_GotoCarnivalArea : ThinkNode
    {
        public override float GetPriority(Pawn pawn)
        {
            var info = pawn.MapHeld.GetComponent<CarnivalInfo>();
            if (!info.Active) return 0f;

            return info.carnivalArea.Contains(pawn.PositionHeld) ? 0f : 10f; 
        }

        public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        {
            var info = pawn.MapHeld.GetComponent<CarnivalInfo>();
            if (!info.Active) return ThinkResult.NoJob;

            IntVec3 gotoSpot;
            if (CellFinder.TryFindRandomReachableCellNear(
                info.carnivalArea.RandomCell,
                pawn.MapHeld,
                info.baseRadius,
                TraverseParms.For(pawn, Danger.Some, TraverseMode.PassDoors),
                null,
                null,
                out gotoSpot
            ))
            {
                return new ThinkResult(new Job(JobDefOf.Goto, gotoSpot), this);
            }

            return ThinkResult.NoJob;
        }

    }
}
