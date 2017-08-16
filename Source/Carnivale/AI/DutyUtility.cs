using RimWorld;
using Verse;
using Verse.AI;

namespace Carnivale
{
    public static class DutyUtility
    {
        public static void AttendShow(Pawn pawn, IntVec3 reservedSpot, IntVec3 focus)
        {
            pawn.mindState.duty = new PawnDuty(_DefOf.Duty_StandAtLoc)
            {
                focus = reservedSpot,
                focusSecond = focus
            };
        }

        public static void BuildCarnival(Pawn pawn, IntVec3 centre, float radius)
        {
            pawn.mindState.duty = new PawnDuty(_DefOf.Duty_BuildCarnival, centre, radius);

            pawn.workSettings.EnableAndInitialize();

            pawn.workSettings.SetPriority(WorkTypeDefOf.Construction, 1);

            if (!pawn.story.WorkTypeIsDisabled(_DefOf.PlantCutting)
                && pawn.skills.GetSkill(SkillDefOf.Growing).Level > 1)
                pawn.workSettings.SetPriority(_DefOf.PlantCutting, 2);
        }

        public static void ChargeHostiles(Pawn pawn)
        {
            pawn.mindState.duty = new PawnDuty(_DefOf.Duty_ChargeNearestHostile);
        }

        public static void DefendPoint(Pawn pawn, LocalTargetInfo defendPoint, Thing target = null, float defendRadius = -1f)
        {
            if (pawn.equipment.Primary != null && defendPoint.IsValid)
            {
                if (defendRadius == -1f)
                {
                    defendRadius = CarnUtils.Info.baseRadius;
                }

                if (!pawn.equipment.Primary.def.IsMeleeWeapon)
                {
                    pawn.mindState.duty = new PawnDuty(_DefOf.Duty_DefendRanged, defendPoint)
                    {
                        focusSecond = defendPoint,
                        radius = defendRadius,
                        locomotion = LocomotionUrgency.Sprint
                    };
                }
                else
                {
                    pawn.mindState.duty = new PawnDuty(_DefOf.Duty_DefendMelee, defendPoint)
                    {
                        focusSecond = defendPoint,
                        radius = defendRadius / 3,
                        locomotion = LocomotionUrgency.Sprint
                    };
                    pawn.mindState.enemyTarget = target;
                }
            }
        }

        public static void EntertainShow(Pawn pawn, IntVec3 spot, IntVec3 facing)
        {
            pawn.mindState.duty = new PawnDuty(_DefOf.Duty_StandAtLoc)
            {
                focus = spot,
                focusSecond = facing
            };
        }

        public static void ForceRest(Pawn pawn)
        {
            pawn.needs.rest.CurLevel = 0.35f; //hacky?
            pawn.mindState.duty = new PawnDuty(_DefOf.Duty_ForceRest);
        }

        public static void GuardCircuit(Pawn pawn)
        {
            pawn.mindState.duty = new PawnDuty(_DefOf.Duty_GuardCircuit);
        }

        public static void HitchToSpot(Pawn pawn, IntVec3 spot)
        {
            pawn.mindState.duty = new PawnDuty(_DefOf.Duty_HitchToSpot, spot);
        }

        public static void LeaveMap(Pawn pawn, Pawn toFollow = null, LocomotionUrgency urgency = LocomotionUrgency.Walk)
        {
            if (toFollow != null && toFollow.Spawned)
            {
                pawn.mindState.duty = new PawnDuty(DutyDefOf.Follow, toFollow, 5f)
                {
                    locomotion = urgency
                };
            }
            else
            {
                pawn.mindState.duty = new PawnDuty(_DefOf.Duty_DefendAndExitMapRoadOrBest)
                {
                    radius = 18f,
                    locomotion = urgency
                };
            }
        }

        public static void LeaveMapAndEscort(Pawn pawn, Pawn escortee, LocomotionUrgency urgency = LocomotionUrgency.Walk)
        {
            if (escortee != null && escortee.Spawned)
            {
                pawn.mindState.duty = new PawnDuty(DutyDefOf.Escort, escortee, 14f)
                {
                    locomotion = urgency
                };
            }
            else
            {
                LeaveMap(pawn, null, urgency);
            }
        }

        public static void Meander(Pawn pawn, IntVec3 centre, float radius)
        {
            pawn.mindState.duty = new PawnDuty(_DefOf.Duty_Meander, centre, radius);
        }

        public static void MeanderAndHelp(Pawn pawn, IntVec3 centre, float radius)
        {
            pawn.mindState.duty = new PawnDuty(_DefOf.Duty_MeanderAndHelp, centre, radius);

            pawn.workSettings.EnableAndInitialize();

            if (!pawn.story.WorkTypeIsDisabled(_DefOf.PlantCutting)
                && pawn.skills.GetSkill(SkillDefOf.Growing).Level > 1)
                pawn.workSettings.SetPriority(_DefOf.PlantCutting, 1);
        }

        public static void StrikeBuildings(Pawn pawn)
        {
            pawn.mindState.duty = new PawnDuty(_DefOf.Duty_StrikeBuildings, CarnUtils.Info.setupCentre);
        }
    }
}
