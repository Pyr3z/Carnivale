using RimWorld;
using Verse;
using Verse.AI;

namespace Carnivale
{
    public static class DutyUtility
    {
        public static void BuildCarnival(Pawn pawn, IntVec3 centre, float radius)
        {
            if (pawn != null)
            {
                pawn.mindState.duty = new PawnDuty(_DefOf.Duty_BuildCarnival, centre, radius);

                pawn.workSettings.EnableAndInitialize();
                pawn.workSettings.SetPriority(WorkTypeDefOf.Construction, 1);
            }
        }

        public static void Meander(Pawn pawn, IntVec3 centre, float radius)
        {
            if (pawn != null)
            {
                pawn.mindState.duty = new PawnDuty(_DefOf.Duty_Meander, centre, radius); 
            }
        }

        public static void HitchToSpot(Pawn pawn, IntVec3 spot)
        {
            if (pawn != null)
            {
                pawn.mindState.duty = new PawnDuty(_DefOf.Duty_HitchToSpot, spot);
            }
        }
    }
}
