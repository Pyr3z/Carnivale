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

            if (this.resourceContainer.Any)
            {
                ThingDef stuff = resourceContainer[0].Stuff;

                this.SetStuffDirect(stuff);

                Thing dummyThingToSatisfyTheGods = ThingMaker.MakeThing(stuff);

                this.resourceContainer.TryAdd(dummyThingToSatisfyTheGods);

                ticked = true;
            }
        }

    }
}
