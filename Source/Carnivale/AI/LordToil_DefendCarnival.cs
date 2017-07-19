using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;
using Xnope;

namespace Carnivale
{
    public class LordToil_DefendCarnival : LordToil_Carn
    {

        public LordToil_DefendCarnival() { }

        public override void Init()
        {
            if (Prefs.DevMode)
                Log.Message("[Carnivale] Carnival is in defending mode.");
        }

        public override void Cleanup()
        {
            if (Prefs.DevMode)
                Log.Message("[Carnivale] Carnival is exiting defending mode.");
        }

        public override void UpdateAllDuties()
        {
            var nearHostilesPos = Map.attackTargetsCache.TargetsHostileToFaction(lord.faction)
                .Where(targ => !targ.ThreatDisabled())
                .Select(targ => targ.Thing.PositionHeld)
                .Average();

            if (!nearHostilesPos.IsValid)
            {
                lord.ReceiveMemo("NoThreats");
                return;
            }

            var closestGuardSpot = Info.guardPositions.MinBy(c => c.DistanceToSquared(nearHostilesPos));

            foreach (var pawn in lord.ownedPawns)
            {
                if (pawn.Dead) continue;

                CarnivalRole role = pawn.GetCarnivalRole();

                pawn.mindState.duty = null;

                if (role.Is(CarnivalRole.Manager))
                {
                    var guard = Info.GetBestGuard(false);

                    if (guard != null && !guard.Dead && !guard.Downed)
                    {
                        pawn.mindState.duty = new PawnDuty(DutyDefOf.Escort, guard, 7f)
                        {
                            locomotion = LocomotionUrgency.Jog
                        };
                    }
                    else
                    {
                        var carny = RandomCarnyByHealth();

                        if (carny != null)
                        {
                            DutyUtility.DefendPoint(pawn, carny);
                        }
                        else
                        {
                            DutyUtility.DefendPoint(pawn, closestGuardSpot);
                        }
                    }
                }
                else if (role.Is(CarnivalRole.Guard))
                {
                    DutyUtility.DefendPoint(pawn, closestGuardSpot);
                }
                else if (pawn.equipment != null && pawn.equipment.Primary != null)
                {
                    var carny = RandomCarnyByHealth();
                    LocalTargetInfo tentDoor;

                    if (carny != null)
                    {
                        DutyUtility.DefendPoint(pawn, carny);
                    }
                    else if (Rand.Bool && (tentDoor = Info.GetRandomTentDoor()).IsValid)
                    {
                        DutyUtility.DefendPoint(pawn, tentDoor);
                    }
                    else
                    {
                        DutyUtility.DefendPoint(pawn, closestGuardSpot);
                    }
                }
                else
                {
                    pawn.mindState.duty = new PawnDuty(DutyDefOf.Travel, Info.AverageLodgeTentPos)
                    {
                        locomotion = LocomotionUrgency.Sprint
                    };
                }

            }
        }


        private Pawn RandomCarnyByHealth()
        {
            Pawn pawn = null;
            Info.pawnsWithRole[CarnivalRole.Entertainer]
                .Concat(Info.pawnsWithRole[CarnivalRole.Vendor])
                .Where(p => !p.Dead && p.health.summaryHealth.SummaryHealthPercent < 0.95f)
                .TryRandomElementByWeight(p => 1f / p.health.summaryHealth.SummaryHealthPercent, out pawn);

            return pawn;
        }

    }
}
