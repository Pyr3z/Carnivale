using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace Carnivale
{
    public class JobDriver_GuardSpot : JobDriver
    {
        private static IntRange wanderWaitTickRange = new IntRange(20, 80);


        public override string GetReport()
        {
            return "guarding carnival.";
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            for (; CurJob.count > 0; CurJob.count--)
            {
                yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);

                yield return FindNextWanderSpot(wanderWaitTickRange.RandomInRange);
            }
        }


        private Toil FindNextWanderSpot(int waitTicks)
        {
            return new Toil
            {
                initAction = delegate
                {
                    pawn.pather.StopDead();

                    var rect = CellRect.CenteredOn(pawn.Position, 4);
                    var dest = rect.Cells
                        .Where(c => c != pawn.Position && pawn.CanReach(c, PathEndMode.OnCell, Danger.None))
                        .RandomElement();
                    if (dest.IsValid)
                    {
                        CurJob.SetTarget(TargetIndex.A, dest);
                    }
                    else
                    {
                        EndJobWith(JobCondition.Succeeded);
                    }
                },
                socialMode = RandomSocialMode.SuperActive,
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = waitTicks
            };
        }
    }
}
