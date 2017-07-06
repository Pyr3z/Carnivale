using RimWorld;
using Verse;
using Verse.AI;

namespace Carnivale
{
    public class JobGiver_GotoCarnivalArea : ThinkNode
    {

        public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        {
            var info = pawn.MapHeld.GetComponent<CarnivalInfo>();
            if (!info.Active) return ThinkResult.NoJob;

            if (info.colonistsInArea.Contains(pawn))
            {
                // sort of wander around
                IntVec3 gotoSpot;
                for (int i = 0; i < 10; i++)
                {
                    if (CellFinder.TryFindRandomReachableCellNear(
                        info.carnivalBuildings.RandomElement().Position,
                        pawn.MapHeld,
                        info.baseRadius,
                        TraverseParms.For(pawn, Danger.Some, TraverseMode.PassDoors),
                        c => c.Walkable(pawn.Map) && c.DistanceToSquared(pawn.Position) > 16,
                        null,
                        out gotoSpot
                    ))
                    {
                        var job = new Job(JobDefOf.Goto, gotoSpot);
                        job.locomotionUrgency = LocomotionUrgency.Amble;
                        return new ThinkResult(job, this);
                    }
                }

                if (Prefs.DevMode)
                    Log.Error("Found no suitable place for " + pawn.NameStringShort + " to go in carnivalArea.ContractedBy(5).");
            }
            else
            {
                // go to entrance
                if (info.bannerCell.IsValid)
                {
                    info.colonistsInArea.Add(pawn);

                    var job = new Job(JobDefOf.Goto, info.bannerCell);
                    job.locomotionUrgency = LocomotionUrgency.Walk;
                    return new ThinkResult(job, this);
                }
            }

            return ThinkResult.NoJob;
        }

    }
}
