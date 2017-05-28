using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Carnivale.AI
{
    public class LordToil_SetupCarnival : LordToil
    {
        private Dictionary<Pawn, DutyDef> rememberedDuties = new Dictionary<Pawn, DutyDef>();

        private LordToilData_Carnival Data
        { get { return (LordToilData_Carnival)this.data; } }

        private IEnumerable<Frame> Frames
        {
            get
            {
                List<Thing> framesList = Map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingFrame);
                foreach (var thing in framesList)
                {
                    Frame frame = thing as Frame;
                    if (frame.Faction == this.lord.faction)
                        yield return frame;
                }
            }
        }


        public LordToil_SetupCarnival(IntVec3 setupSpot, float blueprintPoints)
        {
            this.data = new LordToilData_Carnival()
            {
                setupSpot = setupSpot,
                blueprintPoints = blueprintPoints
            };
        }



        public override void Init()
        {
            throw new NotImplementedException();
        }



        public override void UpdateAllDuties()
        {
            throw new NotImplementedException();
        }



        public override void LordToilTick()
        {
            throw new NotImplementedException();
        }



        public override void Cleanup()
        {
            throw new NotImplementedException();
        }
    }
}
