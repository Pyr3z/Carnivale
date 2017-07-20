using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Carnivale
{
    public class LordToil_Leave : LordToil_Carn
    {
        public override bool AllowRestingInBed { get { return false; } }

        public override bool AllowSatisfyLongNeeds { get { return false; } }

        public override void UpdateAllDuties()
        {
            LocomotionUrgency urg = CarnivalUtils.Info.leavingUrgency;

            foreach (var pawn in this.lord.ownedPawns)
            {
                CarnivalRole role = pawn.GetCarnivalRole();

                if (role.Is(CarnivalRole.Carrier))
                {
                    var vendor = Info.pawnsWithRole[CarnivalRole.Vendor].RandomElementOrNull();
                    DutyUtility.LeaveMap(pawn, vendor, urg);
                }
                else if (role.Is(CarnivalRole.Guard))
                {
                    DutyUtility.LeaveMapAndEscort(pawn, GetClosestCarrier(pawn), urg);
                }
                else
                {
                    DutyUtility.LeaveMap(pawn, null, urg);
                }
            }
        }

        public override void Notify_PawnLost(Pawn victim, PawnLostCondition cond)
        {
            base.Notify_PawnLost(victim, cond);

            if (cond == PawnLostCondition.IncappedOrKilled)
            {
                CarnivalUtils.Info.leavingUrgency = LocomotionUrgency.Sprint;
            }
        }

        public override void LordToilTick()
        {
            if (Find.TickManager.TicksGame % 373 == 0)
            {
                UpdateAllDuties();
            }
        }
    }
}
