using Verse;

namespace Carnivale
{
    public class LordToil_EntertainColony : LordToil_Carn
    {

        public LordToil_EntertainColony() { }


        public override void Init()
        {
            base.Init();

            Info.entertainingNow = true;
        }

        public override void UpdateAllDuties()
        {
            int countCarriers = Info.pawnsWithRole[CarnivalRole.Carrier].Count;
            IntVec3 pos;

            foreach (var pawn in this.lord.ownedPawns)
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

                if (pawnRole.Is(CarnivalRole.Worker))
                {
                    DutyUtility.MeanderAndHelp(pawn, Info.AverageLodgeTentPos, 10f);
                }

                // Default
                DutyUtility.MeanderAndHelp(pawn, Info.setupCentre, Info.baseRadius);
            }
        }


        public override void Cleanup()
        {
            base.Cleanup();

            Info.entertainingNow = false;
            Info.alreadyEntertainedToday = true;
        }


        private void TryAssignEntertainersToGames()
        {

        }
    }
}
