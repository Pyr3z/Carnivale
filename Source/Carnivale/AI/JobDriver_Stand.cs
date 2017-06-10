using System;
using System.Collections.Generic;
using Verse.AI;

namespace Carnivale.AI
{
    public class JobDriver_Stand : JobDriver
    {
        public override string GetReport()
        {
            // TODO: translations
            if (this.pawn.Is(CarnivalRole.Vendor))
            {
                return "peddling goods.";
            }
            else
            {
                return "resting in place.";
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            throw new NotImplementedException();
        }
    }
}
