using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;

namespace Carnivale
{
    public class JobGiver_StrikeBuildings : ThinkNode_JobGiver
    {
        public override float GetPriority(Pawn pawn)
        {
            return ThinkNodePriority.AssignedWork;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.story.WorkTypeIsDisabled(WorkTypeDefOf.Construction)
                    && pawn.skills.GetSkill(SkillDefOf.Construction).Level < 2)
            {
                return null;
            }

            PawnDuty duty = pawn.mindState.duty;

            if (duty == null)
            {
                return null;
            }

            var info = Find.VisibleMap.GetComponent<CarnivalInfo>();

            if (info != null && info.Active)
            {
                Building building = info.carnivalBuildings.LastOrDefault();

                if (building != null && pawn.CanReserveAndReach(building, PathEndMode.ClosestTouch, Danger.None))
                {
                    pawn.Reserve(building);
                    return new Job(_DefOf.Job_StrikeBuildings, building);
                }
            }

            return null;
        }
    }
}
