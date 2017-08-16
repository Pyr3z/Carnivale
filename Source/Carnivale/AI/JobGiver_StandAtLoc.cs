using Verse;
using Verse.AI;

namespace Carnivale
{
    public class JobGiver_StandAtLoc : ThinkNode_JobGiver
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

            return new Job(_DefOf.Job_StandAtLoc, duty.focus, duty.focusSecond);
        }
    }
}
