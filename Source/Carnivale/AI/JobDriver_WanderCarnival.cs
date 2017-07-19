using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Carnivale
{
    public class JobDriver_WanderCarnival : JobDriver
    {
        private CarnivalInfo Info
        {
            get
            {
                return Utilities.CarnivalInfo;
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {


            yield break;
        }
    }
}
