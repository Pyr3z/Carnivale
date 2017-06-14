using System.Linq;
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

            TryGiveAnnouncerPosition();
        }


        public override void UpdateAllDuties()
        {
            int countCarriers = Info.pawnsWithRole[CarnivalRole.Carrier].Count;
            IntVec3 pos;
            foreach (Pawn pawn in this.lord.ownedPawns)
            {
                CarnivalRole pawnRole = pawn.GetCarnivalRole();
                if (pawnRole.Is(CarnivalRole.Entertainer))
                {
                    if (Info.rememberedPositions.TryGetValue(pawn, out pos))
                    {
                        DutyUtility.HitchToSpot(pawn, pos);
                    }
                    else
                    {
                        DutyUtility.Meander(pawn, Info.setupCentre, Info.baseRadius);
                    }
                }
                else if (pawnRole.IsAny(CarnivalRole.Vendor, CarnivalRole.Carrier))
                {
                    if (Info.rememberedPositions.TryGetValue(pawn, out pos))
                    {
                        DutyUtility.HitchToSpot(pawn, pos);
                    }
                    else
                    {
                        DutyUtility.HitchToSpot(pawn, pawn.Position);
                    }
                }
                else if (pawnRole.Is(CarnivalRole.Guard))
                {
                    if (Info.rememberedPositions.TryGetValue(pawn, out pos))
                    {
                        DutyUtility.GuardSmallArea(pawn, pos, countCarriers);
                    }
                    else
                    {
                        DutyUtility.Meander(pawn, Info.setupCentre, Info.baseRadius);
                    }
                }
                else
                {
                    DutyUtility.Meander(pawn, Info.setupCentre, Info.baseRadius);
                }
            }
        }



        private bool TryGiveAnnouncerPosition()
        {
            Pawn announcer;

            if (!(from p in lord.ownedPawns
                  where p.story != null && p.story.adulthood != null
                    && p.story.adulthood.TitleShort == "Announcer"
                    && !Info.rememberedPositions.ContainsKey(p)
                  select p).TryRandomElement(out announcer))
            {
                // If no pawns have the announcer backstory
                if (!Info.pawnsWithRole[CarnivalRole.Entertainer].TryRandomElement(out announcer))
                {
                    // No entertainers either
                    return false;
                }
            }

            IntVec3 offset = new IntVec3(-1, 0, -2);

            Info.rememberedPositions.Add(announcer, Info.bannerCell + offset);

            return true;
        }

    }
}
