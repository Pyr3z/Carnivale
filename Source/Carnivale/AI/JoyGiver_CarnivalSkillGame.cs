using RimWorld;
using Verse;

namespace Carnivale
{
    public class JoyGiver_CarnivalSkillGame : JoyGiver_InteractBuildingInteractionCell
    {

        public override float GetChance(Pawn pawn)
        {
            var meleeSkill = pawn.skills.GetSkill(SkillDefOf.Melee).Level;
            var shootingSkill = pawn.skills.GetSkill(SkillDefOf.Shooting).Level;
            var baseChance = base.GetChance(pawn);

            if (this.def.jobDef == _DefOf.Job_PlayHighStriker)
            {
                return meleeSkill >= shootingSkill ? baseChance : baseChance / 3;
            }



            return 0f;
        }

    }
}
