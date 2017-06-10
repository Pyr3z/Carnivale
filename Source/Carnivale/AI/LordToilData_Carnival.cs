using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI.Group;

namespace Carnivale
{
    public class LordToilData_Carnival : LordToilData
    {
        public IntVec3 setupSpot;

        public float baseRadius;

        public Dictionary<CarnivalRole, List<Pawn>> pawnsWithRole = new Dictionary<CarnivalRole, List<Pawn>>();

        public List<Thing> availableCrates = new List<Thing>();

        public List<Blueprint> blueprints = new List<Blueprint>();

        public List<IntVec3> carrierSpots = new List<IntVec3>();



        public LordToilData_Carnival(IntVec3 setupSpot)
        {
            this.setupSpot = setupSpot;
        }




        public bool TryHaveWorkerCarry(Thing thing)
        {
            Pawn worker;
            // Try give pre-designated worker a thing, 6 attempts
            for (int i = 0; i < 6; i++)
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

            // Failing that, try giving a guard a thing, 2 attempts
            for (int i = 0; i < 2; i++)
            {
                if (pawnsWithRole[CarnivalRole.Vendor].TryRandomElement(out worker))
                {
                    if (!worker.story.WorkTypeIsDisabled(WorkTypeDefOf.Construction))
                    {
                        if (worker.carryTracker.TryStartCarry(thing))
                        {
                            availableCrates.Add(thing);
                            return true;
                        }
                    }
                }
            }

            // You failed me.
            Log.Error("Found no suitable pawn to give " + thing + " to.");

            return false;
        }


        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.setupSpot, "setupSpot", default(IntVec3), false);
            Scribe_Values.Look(ref this.baseRadius, "baseRadius", 0f, false);

            Scribe_Collections.Look(ref pawnsWithRole, "pawnsWithRoles", LookMode.Reference);

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

            Scribe_Collections.Look(ref this.carrierSpots, "carrierSpots", LookMode.Reference, new object[0]);

            //if (Scribe.mode == LoadSaveMode.Saving)
            //{
            //    this.blueprints.RemoveAll(b => b.Destroyed);
            //}
            //Scribe_Collections.Look(ref this.unbuiltThings, "unbuiltThings", LookMode.Reference, new object[0]);
        }


    }
}
