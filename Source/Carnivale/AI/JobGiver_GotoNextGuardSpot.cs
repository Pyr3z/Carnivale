using Verse;
using Verse.AI;

namespace Carnivale
{
    public class JobGiver_GotoNextGuardSpot : JobGiver_Carn
    {
        private static IntRange numWanders = new IntRange(1, 4);

        private int spotIndex = 0;

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!Validate() || Info.guardPositions.NullOrEmpty()) return null;

            spotIndex %= Info.guardPositions.Count;

            var spot = Info.guardPositions[spotIndex++];

            if (spot.IsValid)
            {
                return new Job(_DefOf.Job_GuardSpot, spot)
                {
                    count = numWanders.RandomInRange,
                    locomotionUrgency = LocomotionUrgency.Walk
                };
            }
            else
            {
                Info.guardPositions.RemoveAt(--spotIndex);
            }

            return null;
        }

    }
}
