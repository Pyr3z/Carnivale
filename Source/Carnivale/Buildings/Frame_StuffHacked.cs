using RimWorld;
using Verse;

namespace Carnivale
{
    public class Frame_StuffHacked : Frame
    {
        [Unsaved]
        private bool lordToilDataHacked = false;

        [Unsaved]
        private bool stuffHacked = false;

        public override void Tick()
        {
            // Shouldn't cause performance issues;
            // frames will not typically exist long.
            // This is super hacky nevertheless.

            if (stuffHacked && lordToilDataHacked) return;

            if (this.resourceContainer.Any)
            {
                if (!stuffHacked)
                {
                    ThingDef stuff = resourceContainer[0].Stuff;

                    this.SetStuffDirect(stuff);

                    Thing dummyThingToSatisfyTheGods = ThingMaker.MakeThing(stuff);

                    this.resourceContainer.TryAdd(dummyThingToSatisfyTheGods);

                    stuffHacked = true;
                }

                if (this.factionInt != Faction.OfPlayer)
                {
                    LordToilData_Carnival data = (LordToilData_Carnival)this.Map.lordManager.lords.FindLast(l => l.faction == this.factionInt).CurLordToil.data;

                    if (data != null)
                    {
                        foreach (Thing crate in resourceContainer)
                        {
                            data.availableCrates.Remove(crate);
                        }
                    }

                    if (workDone != 0f)
                    {
                        lordToilDataHacked = true;
                    }
                }
                else
                {
                    lordToilDataHacked = true;
                }
            }
        }

    }
}
