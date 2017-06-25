using RimWorld;
using Verse;

namespace Carnivale
{
    public class Frame_StuffHacked : Frame
    {

        [Unsaved]
        private bool stuffHacked = false;

        public override void Tick()
        {
            // Shouldn't cause performance issues;
            // frames will not typically exist long.
            // This is super hacky nevertheless.

            if (stuffHacked) return;

            if (this.resourceContainer.Any)
            {
                int index = resourceContainer.FirstIndexOf(t => t.IsCrate());

                if (index < resourceContainer.Count)
                {
                    var stuff = resourceContainer[index].Stuff;

                    this.SetStuffDirect(stuff);

                    var dummyThingToSatisfyTheGods = ThingMaker.MakeThing(stuff);

                    this.resourceContainer.TryAdd(dummyThingToSatisfyTheGods);

                    stuffHacked = true;
                }
            }
        }

    }
}
