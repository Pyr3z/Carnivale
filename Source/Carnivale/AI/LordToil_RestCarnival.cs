using Verse;
using RimWorld;
using System;
using System.Linq;

namespace Carnivale
{
    public class LordToil_RestCarnival : LordToil_Carn
    {
        // FIELDS + PROPERTIES //

        


        // CONSTRUCTORS //

        public LordToil_RestCarnival() { }


        // OVERRIDE METHODS //

        public override void UpdateAllDuties()
        {
            int curHour = GenLocalDate.HourInteger(Map);

            foreach (var pawn in lord.ownedPawns)
            {
                CarnivalRole role = pawn.GetCarnivalRole();

                if (role.Is(CarnivalRole.Worker))
                {
                    DutyUtility.MeanderAndHelp(pawn, Info.setupCentre, Info.baseRadius);
                    continue;
                }

                if (role.IsAny(CarnivalRole.Entertainer, CarnivalRole.Vendor)
                    && curHour > 22)
                {
                    DutyUtility.ForceRest(pawn);
                    continue;
                }

                if (role.Is(CarnivalRole.Guard))
                {
                    if (pawn.needs.rest.CurCategory == RestCategory.Rested)
                    {
                        IntVec3 spot = IntVec3.Invalid;
                        if (!Info.rememberedPositions.TryGetValue(pawn, out spot))
                        {
                            foreach (var guardSpot in Info.guardPositions)
                            {
                                if (!Info.rememberedPositions.Values.Any(c => guardSpot == c))
                                {
                                    Info.rememberedPositions.Add(pawn, spot);
                                    spot = guardSpot;
                                    break;
                                }
                            }
                        }

                        if (spot != null && spot.IsValid)
                        {
                            DutyUtility.GuardSmallArea(pawn, spot, 8);
                            continue;
                        }
                    }
                    else
                    {
                        DutyUtility.MeanderAndHelp(pawn, Info.AverageLodgeTentPos, 25f);
                        continue;
                    }
                }

                if (!role.Is(CarnivalRole.Carrier))
                {
                    DutyUtility.Meander(pawn, Info.setupCentre, Info.baseRadius);
                    continue;
                }

            }
        }


        private void SwitchGuardDuties(Pawn guard, Pawn newGuard)
        {
            // TODO
        }

    }
}
