using System;
using System.Collections.Generic;
using Verse;

namespace Carnivale
{
    public class LordToil_Leave : LordToil_Carn
    {
        public override bool AllowRestingInBed { get { return false; } }

        public override bool AllowSatisfyLongNeeds { get { return false; } }



        public override void UpdateAllDuties()
        {
            foreach (var pawn in this.lord.ownedPawns)
            {
                CarnivalRole role = pawn.GetCarnivalRole();

                if (role.Is(CarnivalRole.Carrier))
                {
                    DutyUtility.LeaveMap(pawn, Info.pawnsWithRole[CarnivalRole.Vendor].RandomElementOrNull());
                }
                else if (role.Is(CarnivalRole.Guard))
                {
                    DutyUtility.LeaveMapAndEscort(pawn, GetClosestCarrier(pawn));
                }
                else
                {
                    DutyUtility.LeaveMap(pawn);
                }
            }
        }



        public override void LordToilTick()
        {
            if (Find.TickManager.TicksGame % 499 == 0)
            {
                if (!((List<Pawn>)Info.pawnsWithRole[CarnivalRole.Vendor]).Any(v => v.Spawned))
                {
                    UpdateAllDuties();
                }
            }
        }
    }
}
