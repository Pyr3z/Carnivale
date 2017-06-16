using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Carnivale
{
    public class JobDriver_StandAtLoc : JobDriver
    {
        [Unsaved]
        private CarnivalRole typeInt = 0;

        private CarnivalRole Type
        {
            get
            {
                if (typeInt == 0)
                {
                    typeInt = this.pawn.GetCarnivalRole();
                }
                return typeInt;
            }
        }

        public override string GetReport()
        {
            // TODO: translations
            if (Type.Is(CarnivalRole.Vendor))
            {
                return "peddling goods.";
            }
            else if (Type.Is(CarnivalRole.Entertainer))
            {
                return "announcing.";
            }
            else
            {
                return "resting in place.";
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Go to cell
            Toil gotoCell = Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
            yield return gotoCell;

            // Stand, no rotation, vendors want to trade, motes
            Toil stand = new Toil();
            stand.initAction = delegate
            {
                stand.actor.pather.StopDead();
                stand.actor.Rotation = Rot4.South;
                if (Type.Is(CarnivalRole.Vendor))
                    stand.actor.mindState.wantsToTradeWithColony = true;
            };
            stand.tickAction = delegate
            {
                if (Find.TickManager.TicksGame % (GenDate.TicksPerHour / 4) == 0)
                {
                    // Throw motes

                }
            };
            stand.AddFinishAction(delegate
            {
                if (Type.Is(CarnivalRole.Vendor))
                    stand.actor.mindState.wantsToTradeWithColony = false;
            });
            stand.defaultDuration = GenDate.TicksPerHour;
            stand.defaultCompleteMode = ToilCompleteMode.Delay;
            if (!Type.Is(CarnivalRole.Carrier))
            {
                yield return stand;
                yield break;
            }



            // Carrier stand
            Toil standCarrier = new Toil();
            standCarrier.initAction = delegate
            {
                standCarrier.actor.pather.StopDead();
            };
            standCarrier.tickAction = delegate
            {
                // Rotate randomly if carrier
                if (Find.TickManager.TicksGame % 307 == 0
                    && Rand.Chance(0.4f))
                {
                    standCarrier.actor.Rotation = Rot4.Random;
                }
            };
            standCarrier.defaultDuration = GenDate.TicksPerHour;
            standCarrier.defaultCompleteMode = ToilCompleteMode.Delay;
            yield return standCarrier;
        }

    }
}
