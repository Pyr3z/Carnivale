using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Carnivale
{
    public class JobDriver_WanderCarnival : JobDriver
    {
        [Unsaved]
        private CarnivalInfo infoInt = null;

        private CarnivalInfo Info
        {
            get
            {
                if (infoInt == null)
                {
                    infoInt = Map.GetComponent<CarnivalInfo>();
                }

                return infoInt;
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {


            yield break;
        }
    }
}
