using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI.Group;

namespace Carnivale
{
    public class LordToilData_SetupCarnival : LordToilData
    {
        public List<Thing> availableCrates = new List<Thing>();

        public List<Blueprint> blueprints = new List<Blueprint>();

        public CarnivalInfo Info
        {
            get
            {
                return CarnUtils.Info;
            }
        }


        public LordToilData_SetupCarnival() { }


        public bool TryHaveWorkerCarry(Thing thing)
        {
            Pawn worker;
            // Try give pre-designated worker a thing, 4 attempts
            for (int i = 0; i < 4; i++)
            {
                if (Info.pawnsWithRole[CarnivalRole.Worker].TryRandomElement(out worker))
                {
                    if (worker.carryTracker.TryStartCarry(thing))
                    {
                        availableCrates.Add(thing);
                        return true;
                    }
                }
            }

            // Failing that, try giving anyone else a thing, 6 attempts
            for (int i = 0; i < 6; i++)
            {
                if (Info.pawnsWithRole[CarnivalRole.Any].TryRandomElement(out worker))
                {
                    if (worker.Is(CarnivalRole.Carrier)
                        || worker.story != null && !worker.story.WorkTypeIsDisabled(WorkTypeDefOf.Construction))
                    {
                        if (worker.carryTracker.TryStartCarry(thing))
                        {
                            availableCrates.Add(thing);
                            return true;
                        }
                    }
                }
            }

            // Failing that, try spawning the thing in the centre of the setup area
            if (GenPlace.TryPlaceThing(thing, Info.setupCentre, Info.map, ThingPlaceMode.Near, null))
            {
                availableCrates.Add(thing);
                return true;
            }

            // You failed me.
            Log.Error("Found no suitable pawn or place to drop " + thing + ". Construction may halt.");

            return false;
        }

        public int TryHaveWorkerCarry(ThingDef def, int count, ThingDef stuff = null)
        {
            // Returns how many things were successfully given.
            int result = 0;

            while (count > 0)
            {
                Thing newThing = ThingMaker.MakeThing(def, stuff);

                if (def.stackLimit > 1)
                {
                    int div = count / def.stackLimit;
                    if (div == 0)
                        newThing.stackCount = count;
                    else
                        newThing.stackCount = def.stackLimit;

                    count -= newThing.stackCount;
                }
                else
                {
                    count--;
                }

                newThing.SetForbidden(true);

                if (TryHaveWorkerCarry(newThing))
                {
                    result++;
                }
            }

            return result;
        }


        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                // Clean up unusable elements in collections
                this.availableCrates.RemoveAll(b => b.Destroyed);
                this.blueprints.RemoveAll(b => b.Destroyed);
            }

            Scribe_Collections.Look(ref this.availableCrates, "availableCrates", LookMode.Reference);

            Scribe_Collections.Look(ref this.blueprints, "blueprints", LookMode.Reference);
        }
    }
}