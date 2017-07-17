using System.Linq;
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

            if (Info.Entrance != null && Info.Entrance.assignedPawn == null)
            {
                if (!Info.AssignAnnouncerToBuilding(Info.GetBestAnnouncer(false), Info.Entrance))
                {
                    Log.Error("Unable to assign a ticket taker to carnival entrance.");
                }
            }

            TryAssignEntertainersToGames();
        }

        public override void UpdateAllDuties()
        {
            var wanderRect = CellRect.CenteredOn(Info.setupCentre, 8);
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
                    }
                    else
                    {
                        // rest on the off shift if not assigned a position
                        DutyUtility.ForceRest(pawn);
                    }
                    continue;
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
                    }
                    continue;
                }

                if (pawnRole.Is(CarnivalRole.Worker))
                {
                    DutyUtility.MeanderAndHelp(pawn, Info.AverageLodgeTentPos, 10f);
                    continue;
                }

                // Default
                DutyUtility.MeanderAndHelp(pawn, wanderRect.RandomCell, Info.baseRadius);
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
            foreach (var game in Info.GetBuildingsOf(CarnBuildingType.Attraction | CarnBuildingType.Stall).Select(g => g as Building_Carn))
            {
                var announcer = Info.GetBestAnnouncer();
                if (announcer != null)
                {
                    Info.AssignAnnouncerToBuilding(announcer, game, true);
                }
            }
        }
    }
}
