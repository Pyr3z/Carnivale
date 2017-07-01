using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;

namespace Carnivale
{
    public class JobGiver_HaulToCarrierOrTrash : ThinkNode_JobGiver
    {

        protected override Job TryGiveJob(Pawn pawn)
        {
            var info = pawn.MapHeld.GetComponent<CarnivalInfo>();
            if (!info.Active
                || !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)
                || pawn.story.WorkTagIsDisabled(WorkTags.Hauling))
                return null;

            var lord = info.currentLord;

            if (lord.LordJob is LordJob_EntertainColony)
            {
                if (info.thingsToHaul.Any())
                {
                    var haulable = info.thingsToHaul.LastOrDefault(t => 
                        pawn.carryTracker.MaxStackSpaceEver(t.def) > 0 &&
                        HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, t, false)
                    );

                    if (haulable != null)
                    {
                        if (!haulable.IsForbidden(Faction.OfPlayer))
                        {
                            if (Prefs.DevMode)
                                Log.Warning("[Debug] " + haulable + " from CarnivalInfo.thingsToHaul was claimed by the player. Removing from list.");

                            info.thingsToHaul.Remove(haulable);

                            return null;
                        }

                        pawn.Reserve(haulable);

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
