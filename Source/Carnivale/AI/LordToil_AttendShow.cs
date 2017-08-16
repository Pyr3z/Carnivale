using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Carnivale
{
    public class LordToil_AttendShow : LordToil
    {
        public override bool AllowRestingInBed
        {
            get
            {
                return false;
            }
        }

        public override bool AllowSatisfyLongNeeds
        {
            get
            {
                return false;
            }
        }

        private LordToilData_AttendShow Data
        {
            get
            {
                return (LordToilData_AttendShow)this.data;
            }
        }

        public LordToil_AttendShow(CellRect audienceRect, Pawn entertainer, IntVec3 entertainerSpot)
        {
            this.data = new LordToilData_AttendShow(audienceRect, entertainer, entertainerSpot);
        }


        public override void Init()
        {
            CarnUtils.Info.alreadyHadShowToday = true;
            CarnUtils.Info.showingNow = true;
        }

        public override void Cleanup()
        {
            CarnUtils.Info.showingNow = false;
            DutyUtility.MeanderAndHelp(Data.entertainer, Data.entertainerSpot, 20);
        }


        public override void UpdateAllDuties()
        {
            DutyUtility.EntertainShow(Data.entertainer, Data.entertainerSpot, Data.audienceRect.CenterCell);

            foreach (var pawn in lord.ownedPawns)
            {
                var spectateSpot = IntVec3.Invalid;
                
                if (!Data.audienceRect.Cells.Where((IntVec3 c) => pawn.CanReserve(c)).TryRandomElement(out spectateSpot))
                {
                    Log.Warning("[Carnivale] " + pawn + " tried to attend a show, but could not reserve any spot to go to.");
                    continue;
                }

                DutyUtility.AttendShow(pawn, spectateSpot, Data.entertainerSpot);
            }
        }


        public override ThinkTreeDutyHook VoluntaryJoinDutyHookFor(Pawn p)
        {
            return ThinkTreeDutyHook.MediumPriority;
        }

    }
}
