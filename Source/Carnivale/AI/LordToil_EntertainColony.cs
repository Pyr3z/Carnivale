using Verse;
using Verse.AI.Group;

namespace Carnivale
{
    public class LordToil_EntertainColony : LordToil_Carn
    {
        // FIELDS + PROPERTIES //




        // CONSTRUCTORS //

        public LordToil_EntertainColony() { }

        public LordToil_EntertainColony(LordToilData_Carnival data)
        {
            // Need to clone the data structure?
            this.data = data;
        }


        // OVERRIDE METHODS //

        public override void Init()
        {
            base.Init();
        }

        public override void UpdateAllDuties()
        {
            foreach (Pawn pawn in this.lord.ownedPawns)
            {
                switch (pawn.GetCarnivalRole())
                {
                    case CarnivalRole.Entertainer:

                        break;
                    case CarnivalRole.Vendor:
                        
                        break;
                    default:

                        break;
                }
            }
        }
    }
}
