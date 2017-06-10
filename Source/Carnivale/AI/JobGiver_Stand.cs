using System;
using Verse;
using Verse.AI;

namespace Carnivale
{
    public class JobGiver_Stand : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            PawnDuty duty = pawn.mindState.duty;

            if (duty == null || duty.def != _DefOf.Duty_Stand)
            {
                return null;
            }

            return null;
        }
    }
}
