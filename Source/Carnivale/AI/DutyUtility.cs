using RimWorld;
using Verse;
using Verse.AI;

namespace Carnivale
{
    public static class DutyUtility
    {
        public static void BuildCarnival(Pawn pawn, IntVec3 centre, float radius)
        {
            if (pawn.mindState != null)
            {
                pawn.mindState.duty = new PawnDuty(_DefOf.Duty_BuildCarnival, centre, radius);

                pawn.workSettings.EnableAndInitialize();

                pawn.workSettings.SetPriority(WorkTypeDefOf.Construction, 1);

                if (!pawn.story.WorkTypeIsDisabled(_DefOf.PlantCutting)
                    && pawn.skills.GetSkill(SkillDefOf.Growing).Level > 1)
                    pawn.workSettings.SetPriority(_DefOf.PlantCutting, 2);
            }
        }

        public static void MeanderAndHelp(Pawn pawn, IntVec3 centre, float radius)
        {
            if (pawn.mindState != null)
            {
                pawn.mindState.duty = new PawnDuty(_DefOf.Duty_MeanderAndHelp, centre, radius);

                pawn.workSettings.EnableAndInitialize();

                if (!pawn.story.WorkTypeIsDisabled(_DefOf.PlantCutting)
                    && pawn.skills.GetSkill(SkillDefOf.Growing).Level > 1)
                    pawn.workSettings.SetPriority(_DefOf.PlantCutting, 1);

                // Does setting hauling even do anything for non-colonists?
                // I still have to assign them the hauling job via a
                // Duty_MeanderAndWork JobGiver, but that's giving errors.
                //if (!pawn.story.WorkTypeIsDisabled(WorkTypeDefOf.Hauling))
                //    pawn.workSettings.SetPriority(WorkTypeDefOf.Hauling, 1);
            }
        }

        public static void Meander(Pawn pawn, IntVec3 centre, float radius)
        {
            if (pawn.mindState != null)
            {
                pawn.mindState.duty = new PawnDuty(_DefOf.Duty_Meander, centre, radius);
            }
        }

        public static void HitchToSpot(Pawn pawn, IntVec3 spot)
        {
            if (pawn.mindState != null)
            {
                pawn.mindState.duty = new PawnDuty(_DefOf.Duty_HitchToSpot, spot);
            }
        }

        public static void GuardSmallArea(Pawn pawn, IntVec3 centre, float radius)
        {
            if (pawn.mindState != null)
            {
                pawn.mindState.duty = new PawnDuty(_DefOf.Duty_GuardSmallArea, centre, radius);
            }
        }

        public static void ForceRest(Pawn pawn)
        {
            if (pawn.mindState != null)
            {
                pawn.needs.rest.CurLevel = 0.35f;
                pawn.mindState.duty = new PawnDuty(_DefOf.Duty_ForceRest);
            }
        }

    }
}
