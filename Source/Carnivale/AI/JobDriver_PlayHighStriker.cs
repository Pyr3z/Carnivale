using RimWorld;
using Verse;

namespace Carnivale
{
    public class JobDriver_PlayHighStriker : JobDriver_PlayCarnGame
    {
        private static Effecter strikeEffect = EffecterDefOf.Mine.Spawn();

        private const int RingerJumpInterval = 350;

        protected override void WatchTickAction()
        {
            if (this.pawn.IsHashIntervalTick(RingerJumpInterval))
            {
                // Do strike effect
                strikeEffect.Trigger(pawn, new TargetInfo(pawn.Position + IntVec3.East, Map));
                
                // Make ringer jump

            }

            base.WatchTickAction();
        }

        protected override void GetPrizeInitAction()
        {
            
        }

    }
}
