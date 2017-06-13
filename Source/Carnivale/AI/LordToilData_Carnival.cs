using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI.Group;

namespace Carnivale
{
    public class LordToilData_Carnival : LordToilData
    {
        public LordToil currentLordToil;

        public IntVec3 setupCentre;

        public IntVec3 bannerCell;

        public float baseRadius;

        public CellRect carnivalArea;

        public Dictionary<CarnivalRole, DeepPawnList> pawnsWithRole = new Dictionary<CarnivalRole, DeepPawnList>();

        public Dictionary<Pawn, IntVec3> rememberedPositions = new Dictionary<Pawn, IntVec3>();

        public List<Thing> availableCrates = new List<Thing>();

        public List<Blueprint> blueprints = new List<Blueprint>();



        public LordToilData_Carnival() { }

        public LordToilData_Carnival(IntVec3 setupSpot)
        {
            this.setupCentre = setupSpot;
        }

        public LordToilData_Carnival Clone()
        {
            // Cloning might be necessary due to the way that
            // LordToilData is saved, and I want to be able to
            // use the same data structure for each toil.
            // Shrug. Maybe it's not necessary.

            // Also, no need to worry about not deep-cloning each field
            // (especally data structures like lists), because if they
            // are changed by one toil, the change should be the same in
            // the next toil.

            LordToilData_Carnival clone = new LordToilData_Carnival();
            clone.currentLordToil = this.currentLordToil;
            clone.setupCentre = this.setupCentre;
            clone.baseRadius = this.baseRadius;
            clone.pawnsWithRole = this.pawnsWithRole;
            clone.rememberedPositions = this.rememberedPositions;
            clone.availableCrates = this.availableCrates;
            clone.blueprints = this.blueprints;

            return clone;
        }


        public LordToilData_Carnival SetCurrentLordToil(LordToil cur)
        {
            this.currentLordToil = cur;
            return this;
        }




        public bool TryHaveWorkerCarry(Thing thing)
        {
            Pawn worker;
            // Try give pre-designated worker a thing, 4 attempts
            for (int i = 0; i < 4; i++)
            {
                if (pawnsWithRole[CarnivalRole.Worker].TryRandomElement(out worker))
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
                if (pawnsWithRole[CarnivalRole.Any].TryRandomElement(out worker))
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
            if (GenPlace.TryPlaceThing(thing, setupCentre, currentLordToil.Map, ThingPlaceMode.Near, null))
            {
                availableCrates.Add(thing);
                return true;
            }

            // You failed me.
            Log.Warning("Found no suitable pawn or place to drop " + thing + ". Construction may halt.");

            return false;
        }

        public int TryHaveWorkerCarry(ThingDef def, int count, ThingDef stuff = null)
        {
            // Returns how many things were successfully given.
            int result = 0;

            for (int i = 0; i < count; i++)
            {
                Thing newThing = ThingMaker.MakeThing(def, stuff);
                if (TryHaveWorkerCarry(newThing))
                {
                    result++;
                }
            }

            return result;
        }


        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.currentLordToil, "currentLordToil", default(LordToil), false);

            Scribe_Values.Look(ref this.setupCentre, "setupCentre", default(IntVec3), false);
            Scribe_Values.Look(ref this.bannerCell, "bannerCell", default(IntVec3), false);
            Scribe_Values.Look(ref this.baseRadius, "baseRadius", 0f, false);
            Scribe_Values.Look(ref this.carnivalArea, "carnivalArea", default(CellRect), false);

            Scribe_Collections.Look(ref this.pawnsWithRole, "pawnsWithRoles", LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look(ref this.rememberedPositions, "rememberedPositions", LookMode.Reference, LookMode.Value);


            //if (Scribe.mode == LoadSaveMode.Saving)
            //{
            //    this.workersWithCrates.RemoveAll(b => b.Destroyed);
            //}
            //Scribe_Collections.Look(ref this.workersWithCrates, "workersWithCrates", LookMode.Reference, new object[0]);

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                this.availableCrates.RemoveAll(b => b.Destroyed);
            }
            Scribe_Collections.Look(ref this.availableCrates, "availableCrates", LookMode.Reference, new object[0]);


            //Scribe_Collections.Look(ref this.workersWithCrates, false, "workersWithCrates", LookMode.Reference);

            //Scribe_Collections.Look(ref this.availableCrates, false, "availableCrates", LookMode.Reference);

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                this.blueprints.RemoveAll(b => b.Destroyed);
            }
            Scribe_Collections.Look(ref this.blueprints, "blueprints", LookMode.Reference, new object[0]);

            //Scribe_Collections.Look(ref this.carrierSpots, "carrierSpots", LookMode.Value, new object[0]);

            //if (Scribe.mode == LoadSaveMode.Saving)
            //{
            //    this.blueprints.RemoveAll(b => b.Destroyed);
            //}
            //Scribe_Collections.Look(ref this.unbuiltThings, "unbuiltThings", LookMode.Reference, new object[0]);
        }


    }
}
