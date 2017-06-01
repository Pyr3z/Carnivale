using RimWorld;
using Verse;

namespace Carnivale
{
    public class Frame_Tent : Frame
    {
        [Unsaved]
        private bool ticked = false;

        public override void Tick()
        {
            // Shouldn't cause performance issues;
            // frames will not typically exist long.
            // This is super hacky nevertheless.

            if (ticked) return;

            this.SetStuffDirect(resourceContainer[0].Stuff);

            ticked = true;
        }

    }
}
