using System;
using Verse;
using Verse.AI;

namespace Carnivale
{
    public class JobGiver_GuardSmallArea : JobGiver_Wander
    {
        public override float GetPriority(Pawn pawn)
        {
            return ThinkNodePriority.AssignedWork;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            PawnDuty duty = pawn.mindState.duty;

            if (duty == null)
            {
                return null;
            }

            return new Job(_DefOf.Job_GuardSmallArea, duty.focus);
        }

        protected override IntVec3 GetWanderRoot(Pawn pawn)
        {
            return WanderUtility.BestCloseWanderRoot(pawn.mindState.duty.focus.Cell, pawn);
        }

    }
}
