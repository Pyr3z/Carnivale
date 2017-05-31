using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Carnivale
{
    public class Frame_Tent : Frame
    {
        [Unsaved]
        private bool ticked = false;

        public override void Tick()
        {
            if (ticked) return;

            this.SetStuffDirect(resourceContainer[0].Stuff);

            ticked = true;
        }

    }
}
