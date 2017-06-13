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

        public LordToil_EntertainColony(LordToilData_Carnival data)
        {
            // Need to clone the data structure?
            this.data = data;
        }


        // OVERRIDE METHODS //

        public override void Init()
        {
            base.Init();

            // Calculate vendor positions
            // Deprecated! Spots are now assigned in one swoop when stall blueprints are placed
            // FindVendorSpots();



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
                        DutyUtility.HitchToSpot(pawn, Data.rememberedPositions[pawn]);
                        break;
                    default:
                        DutyUtility.Meander(pawn, Data.setupCentre);
                        break;
                }
            }
        }


        

        private void FindVendorSpots()
        {
            // Deprecated! Spots are now assigned in one swoop when stall blueprints are placed
            foreach (Pawn vendor in Data.pawnsWithRole[CarnivalRole.Vendor])
            {
                // Validation check
                if (vendor.TraderKind == null)
                {
                    Log.Warning("Detected a carny vendor without a TraderKind. What gives?");
                    Data.rememberedPositions.Add(vendor, Data.setupCentre);
                    continue;
                }

                ThingDef stallDefToFind;

                if (vendor.TraderKind == _DefOf.Carn_Trader_Food)
                {
                    stallDefToFind = _DefOf.Carn_StallFood;
                }
                else if (vendor.TraderKind == _DefOf.Carn_Trader_Curios)
                {
                    stallDefToFind = _DefOf.Carn_StallCurios;
                }
                else if (vendor.TraderKind == _DefOf.Carn_Trader_Surplus)
                {
                    stallDefToFind = _DefOf.Carn_StallSurplus;
                }
                else
                {
                    Log.Warning("Detected a carny vendor without a carnival TraderKind. WTF");
                    Data.rememberedPositions.Add(vendor, Data.setupCentre);
                    continue;
                }

                List<Thing> validStalls = Map.listerThings.ThingsOfDef(stallDefToFind).FindAll(s => s.Faction == this.lord.faction);

                IntVec3 pos = IntVec3.Invalid;
                foreach (Thing stall in validStalls)
                {
                    if (!Data.rememberedPositions.ContainsValue(stall.Position))
                    {
                        pos = stall.Position;
                        break;
                    }
                }

                if (pos.IsValid)
                {
                    // Bingo
                    Data.rememberedPositions.Add(vendor, pos);
                }
                else
                {
                    Log.Warning("Could not find a valid stall for " + vendor.NameStringShort);
                    Data.rememberedPositions.Add(vendor, Data.setupCentre);
                }
            }
        }


    }
}
