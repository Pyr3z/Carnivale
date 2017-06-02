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


        public LordToil_SetupCarnival(IntVec3 setupSpot)
        {
            LordToilData_Carnival dat = new LordToilData_Carnival(setupSpot);

            this.data = dat;
        }



        public override void Init()
        {
            base.Init();

            LordToilData_Carnival data = Data; // more efficient to cast once

            // Set radius for carnies to stick to
            data.baseRadius = Mathf.InverseLerp(RADIUS_MIN, RADIUS_MAX, this.lord.ownedPawns.Count / 50f);
            data.baseRadius = Mathf.Clamp(data.baseRadius, RADIUS_MIN, RADIUS_MAX);

            // Cache pawn roles (somewhat inefficient memory use, I know...)
            foreach (CarnivalRole role in Enum.GetValues(typeof(CarnivalRole)))
            {
                List<Pawn> pawns = (from p in this.lord.ownedPawns
                                    where p.Is(role)
                                    select p).ToList();
                data.pawnsWithRoles.Add(role, pawns);
            }


            int numCarnies = this.lord.ownedPawns.Count - data.pawnsWithRoles[CarnivalRole.Carrier].Count;
            
            // Give workers tents (currently 8 carnies per tent)
            int numBedTents = numCarnies > 8 ? Mathf.CeilToInt(numCarnies / 8f) : 1;

            Thing tentCrate;
            // one extra for manager
            for (int i = 0; i < numBedTents * _DefOf.Carn_TentMedBed.costList[0].count + 1; i++)
            {
                tentCrate = ThingMaker.MakeThing(_DefOf.Carn_Crate_TentFurn, GenStuff.RandomStuffFor(_DefOf.Carn_Crate_TentFurn));
                // Makes them carry them instead of just having them in inventory
                data.TryGiveRandomWorker(tentCrate);
            }

            // Place blueprints
            BlueprintPlacer.PlaceCarnivalBlueprints(data.setupSpot, base.Map, this.lord.faction, data.availableCrates);

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



        private void SetAsBuilder(Pawn p)
        {
            p.mindState.duty = new PawnDuty(DutyDefOf.Build, Data.setupSpot, Data.baseRadius);

            p.workSettings.EnableAndInitialize();
            p.workSettings.SetPriority(WorkTypeDefOf.Construction, 1);
        }

        private void SetAsIdler(Pawn p)
        {

        }

    }
}
