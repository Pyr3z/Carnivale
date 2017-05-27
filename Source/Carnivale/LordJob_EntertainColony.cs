using System;
using Verse.AI.Group;
using RimWorld;
using Verse;

namespace Carnivale
{
    public class LordJob_EntertainColony : LordJob
    {
        private Faction faction;

        private IntVec3 setupSpot;

        private LordJob_EntertainColony() { }

        public LordJob_EntertainColony(Faction faction, IntVec3 setupSpot)
        {
            this.faction = faction;
            this.setupSpot = setupSpot;
        }


        public override StateGraph CreateGraph()
        {
            throw new NotImplementedException();
        }
    }
}
