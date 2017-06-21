using Verse;
using Verse.AI.Group;

namespace Carnivale
{
    public class LordToil_RestCarnival : LordToil_Carn
    {
        // FIELDS + PROPERTIES //

        


        // CONSTRUCTORS //

        public LordToil_RestCarnival() { }

        public LordToil_RestCarnival(CarnivalInfo info)
        {
            this.data = new LordToilData_Carnival(info);
        }


        // OVERRIDE METHODS //

        public override void UpdateAllDuties()
        {
            foreach (var pawn in lord.ownedPawns)
            {
                CarnivalRole role = pawn.GetCarnivalRole();

                if (role.Is(CarnivalRole.Worker))
                {
                    DutyUtility.MeanderAndHelp(pawn, Info.setupCentre, Info.baseRadius);
                    continue;
                }

                if (!role.Is(CarnivalRole.Carrier))
                {
                    DutyUtility.Meander(pawn, Info.setupCentre, Info.baseRadius);
                    continue;
                }

            }
        }


        public override void LordToilTick()
        {
            base.LordToilTick();
        }


        private void SwitchGuardDuties(Pawn guard, Pawn newGuard)
        {

        }

    }
}
