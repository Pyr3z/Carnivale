using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Carnivale.AI
{
    public class LordToil_DefendCarnival : LordToil_DefendPoint
    {

        private LordToil_DefendCarnival() { }

        public LordToil_DefendCarnival(IntVec3 defendPoint, float defendRadius) : base(defendPoint, defendRadius)
        { }

        public override void UpdateAllDuties()
        {
            LordToilData_DefendPoint data = base.Data;

            Pawn leader = this.lord.faction.leader;

            if (leader != null)
            {
                leader.mindState.duty = new PawnDuty(DutyDefOf.Defend, data.defendPoint, data.defendRadius);
                
                foreach (Pawn p in lord.ownedPawns)
                {
                    switch (p.GetCarnivalRole())
                    {
                        case CarnivalRole.Carrier:
                            p.mindState.duty = new PawnDuty(DutyDefOf.Follow, leader, 10f);
                            p.mindState.duty.locomotion = LocomotionUrgency.Walk;
                            break;
                        default:
                            p.mindState.duty = new PawnDuty(DutyDefOf.Defend, data.defendPoint, data.defendRadius);
                            break;
                        
                    }
                }
            }
        }

    }
}
