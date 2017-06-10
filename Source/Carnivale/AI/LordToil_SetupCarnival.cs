using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Carnivale
{
    public class LordToil_SetupCarnival : LordToil
    {
        private const float RADIUS_MIN = 25f;

        private const float RADIUS_MAX = 50f;

        public LordToilData_Carnival Data
        { get { return (LordToilData_Carnival)this.data; } }

        public override IntVec3 FlagLoc
        {
            get
            {
                return Data.setupSpot;
            }
        }

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

            LordToilData_Carnival data = Data; // more efficient to cast once?

            // Set radius for carnies to stick to
            data.baseRadius = Mathf.InverseLerp(RADIUS_MIN, RADIUS_MAX, this.lord.ownedPawns.Count / 50f);
            data.baseRadius = Mathf.Clamp(data.baseRadius, RADIUS_MIN, RADIUS_MAX);

            // Cache pawn roles (somewhat inefficient memory use, I know...)
            foreach (CarnivalRole role in Enum.GetValues(typeof(CarnivalRole)))
            {
                List<Pawn> pawns = (from p in this.lord.ownedPawns
                                    where p.Is(role)
                                    select p).ToList();
                data.pawnsWithRole.Add(role, pawns);
            }


            int numCarnies = this.lord.ownedPawns.Count - data.pawnsWithRole[CarnivalRole.Carrier].Count;
            
            // Give workers tents (currently 8 carnies per tent), manager gets own tent
            int numBedTents = numCarnies > 9 ? Mathf.CeilToInt(numCarnies / 8f) : 1;

            Thing tentCrate;
            for (int i = 0; i < numBedTents; i++)
            {
                tentCrate = ThingMaker.MakeThing(_DefOf.Carn_Crate_TentLodge, GenStuff.RandomStuffFor(_DefOf.Carn_Crate_TentLodge));
                // Makes them carry them instead of just having them in inventory:
                data.TryHaveWorkerCarry(tentCrate);
            }

            if (data.pawnsWithRole[CarnivalRole.Manager].Count > 0)
            {
                tentCrate = ThingMaker.MakeThing(_DefOf.Carn_Crate_TentMan, _DefOf.DevilstrandCloth);
                if (!data.TryHaveWorkerCarry(tentCrate))
                {
                    // If no workers to carry it, force the manager to carry it
                    data.pawnsWithRole[CarnivalRole.Manager].First().carryTracker.TryStartCarry(tentCrate);
                    data.availableCrates.Add(tentCrate);
                }
            }

            // Place blueprints
            data.blueprints = BlueprintPlacer.PlaceCarnivalBlueprints(data.setupSpot, (int)(data.baseRadius / 1.5f), base.Map, this.lord.faction, data.availableCrates).ToList();


            // Find spots for carriers to chill
            TryFindCarrierSpots();
            

        }



        public override void UpdateAllDuties()
        {
            LordToilData_Carnival data = this.Data;

            int carrierIndex = 0;
            foreach (Pawn pawn in this.lord.ownedPawns)
            {
                if (pawn.Is(CarnivalRole.Worker))
                {
                    DutyUtility.SetAsBuilder(pawn, data.setupSpot, data.baseRadius);
                    continue;
                }

                if (pawn.Is(CarnivalRole.Carrier))
                {
                    IntVec3 spot = data.carrierSpots[carrierIndex++];

                    DutyUtility.SetAsStander(pawn, spot);
                    continue;
                }

                DutyUtility.SetAsIdler(pawn, this.lord.ownedPawns);
            }
        }



        public override void LordToilTick()
        {
            base.LordToilTick();

            // Check if everything is setup
            if (this.lord.ticksInToil % 400 == 0)
            {
                if (!(from frame in this.Frames
                      where !frame.Destroyed
                      select frame).Any())
                {
                    LordToilData_Carnival data = this.Data;

                    if (!(from blue in data.blueprints
                          where !blue.Destroyed
                          select blue).Any())
                    {
                        if (!base.Map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial).Any(b => b.Faction == this.lord.faction))
                        {
                            // No frames, blueprints, OR buildings
                            if ((from crate in data.availableCrates
                                 where !crate.Destroyed
                                 select crate).Any())
                            {
                                // Some more available crates, place new blueprints
                                data.blueprints = BlueprintPlacer.PlaceCarnivalBlueprints(data.setupSpot, (int)(data.baseRadius / 1.5f), base.Map, this.lord.faction, data.availableCrates).ToList();
                                this.UpdateAllDuties();
                                return;
                            }
                            else
                            {
                                // Nothing is buildable. Was the carnival attacked?
                                this.lord.ReceiveMemo("NothingBuildable");
                                return;
                            }
                        }
                        else
                        {
                            // Buildings are there. Next toil.
                            this.lord.ReceiveMemo("SetupDone");
                            return;
                        }
                    }
                }
            }
            // End check

        }

        public override void Notify_PawnLost(Pawn victim, PawnLostCondition cond)
        {
            if (cond == PawnLostCondition.IncappedOrKilled
                || cond == PawnLostCondition.MadePrisoner)
            {
                // Hostile actions
                this.lord.ReceiveMemo("HostileConditions");
                return;
            }

            // Non-hostile cause of PawnLost
            this.UpdateAllDuties();
        }


        public override void Notify_ConstructionFailed(Pawn pawn, Frame frame, Blueprint_Build newBlueprint)
        {
            if (frame.Faction == this.lord.faction && newBlueprint != null)
            {
                this.Data.blueprints.Add(newBlueprint);
            }
        }


        public override void Cleanup()
        {
            // Do more cleanup here?
            Data.blueprints.RemoveAll(blue => blue.Destroyed);
            foreach (Blueprint b in Data.blueprints)
            {
                b.Destroy(DestroyMode.Cancel);
            }
            foreach (Frame frame in this.Frames)
            {
                frame.Destroy(DestroyMode.Cancel);
            }
        }


        public bool TryFindCarrierSpots()
        {
            // Should only be called right after blueprint creation

            LordToilData_Carnival data = this.Data;

            int countCarriers = data.pawnsWithRole[CarnivalRole.Carrier].Count;
            int countSpots = 0;
            CellRect rect = CellRect.CenteredOn(data.setupSpot, (int)(data.baseRadius / 2f));

            for (int i = 0; i < 50; i++)
            {
                // Try to find initial spot
                IntVec3 randomCell = rect.RandomCell;
                if (Map.reachability.CanReach(randomCell, data.setupSpot, PathEndMode.OnCell, TraverseMode.NoPassClosedDoors, Danger.Deadly)
                    && randomCell.GetEdifice(Map) == null)
                {
                    data.carrierSpots.Add(randomCell);
                    countSpots++;
                    break;
                }
            }

            byte attempts = 0;
            byte directionalTries = 0;
            while (attempts < 75 && countSpots < countCarriers)
            {
                IntVec3 lastSpot = data.carrierSpots.Last();
                IntVec3 newSpot = new IntVec3(lastSpot.x, lastSpot.y, lastSpot.z);
                IntVec3 offset;

                switch (directionalTries)
                {
                    case 0:
                        offset = IntVec3.East * 2;
                        break;
                    case 1:
                        offset = IntVec3.West * 2;
                        break;
                    case 2:
                        offset = IntVec3.North * 2;
                        break;
                    case 3:
                        offset = IntVec3.South * 2;
                        break;
                    default:
                        offset = IntVec3.West * 2;
                        break;
                }

                newSpot += offset;

                if (!data.carrierSpots.Contains(newSpot)
                    && Map.reachability.CanReach(newSpot, data.setupSpot, PathEndMode.OnCell, TraverseMode.NoPassClosedDoors, Danger.Deadly))
                {
                    // Spot found
                    data.carrierSpots.Add(newSpot);
                    countSpots++;
                }
                else if (directionalTries < 3)
                {
                    // No spot, increment direction to try
                    directionalTries++;
                }
                else
                {
                    // No spot, reset direction to try and find new starting spot
                    for (int i = 0; i < 50; i++)
                    {
                        IntVec3 randomCell = rect.RandomCell;
                        if (Map.reachability.CanReach(randomCell, data.setupSpot, PathEndMode.OnCell, TraverseMode.NoPassClosedDoors, Danger.Deadly)
                            && randomCell.GetEdifice(Map) == null)
                        {
                            data.carrierSpots.Add(randomCell);
                            countSpots++;
                            break;
                        }
                    }
                    directionalTries = 0;
                }

                // Whether or not a spot was found, increment attempts to avoid infinite looping
                attempts++;
            }

            if (countSpots == countCarriers)
            {
                return true;
            }
            else
            {
                Log.Error("Found no spots for carnival carriers to chill. Idling them instead.");
                return false;
            }
        }


    }
}
