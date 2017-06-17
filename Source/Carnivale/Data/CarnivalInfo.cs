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
    public sealed class CarnivalInfo : MapComponent, ILoadReferenceable
    {

        private static IntRange addToRadius = new IntRange(13, 20);


        // Fields

        public Lord currentLord;

        public IntVec3 setupCentre;

        public float baseRadius;

        public CellRect carnivalArea;

        public IntVec3 bannerCell;

        public IntVec3 trashCell; // Assigned when blueprint is placed

        public Stack<Thing> thingsToHaul = new Stack<Thing>();

        public List<Building> carnivalBuildings = new List<Building>();

        public Dictionary<CarnivalRole, DeepReferenceableList<Pawn>> pawnsWithRole = new Dictionary<CarnivalRole, DeepReferenceableList<Pawn>>();

        public Dictionary<Pawn, IntVec3> rememberedPositions = new Dictionary<Pawn, IntVec3>();

        [Unsaved]
        List<Pawn> pawnWorkingList = null;
        [Unsaved]
        List<IntVec3> vec3WorkingList = null;

        public CarnivalInfo(Map map) : base(map)
        {

        }

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                // Clean up unusable elements in collections

                carnivalBuildings.RemoveAll(b => b.DestroyedOrNull() || !b.Spawned);

                foreach (var list in pawnsWithRole.Values)
                {
                    foreach (var pawn in list)
                    {
                        if (pawn.DestroyedOrNull() || !pawn.Spawned || pawn.Dead)
                        {
                            list.Remove(pawn);
                        }
                    }
                }

                foreach (var pawn in rememberedPositions.Keys)
                {
                    if (pawn.DestroyedOrNull() || !pawn.Spawned || pawn.Dead)
                    {
                        rememberedPositions.Remove(pawn);
                    }
                }
            }

            Scribe_References.Look(ref this.currentLord, "currentLord");

            Scribe_Values.Look(ref this.setupCentre, "setupCentre", default(IntVec3), false);

            Scribe_Values.Look(ref this.baseRadius, "baseRadius", 0f, false);

            Scribe_Values.Look(ref this.carnivalArea, "carnivalArea", default(CellRect), false);

            Scribe_Values.Look(ref this.bannerCell, "bannerCell", default(IntVec3), false);

            Scribe_Values.Look(ref this.trashCell, "trashCell", default(IntVec3), false);

            Scribe_Collections.Look(ref this.thingsToHaul, "thingsToHaul", LookMode.Reference);

            Scribe_Collections.Look(ref this.carnivalBuildings, "carnivalBuildings", LookMode.Reference);

            Scribe_Collections.Look(ref this.pawnsWithRole, "pawnsWithRoles", LookMode.Value, LookMode.Deep);

            Scribe_Collections.Look(ref this.rememberedPositions, "rememberedPositions", LookMode.Reference, LookMode.Value, ref pawnWorkingList, ref vec3WorkingList);
        }



        public string GetUniqueLoadID()
        {
            return "CarnivalInfo_" + map.uniqueID;
        }



        public CarnivalInfo ReInitWith(Lord lord, IntVec3 centre)
        {
            // MUST BE CALLED AFTER LordMaker.MakeNewLord()

            this.currentLord = lord;
            this.setupCentre = centre;

            // Set radius for carnies to stick to
            baseRadius = lord.ownedPawns.Count + addToRadius.RandomInRange;
            baseRadius = Mathf.Clamp(baseRadius, 15f, 35f);

            // Set carnival area
            carnivalArea = CellRect.CenteredOn(setupCentre, (int)baseRadius + 10).ClipInsideMap(map).ContractedBy(10);

            // Set banner spot
            bannerCell = PreCalculateBannerCell();

            // Moved to LordToil_SetupCarnival.. dunno why but it doesn't work here
            foreach (CarnivalRole role in Enum.GetValues(typeof(CarnivalRole)))
            {
                List<Pawn> pawns = (from p in currentLord.ownedPawns
                                    where p.Is(role)
                                    select p).ToList();
                if (role == CarnivalRole.Vendor)
                {
                    // Will enable this when they are standing in stalls
                    pawns.ForEach(p => p.mindState.wantsToTradeWithColony = false);
                }

                pawnsWithRole.Add(role, pawns);
            }

            // The rest is assigned as the carnival goes along

            return this;
        }


        public void Cleanup()
        {
            this.currentLord = null;
            Utilities.cachedRoles.Clear();
        }


        public Building GetFirstBuildingOf(ThingDef def)
        {
            if (this.currentLord == null)
            {
                Log.Error("Cannot get carnival building: carnival is not in town.");
                return null;
            }

            foreach (var building in this.carnivalBuildings)
            {
                if (building.def == def)
                {
                    return building;
                }
            }

            Log.Warning("Tried to find any building of def " + def + " in CarnivalInfo, but none exists.");
            return null;
        }


        public IntVec3 GetNextTrashSpotFor(Thing thing = null)
        {
            // a better way to do this might be to cache the radial
            foreach (var cell in GenRadial.RadialCellsAround(trashCell, 10, false))
            {
                if (!cell.Standable(map)) continue;

                var first = cell.GetFirstHaulable(map);

                if (first == null || (thing == null || (thing.def == first.def && thing.stackCount + first.stackCount <= thing.def.stackLimit)))
                {
                    return cell;
                }
            }

            Log.Error("Found no spot to put trash. Did the trash area overflow?");
            return IntVec3.Invalid;
        }


        public bool AnyCarriersCanCarry(Thing thing)
        {
            return pawnsWithRole[CarnivalRole.Carrier].Any(c => c.HasSpaceFor(thing));
        }


        public int TotalCountToHaulFor(ThingDef def)
        {
            int result = 0;
            foreach (var thing in from t in this.thingsToHaul
                                  where t.def == def
                                  select t)
            {
                result += thing.stackCount;
            }

            return result;
        }


        

        private IntVec3 PreCalculateBannerCell()
        {
            IntVec3 colonistPos = map.listerBuildings.allBuildingsColonist.NullOrEmpty() ?
                map.mapPawns.FreeColonistsSpawned.RandomElement().Position : map.listerBuildings.allBuildingsColonist.RandomElement().Position;

            IntVec3 closestCell = carnivalArea.ClosestCellTo(colonistPos);

            if (Prefs.DevMode)
                Log.Warning("CarnivalInfo.bannerCell first pre pass: " + closestCell);

            // Try to not have too much mountain in the way
            int attempts = 0;
            while ( attempts < 10 && Utilities.CountMineableCells(setupCentre, closestCell, map) > 9)
            {
                IntVec3 quadPos = setupCentre - map.Center;
                Rot4 rot;

                if (quadPos.x > 0 && quadPos.z > 0)
                {
                    // quadrant I
                    if (attempts < 5)
                    {
                        rot = Rot4.South;
                    }
                    else
                    {
                        rot = Rot4.West;
                    }
                }
                else if (quadPos.x < 0 && quadPos.z > 0)
                {
                    // quadrant II
                    if (attempts < 5)
                    {
                        rot = Rot4.South;
                    }
                    else
                    {
                        rot = Rot4.East;
                    }
                }
                else if (quadPos.x < 0 && quadPos.z < 0)
                {
                    // quadrant III
                    if (attempts < 5)
                    {
                        rot = Rot4.North;
                    }
                    else
                    {
                        rot = Rot4.East;
                    }
                }
                else
                {
                    // quadrant IV
                    if (attempts < 5)
                    {
                        rot = Rot4.North;
                    }
                    else
                    {
                        rot = Rot4.West;
                    }
                }

                closestCell = carnivalArea.GetEdgeCells(rot).RandomElement();
                attempts++;

                if (Prefs.DevMode)
                    Log.Warning("CarnivalInfo.bannerCell anti-mountain pass #" + attempts + ": " + closestCell);
            }


            if (!map.reachability.CanReach(setupCentre, closestCell, PathEndMode.OnCell, TraverseMode.NoPassClosedDoors, Danger.Some))
            {
                foreach (var cell in GenRadial.RadialCellsAround(closestCell, 25, false))
                {
                    if (map.reachability.CanReach(setupCentre, cell, PathEndMode.OnCell, TraverseMode.NoPassClosedDoors, Danger.Some))
                    {
                        closestCell = cell;
                        break;
                    }
                }

                if (Prefs.DevMode)
                    Log.Warning("CarnivalInfo.bannerCell reachability pass: " + closestCell);
            }
            

            if (map.roadInfo.roadEdgeTiles.Any())
            {
                // Prefer to place banner on nearest road
                CellRect searchArea = CellRect.CenteredOn(closestCell, 75).ClipInsideMap(map).ContractedBy(10);
                float distance = float.MaxValue;
                IntVec3 roadCell = IntVec3.Invalid;

                foreach (var cell in searchArea)
                {
                    if (cell.GetTerrain(map).HasTag("Road")
                        && !cell.InNoBuildEdgeArea(map)
                        && cell.Standable(map))
                    {
                        float tempDist = closestCell.DistanceToSquared(cell);
                        if (tempDist < distance)
                        {
                            distance = tempDist;
                            roadCell = cell;
                        }
                    }
                }

                if (Prefs.DevMode)
                    Log.Warning("CarnivalInfo.bannerCell first road pass: " + roadCell);

                if (roadCell.IsValid
                    && roadCell.DistanceToSquared(setupCentre) < baseRadius * baseRadius * 3f)
                {
                    // Found the edge of a road,
                    // try to centre it if it is diagonal or vertical
                    IntVec3 adjustedCell = roadCell;

                    if ((adjustedCell + IntVec3.East * 2).GetTerrain(map).HasTag("Road"))
                    {
                        adjustedCell += IntVec3.East * 2;
                    }
                    else if ((adjustedCell + IntVec3.West * 2).GetTerrain(map).HasTag("Road"))
                    {
                        adjustedCell += IntVec3.West * 2;
                    }
                    else
                    {
                        adjustedCell += IntVec3.North;
                    }

                    closestCell = adjustedCell.Standable(map) ? adjustedCell : roadCell;

                    if (Prefs.DevMode)
                        Log.Warning("CarnivalInfo.bannerCell final road pass: " + closestCell);
                }
                else
                {
                    if (Prefs.DevMode)
                        if (!roadCell.IsValid)
                            Log.Warning("CarnivalInfo.bannerCell road pass was invalid. Reason: no road cell found in search area.");
                        else
                            Log.Warning("CarnivalInfo.bannerCell road pass was invalid. Reason: found road cell too far from setupCentre.");
                }
            }

            if (Prefs.DevMode)
                Log.Warning("CarnivalInfo.bannerCell final pre pass: " + closestCell);

            return closestCell;
        }

    }
}
