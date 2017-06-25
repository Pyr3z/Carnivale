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

        public override void Init()
        {
            foreach (var building in Info.carnivalBuildings)
            {
                
            }
        }


        public override void UpdateAllDuties()
        {
            foreach (var pawn in lord.ownedPawns)
            {
                CarnivalRole role = pawn.GetCarnivalRole();

                if (!role.Is(CarnivalRole.Carrier))
                {
                    DutyUtility.StrikeBuildings(pawn);
                    continue;
                }

            }
        }


        public override void LordToilTick()
        {
            if (lord.ticksInToil % 743 == 0)
            {
                Info.CheckForHaulables();

                if (Info.carnivalBuildings.NullOrEmpty()
                    && !Info.thingsToHaul.Any(t => t.def.defName.StartsWith("Carn_Crate")))
                {
                    lord.ReceiveMemo("StrikeDone");
                }
            }
        }

    }
}
