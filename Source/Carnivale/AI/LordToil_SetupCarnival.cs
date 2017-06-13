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
    public class LordToil_SetupCarnival : LordToil_Carn
    {
        // FIELDS + PROPERTIES //

        private const float RADIUS_MIN = 25f;

        private const float RADIUS_MAX = 50f;


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


        // CONSTRUCTORS //

        public LordToil_SetupCarnival() { }

        public LordToil_SetupCarnival(LordToilData_Carnival data)
        {
            this.data = data;
        }


        // OVERRIDE METHODS //

        public override void Init()
        {
            base.Init();

            Data.SetCurrentLordToil(this);

            // Set radius for carnies to stick to
            Data.baseRadius = Mathf.InverseLerp(RADIUS_MIN, RADIUS_MAX, this.lord.ownedPawns.Count / RADIUS_MAX);
            Data.baseRadius = Mathf.Clamp(Data.baseRadius, RADIUS_MIN, RADIUS_MAX);

            // Set carnival area
            Data.carnivalArea = CellRect.CenteredOn(Data.setupCentre, (int)Data.baseRadius).ClipInsideMap(Map);

            // Set banner spot
            Data.bannerCell = CalculateBannerCell(Map);

            // Cache pawn roles (somewhat inefficient memory use, I know...)
            foreach (CarnivalRole role in Enum.GetValues(typeof(CarnivalRole)))
            {
                List<Pawn> pawns = (from p in this.lord.ownedPawns
                                    where p.Is(role)
                                    select p).ToList();
                Data.pawnsWithRole.Add(role, pawns);
            }


            int numCarnies = this.lord.ownedPawns.Count - Data.pawnsWithRole[CarnivalRole.Carrier].Count;



            // Give workers tents (currently 8 carnies per tent), manager gets own tent

            int numBedTents = numCarnies > 9 ? Mathf.CeilToInt(numCarnies / 8f) : 1;

            bool errorFlag = Data.TryHaveWorkerCarry(_DefOf.Carn_Crate_TentLodge, numBedTents, Utilities.RandomSimpleFabricByValue()) != numBedTents;

            if (Data.pawnsWithRole[CarnivalRole.Manager].Any())
            {
                if (Data.TryHaveWorkerCarry(_DefOf.Carn_Crate_TentMan, 1, _DefOf.DevilstrandCloth) != 1)
                {
                    errorFlag = true;
                }
            }

            if (errorFlag)
            {
                Log.Warning("Could not give enough tent crates to workers of " + this.lord.faction);
            }




            // Give workers stalls + entry sign

            int numStallCrates = Data.pawnsWithRole[CarnivalRole.Vendor].Count + _DefOf.Carn_SignEntry.costList.First().count;
            Data.TryHaveWorkerCarry(_DefOf.Carn_Crate_Stall, numStallCrates, ThingDefOf.WoodLog);



            // Place blueprints //
            foreach (Blueprint bp in CarnivalBlueprints.PlaceCarnivalBlueprints(Data))
            {
                Data.blueprints.Add(bp);
            }



            // Find spots for carriers to chill //
            TryFindCarrierSpots();
            

        }



        public override void UpdateAllDuties()
        {
            foreach (Pawn pawn in this.lord.ownedPawns)
            {
                if (pawn.Is(CarnivalRole.Worker))
                {
                    DutyUtility.BuildCarnival(pawn, Data.setupCentre, Data.baseRadius);
                    continue;
                }

                if (pawn.Is(CarnivalRole.Carrier))
                {
                    IntVec3 pos = Data.rememberedPositions[pawn];

                    if (pos.IsValid)
                    {
                        DutyUtility.HitchToSpot(pawn, pos);
                    }
                    else
                    {
                        DutyUtility.HitchToSpot(pawn, pawn.Position);
                    }
                    
                    continue;
                }

                DutyUtility.Meander(pawn, Data.setupCentre);
            }
        }



        public override void LordToilTick()
        {
            base.LordToilTick();

            // Check if everything is setup
            if (this.lord.ticksInToil % 600 == 0)
            {
                if (!(from frame in this.Frames
                      where !frame.Destroyed
                      select frame).Any())
                {
                    if (!(from blue in Data.blueprints
                          where !blue.Destroyed
                          select blue).Any())
                    {
                        if (!base.Map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial).Any(b => b.Faction == this.lord.faction))
                        {
                            // No frames, blueprints, OR buildings
                            // Nothing is buildable. Was the carnival attacked?
                            this.lord.ReceiveMemo("NoBuildings");
                            return;
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
            //foreach (Blueprint b in Data.blueprints)
            //{
            //    b.Destroy(DestroyMode.Cancel);
            //}
            //foreach (Frame frame in this.Frames)
            //{
            //    frame.Destroy(DestroyMode.Cancel);
            //}
        }


        private bool TryFindCarrierSpots()
        {
            // Should only be called right after blueprint creation

            int countCarriers = Data.pawnsWithRole[CarnivalRole.Carrier].Count;
            int countSpots = 0;
            List<IntVec3> spots = new List<IntVec3>();
            CellRect searchRect = CellRect.CenteredOn(Data.setupCentre, (int)(Data.baseRadius / 2f));

            for (int i = 0; i < 50; i++)
            {
                // Try to find initial spot
                IntVec3 randomCell = searchRect.RandomCell;
                if (Map.reachability.CanReach(randomCell, Data.setupCentre, PathEndMode.OnCell, TraverseMode.NoPassClosedDoors, Danger.Deadly)
                    && !randomCell.GetThingList(this.Map).Any(t => t is Blueprint))
                {
                    //Data.carrierSpots.Add(randomCell); // Got rid of this field in favour of Data.rememberedPositions
                    spots.Add(randomCell);
                    countSpots++;
                    break;
                }
            }

            if (countSpots == 0)
            {
                goto Error;
            }

            byte attempts = 0;
            byte directionalTries = 0;
            while (attempts < 75 && countSpots < countCarriers)
            {
                IntVec3 lastSpot = spots.Last();
                IntVec3 newSpot = new IntVec3(lastSpot.x, lastSpot.y, lastSpot.z);
                IntVec3 offset;

                if (countSpots % 4 == 0)
                {
                    // New row
                    directionalTries++;
                }

                switch (directionalTries)
                {
                    case 0:
                        offset = IntVec3.East * 3;
                        break;
                    case 1:
                        offset = IntVec3.North * 3;
                        break;
                    case 2:
                        offset = IntVec3.West * 3;
                        break;
                    case 3:
                        offset = IntVec3.South * 3;
                        break;
                    default:
                        directionalTries = 1;
                        offset = IntVec3.East * 3;
                        break;
                }

                newSpot += offset;

                if (!spots.Contains(newSpot)
                    && Map.reachability.CanReach(newSpot, Data.setupCentre, PathEndMode.OnCell, TraverseMode.NoPassClosedDoors, Danger.Deadly)
                    && !newSpot.GetThingList(this.Map).Any(t => t is Blueprint))
                {
                    // Spot found
                    //Data.carrierSpots.Add(newSpot);
                    spots.Add(newSpot);
                    if (countSpots % 4 == 0)
                    {
                        // New row
                        directionalTries++;
                    }
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

                    if (spots.Any())
                    {
                        // Try to find next spots close to other spots
                        searchRect = CellRect.CenteredOn(spots.Average(), 6);
                    }

                    for (int i = 0; i < 50; i++)
                    {
                        IntVec3 randomCell = searchRect.RandomCell;
                        if (Map.reachability.CanReach(randomCell, Data.setupCentre, PathEndMode.OnCell, TraverseMode.NoPassClosedDoors, Danger.Deadly)
                            && !randomCell.GetThingList(this.Map).Any(t => t is Blueprint))
                        {
                            //Data.carrierSpots.Add(randomCell);
                            spots.Add(randomCell);
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
                for (int i = 0; i < countSpots; i++)
                {
                    // Add calculated spots to rememberedPositions
                    Data.rememberedPositions.Add(Data.pawnsWithRole[CarnivalRole.Carrier][i], spots[i]);
                }
                return true;
            }


            Error:
            Log.Error("Not enough spots found for carnival carriers to chill.");
            return false;
        }


        private IntVec3 CalculateBannerCell(Map map)
        {
            IntVec3 colonistPos = map.listerBuildings.allBuildingsColonist.NullOrEmpty() ?
                map.mapPawns.FreeColonistsSpawned.RandomElement().Position : map.listerBuildings.allBuildingsColonist.RandomElement().Position;

            IntVec3 bannerCell = Data.carnivalArea.ClosestCellTo(colonistPos);

            if (map.roadInfo.roadEdgeTiles.Any())
            {
                // Prefer to place banner on nearest road
                CellRect searchArea = CellRect.CenteredOn(bannerCell, 70).ClipInsideMap(map);
                float distance = 999999;
                IntVec3 roadCell = IntVec3.Invalid;

                foreach (var cell in searchArea)
                {
                    if (cell.GetTerrain(map).HasTag("Road") && !cell.InNoBuildEdgeArea(map))
                    {
                        float tempDist = bannerCell.DistanceTo(cell);
                        if (tempDist < distance)
                        {
                            distance = tempDist;
                            roadCell = cell;
                        }
                    }
                }

                if (roadCell.IsValid)
                {
                    // Found the edge of a road, try to centre it
                    IntVec3 centredCell = roadCell + IntVec3.East * 2;
                    if (centredCell.GetTerrain(map).HasTag("Road") && !centredCell.InNoBuildEdgeArea(map))
                    {
                        roadCell = centredCell;
                    }
                    else
                    {
                        roadCell += IntVec3.West * 2;
                    }

                    bannerCell = roadCell;
                }
            }

            return bannerCell;
        }

        
    }
}
