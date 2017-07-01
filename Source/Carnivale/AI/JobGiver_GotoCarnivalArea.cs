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



            IntVec3 gotoSpot;
            if (info.colonistsInArea.Contains(pawn))
            {
                // sort of wander around
                for (int i = 0; i < 10; i++)
                {
                    if (CellFinder.TryFindRandomReachableCellNear(
                        info.carnivalArea.ContractedBy(5).RandomCell,
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
                    return new ThinkResult(new Job(JobDefOf.Goto, info.bannerCell), this);
                }
            }

            return ThinkResult.NoJob;
        }

    }
}
