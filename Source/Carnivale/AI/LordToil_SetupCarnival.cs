using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Carnivale.AI
{
    public class LordToil_SetupCarnival : LordToil
    {
        private const float RADIUS_MIN = 14f;

        private const float RADIUS_MAX = 25f;


        private Dictionary<Pawn, DutyDef> rememberedDuties = new Dictionary<Pawn, DutyDef>();


        public LordToilData_Carnival Data
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


        public LordToil_SetupCarnival(IntVec3 setupSpot, HashSet<Pawn> workersWithCrates, HashSet<Thing> availableCrates)
        {
            this.data = new LordToilData_Carnival()
            {
                setupSpot = setupSpot,
                workersWithCrates = workersWithCrates,
                availableCrates = availableCrates
            };
        }



        public override void Init()
        {
            base.Init();

            LordToilData_Carnival data = Data; // more efficient to cast once

            data.baseRadius = Mathf.InverseLerp(RADIUS_MIN, RADIUS_MAX, this.lord.ownedPawns.Count / 50f);
            data.baseRadius = Mathf.Clamp(data.baseRadius, RADIUS_MIN, RADIUS_MAX);

            IEnumerable<Blueprint_Build> blueprints = BlueprintPlacer.PlaceCarnivalBlueprints(data.setupSpot, base.Map, this.lord.faction, data.availableCrates);

            foreach (var bp in blueprints)
            {

            }

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
