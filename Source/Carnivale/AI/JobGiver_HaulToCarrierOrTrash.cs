using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Carnivale
{
    public class JobGiver_HaulToCarrierOrTrash : ThinkNode_JobGiver
    {
        public override float GetPriority(Pawn pawn)
        {
            return base.GetPriority(pawn);
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            var info = pawn.MapHeld.GetComponent<CarnivalInfo>();
            if (info.currentLord == null
                || !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                return null;

            var lord = info.currentLord;

            if (lord.LordJob is LordJob_EntertainColony)
            {
                if (info.thingsToHaul.Any())
                {
                    var haulable = info.thingsToHaul.Last();

                    if (haulable != null)
                    {
                        return new Job(_DefOf.Job_HaulToCarrierOrTrash, haulable)
                        {
                            lord = lord
                        };
                    }
                }
            }

            return null;
        }
    }
}
