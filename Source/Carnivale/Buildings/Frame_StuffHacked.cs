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
                    int index = resourceContainer.FirstIndexOf(t => t.Stuff != null);

                    if (index < resourceContainer.Count)
                    {
                        ThingDef stuff = resourceContainer[index].Stuff;

                        this.SetStuffDirect(stuff);

                        Thing dummyThingToSatisfyTheGods = ThingMaker.MakeThing(stuff);

                        this.resourceContainer.TryAdd(dummyThingToSatisfyTheGods);

                        stuffHacked = true;
                    }
                }

                if (!lordToilDataHacked && this.factionInt != Faction.OfPlayer)
                {
                    LordToilData_Setup data = this.MapHeld.lordManager.lords.FindLast(l => l.faction == this.factionInt).CurLordToil.data as LordToilData_Setup;

                    if (data != null)
                    {
                        foreach (Thing crate in resourceContainer)
                        {
                            data.availableCrates.Remove(crate);
                        }
                    }

                    if (workDone != 0f)
                    {
                        // Work is being done -> all resources are present
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
