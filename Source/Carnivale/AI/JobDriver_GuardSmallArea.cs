using System;
using System.Collections.Generic;
using Verse.AI;

namespace Carnivale
{
    public class JobDriver_GuardSmallArea : JobDriver
    {

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Go to cell
            Toil gotoCell = Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
            yield return gotoCell;

            // Giving up on this, using JobGiver_WanderNearDutyLocation instead
        }

    }
}
