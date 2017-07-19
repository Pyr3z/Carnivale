using Verse;
using RimWorld;
using System;
using System.Linq;
using UnityEngine;

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
            int numActiveGuards = Mathf.RoundToInt(Info.pawnsWithRole[CarnivalRole.Guard].Count / 2f);

            foreach (var pawn in lord.ownedPawns)
            {
                CarnivalRole role = pawn.GetCarnivalRole();

                if (role.Is(CarnivalRole.Guard))
                {
                    if (numActiveGuards > 0 && pawn.needs.rest.CurCategory == RestCategory.Rested)
                    {
                        DutyUtility.GuardCircuit(pawn);
                        numActiveGuards--;
                    }
                    else
                    {
                        // rest on the off shift if not assigned a position
                        DutyUtility.ForceRest(pawn);
                    }
                }
                else if (role.IsAny(CarnivalRole.Entertainer, CarnivalRole.Vendor)
                    && curHour >= 22)
                {
                    DutyUtility.ForceRest(pawn);
                }
                else if (role.Is(CarnivalRole.Worker))
                {
                    DutyUtility.MeanderAndHelp(pawn, Info.setupCentre, Info.baseRadius);
                }
                else if (!role.Is(CarnivalRole.Carrier))
                {
                    DutyUtility.Meander(pawn, Info.setupCentre, Info.baseRadius);
                }

            }
        }


        private void SwitchGuardDuties(Pawn guard, Pawn newGuard)
        {
            // TODO
        }

    }
}
