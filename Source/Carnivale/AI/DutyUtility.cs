using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Carnivale
{
    public static class DutyUtility
    {
        public static void SetAsBuilder(Pawn pawn, IntVec3 centre, float radius)
        {
            if (pawn != null)
            {
                pawn.mindState.duty = new PawnDuty(_DefOf.Duty_BuildCarnival, centre, radius);

                pawn.workSettings.EnableAndInitialize();
                pawn.workSettings.SetPriority(WorkTypeDefOf.Construction, 1);
            }
        }

        public static void SetAsIdler(Pawn pawn, List<Pawn> totalPawns)
        {
            Pawn random;

            if (totalPawns.TryRandomElement(out random)
                && pawn != null)
            {
                // TODO: custom duties, i.e. socialising, moving between carnival buildings
                pawn.mindState.duty = new PawnDuty(DutyDefOf.Escort, random, 6f);
            }
        }

        public static void SetAsStander(Pawn pawn, IntVec3 spot)
        {

        }
    }
}
