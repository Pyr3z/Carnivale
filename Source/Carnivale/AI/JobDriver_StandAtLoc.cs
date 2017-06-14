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

            // Stand
            Toil stand = new Toil();
            stand.initAction = delegate
            {
                stand.actor.pather.StopDead();
                stand.actor.Rotation = Rot4.South;
            };
            stand.tickAction = delegate
            {
                // Rotate randomly if carrier
                if (Find.TickManager.TicksGame % 300 == 0
                    && Type.Is(CarnivalRole.Carrier)
                    && Rand.Chance(0.6f))
                {
                    stand.actor.Rotation = Rot4.Random;
                }
            };
            stand.defaultDuration = 1000;
            stand.defaultCompleteMode = ToilCompleteMode.Delay;
            yield return stand;
        }

    }
}
