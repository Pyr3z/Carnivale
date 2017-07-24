using RimWorld;
using Verse;

namespace Carnivale
{
    public class LordToil_StrikeCarnival : LordToil_Carn
    {
        // FIELDS + PROPERTIES //

        


        // CONSTRUCTORS //

        public LordToil_StrikeCarnival() { }


        // OVERRIDE METHODS //

        public override void UpdateAllDuties()
        {
            var guard = Info.pawnsWithRole[CarnivalRole.Guard].MinBy(p => p.skills.GetSkill(SkillDefOf.Construction).Level);

            DutyUtility.GuardCircuit(guard);

            foreach (var pawn in lord.ownedPawns)
            {
                CarnivalRole role = pawn.GetCarnivalRole();

                if (!role.Is(CarnivalRole.Carrier) && pawn != guard)
                {
                    DutyUtility.StrikeBuildings(pawn);
                }

            }
        }


        public override void LordToilTick()
        {
            if (lord.ticksInToil % 1013 == 0)
            {
                if (Info.carnivalBuildings.NullOrEmpty())
                {
                    Info.CheckForHaulables(true);

                    if (!Info.thingsToHaul.Any(t => t.DefaultHaulLocation(true) == HaulLocation.ToCarriers))
                    {
                        lord.ReceiveMemo("StrikeDone");
                    }
                }
            }
        }

    }
}
