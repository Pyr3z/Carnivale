using System.Collections.Generic;
using Verse;
using Verse.AI.Group;

namespace Carnivale
{
    public class LordToil_EntertainColony : LordToil_Carn
    {
        // FIELDS + PROPERTIES //



        // CONSTRUCTORS //

        public LordToil_EntertainColony() { }

        public LordToil_EntertainColony(CarnivalInfo info)
        {
            this.data = new LordToilData_Carnival(info);
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
                    case CarnivalRole.Carrier:
                        IntVec3 pos;
                        if (Info.rememberedPositions.TryGetValue(pawn, out pos))
                            DutyUtility.HitchToSpot(pawn, pos);
                        else
                            DutyUtility.HitchToSpot(pawn, pawn.Position);
                        break;
                    default:
                        DutyUtility.Meander(pawn, Info.setupCentre, Info.baseRadius);
                        break;
                }
            }
        }

    }
}
