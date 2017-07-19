using RimWorld;
using Verse;
using Verse.AI;

namespace Carnivale
{
    public class JobGiver_GotoNextGuardSpot : JobGiver_Carn
    {
        private int spotIndex = 0;

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!Validate() || Info.guardPositions.NullOrEmpty()) return null;

            spotIndex %= Info.guardPositions.Count;

            var spot = Info.guardPositions[spotIndex++];

            if (spot.IsValid)
            {
                pawn.mindState.duty.focus = spot;

                return new Job(JobDefOf.Goto, spot)
                {
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
