using Verse;

namespace Carnivale
{
    public class LordToil_EntertainColony : LordToil_Carn
    {
        // FIELDS + PROPERTIES //



        // CONSTRUCTORS //

        public LordToil_EntertainColony() { }

        public LordToil_EntertainColony(CarnivalInfo info)
        {
            this.data = new LordToilData_Carnival(info);
        }


        // OVERRIDE METHODS //


        public override void UpdateAllDuties()
        {
            int countCarriers = Info.pawnsWithRole[CarnivalRole.Carrier].Count;
            IntVec3 pos;

            foreach (Pawn pawn in this.lord.ownedPawns)
            {
                CarnivalRole pawnRole = pawn.GetCarnivalRole();

                if (pawnRole.Is(CarnivalRole.Guard))
                {
                    if (Info.rememberedPositions.TryGetValue(pawn, out pos))
                    {
                        // more carriers = more radius
                        DutyUtility.GuardSmallArea(pawn, pos, countCarriers);
                        continue;
                    }
                    else
                    {
                        // rest on the off shift if not assigned a position
                        DutyUtility.ForceRest(pawn);
                    }
                }

                if (pawnRole.IsAny(CarnivalRole.Vendor, CarnivalRole.Carrier))
                {
                    if (Info.rememberedPositions.TryGetValue(pawn, out pos))
                    {
                        DutyUtility.HitchToSpot(pawn, pos);
                    }
                    else
                    {
                        DutyUtility.HitchToSpot(pawn, pawn.Position);
                    }
                    continue;
                }

                if (pawnRole.Is(CarnivalRole.Entertainer))
                {
                    if (Info.rememberedPositions.TryGetValue(pawn, out pos))
                    {
                        DutyUtility.HitchToSpot(pawn, pos);
                        continue;
                    }
                }

                // Default
                DutyUtility.MeanderAndHelp(pawn, Info.setupCentre, Info.baseRadius);
            }
        }


    }
}
