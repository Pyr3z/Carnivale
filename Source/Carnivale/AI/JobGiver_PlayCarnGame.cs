using RimWorld;
using Verse;
using Verse.AI;

namespace Carnivale
{
    public class JobGiver_PlayCarnGame : ThinkNode
    {
        public override float GetPriority(Pawn pawn)
        {
            var info = pawn.MapHeld.GetComponent<CarnivalInfo>();
            if (!info.Active) return 0f;

            return info.carnivalArea.Contains(pawn.PositionHeld) ? 0f : ThinkNodePriority.AssignedJoy; 
        }

        public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        {
            var info = pawn.MapHeld.GetComponent<CarnivalInfo>();
            if (!info.Active) return ThinkResult.NoJob;

            var meleeSkill = pawn.skills.GetSkill(SkillDefOf.Melee).Level;
            var shootingSkill = pawn.skills.GetSkill(SkillDefOf.Shooting).Level;

            foreach (var gameStall in info.GetBuildingsOf(CarnBuildingType.Stall | CarnBuildingType.Attraction))
            {
                if (gameStall.def == _DefOf.Carn_GameHighStriker
                    && meleeSkill >= shootingSkill)
                {
                    return new ThinkResult(new Job(_DefOf.Job_PlayHighStriker, gameStall, gameStall.InteractionCell), this);
                }
            }

            return ThinkResult.NoJob;
        }

    }
}
