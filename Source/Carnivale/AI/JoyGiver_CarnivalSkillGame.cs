using RimWorld;
using Verse;

namespace Carnivale
{
    public class JoyGiver_CarnivalSkillGame : JoyGiver_InteractBuildingInteractionCell
    {

        public override float GetChance(Pawn pawn)
        {
            if (this.def.jobDef == _DefOf.Job_PlayHighStriker
                && pawn.equipment.Primary.def.IsMeleeWeapon)
            {
                // Only pawns holding melee weapon play high striker
                return base.GetChance(pawn);
            }



            return 0f;
        }

    }
}
