using Verse;

namespace Carnivale
{
    public class JobDriver_PlayHighStriker : JobDriver_PlayCarnGame
    {
        private const int RingerJumpInterval = 650;

        protected override void WatchTickAction()
        {
            if (this.pawn.IsHashIntervalTick(RingerJumpInterval))
            {
                // Make ringer jump

            }

            base.WatchTickAction();
        }

        protected override void GetPrizeInitAction()
        {
            
        }

    }
}
