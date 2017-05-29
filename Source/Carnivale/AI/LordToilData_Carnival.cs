using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI.Group;

namespace Carnivale.AI
{
    public class LordToilData_Carnival : LordToilData
    {
        public IntVec3 setupSpot;

        public float baseRadius;

        public List<Blueprint> blueprints = new List<Blueprint>();

        public List<Thing> unbuiltThings = new List<Thing>();


        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.setupSpot, "setupSpot", default(IntVec3), false);
            Scribe_Values.Look(ref this.baseRadius, "baseRadius", 0f, false);
            
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                this.blueprints.RemoveAll(b => b.Destroyed);
            }
            Scribe_Collections.Look(ref this.blueprints, "blueprints", LookMode.Reference, new object[0]);

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                this.blueprints.RemoveAll(b => b.Destroyed);
            }
            Scribe_Collections.Look(ref this.unbuiltThings, "unbuiltThings", LookMode.Reference, new object[0]);
        }
    }
}
