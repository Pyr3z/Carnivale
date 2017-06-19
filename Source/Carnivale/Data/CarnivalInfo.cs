using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Xnope;

namespace Carnivale
{
    public sealed class CarnivalInfo : MapComponent, ILoadReferenceable
    {

        private static IntRange addToRadius = new IntRange(13, 20);


        // Fields

        public Lord currentLord;

        public IntVec3 setupCentre; // possibly use the same setup area for every carnival after initial calculation?

        public float baseRadius;

        public CellRect carnivalArea;

        public IntVec3 bannerCell;

        private IntVec3 trashCentre; // Assigned when blueprint is placed

        //public Stack<Thing> thingsToHaul = new Stack<Thing>();

        public List<Thing> thingsToHaul = new List<Thing>(); // would a hashset be better?

        public List<Building> carnivalBuildings = new List<Building>();

        public Dictionary<CarnivalRole, DeepReferenceableList<Pawn>> pawnsWithRole = new Dictionary<CarnivalRole, DeepReferenceableList<Pawn>>();

        public Dictionary<Pawn, IntVec3> rememberedPositions = new Dictionary<Pawn, IntVec3>();

        [Unsaved]
        private List<Pawn> pawnWorkingList = null;
        [Unsaved]
        private List<IntVec3> vec3WorkingList = null;


        public bool Active { get { return currentLord != null; } }

        public bool ShouldHaulTrash { get { return trashCentre != null && trashCentre.IsValid; } }

        public IntVec3 TrashCell
        {
            get
            {
                return trashCentre;
            }
            set
            {
                trashCentre = value;

                if (value.IsValid)
                {
                    
                }
            }
        }


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
                    for (int i = list.Count - 1; i > -1; i--)
                    {
                        var pawn = list[i];
                        if (pawn.DestroyedOrNull() || !pawn.Spawned || pawn.Dead)
                        {
                            list.RemoveAt(i);
                        }
                    }
                }

                IEnumerator<Pawn> en = rememberedPositions.Keys.GetEnumerator();

                while (en.MoveNext())
                {
                    var pawn = en.Current;
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

            Scribe_Values.Look(ref this.trashCentre, "trashCell", default(IntVec3), false);

            Scribe_Collections.Look(ref this.thingsToHaul, "thingsToHaul", LookMode.Reference);

            Scribe_Collections.Look(ref this.carnivalBuildings, "carnivalBuildings", LookMode.Reference);

            Scribe_Collections.Look(ref this.pawnsWithRole, "pawnsWithRoles", LookMode.Value, LookMode.Deep);

            Scribe_Collections.Look(ref this.rememberedPositions, "rememberedPositions", LookMode.Reference, LookMode.Value, ref pawnWorkingList, ref vec3WorkingList);

            pawnWorkingList = null;

            vec3WorkingList = null;
        }


        public void Cleanup(bool debug = false)
        {
            Utilities.cachedRoles.Clear();
            if (debug)
                Log.Message("[Debug] cleared Utilities.cachedRoles.");

            currentLord = null;

            setupCentre = default(IntVec3);

            baseRadius = 0f;

            carnivalArea = default(CellRect);

            bannerCell = default(IntVec3);

            trashCentre = default(IntVec3);

            thingsToHaul.Clear();

            carnivalBuildings.Clear();

            pawnsWithRole.Clear();

            rememberedPositions.Clear();
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

            // Cache pawn roles to lists
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


        public override void MapRemoved()
        {
            this.Cleanup(false);

            base.MapRemoved();
        }


        public override void MapComponentTick()
        {
            // Check if there are any things needing to be hauled to carriers or trash
            if (Active && Find.TickManager.TicksGame % 1000 == 0)
            {
                int curCount = thingsToHaul.Count;

                // Should cache search cells? Is optimisation essential here?
                foreach (var thing in from t in GenRadial.RadialDistinctThingsAround(setupCentre, this.map, baseRadius, true)
                                        where t.def.EverHaulable
                                           && !t.def.IsWithinCategory(ThingCategoryDefOf.Chunks)
                                           && (!(currentLord.CurLordToil is LordToil_SetupCarnival)
                                              || ((LordToilData_SetupCarnival)currentLord.CurLordToil.data).availableCrates.Contains(t))
                                           && !thingsToHaul.Contains(t)
                                        select t)
                {
                    if (Prefs.DevMode)
                        Log.Warning("[Debug] Adding " + thing + " to CarnivalInfo.thingsToHaul.");
                    thingsToHaul.Add(thing);
                }

                if (Prefs.DevMode && curCount == thingsToHaul.Count)
                {
                    Log.Warning("[Debug] CarnivalInfo ticked but found no new things to haul. Everything could still be fine.");
                }
            }
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

            Log.Warning("[Debug] Tried to find any building of def " + def + " in CarnivalInfo, but none exists.");
            return null;
        }



        public IntVec3 GetNextTrashSpotFor(Thing thing, Pawn carrier = null)
        {
            if (thing != null && ShouldHaulTrash)
            {
                // a better way to do this might be to cache the radial
                foreach (var cell in GenRadial.RadialCellsAround(trashCentre, 10, false))
                {
                    if (!cell.Standable(map) || !cell.InBounds(map)) continue;

                    if (StoreUtility.IsGoodStoreCell(cell, map, thing, carrier, currentLord.faction))
                    {
                        return cell;
                    }
                }
            }
            

            Log.Error("Found no spot to put trash. Jobs will be ended. Did the trash area overflow?");
            this.trashCentre = IntVec3.Invalid;
            return trashCentre;
        }


        public bool AnyCarriersCanCarry(Thing thing)
        {
            return pawnsWithRole[CarnivalRole.Carrier].Any(c => c.HasSpaceFor(thing));
        }


        public int UnreservedThingsToHaulOf(ThingDef def)
        {
            return thingsToHaul.Sum(delegate (Thing t)
            {
                if (t.def == def && !map.reservationManager.IsReserved(t, currentLord.faction))
                    return t.stackCount;
                return 0;
            });
        }




        private IntVec3 PreCalculateBannerCell()
        {
            // Yo stop working on this. If it ain't broke don't fix it.

            IntVec3 colonistPos = map.listerBuildings.allBuildingsColonist.NullOrEmpty() ?
                map.mapPawns.FreeColonistsSpawned.RandomElement().Position : map.listerBuildings.allBuildingsColonist.RandomElement().Position;

            // Initial pass
            IntVec3 closestCell = carnivalArea.ClosestCellTo(colonistPos);

            if (Prefs.DevMode)
                Log.Warning("[Debug] CarnivalInfo.bannerCell initial pass: " + closestCell);


            // Mountain line of sight pass

            int attempts = 0;
            //IntVec3 quadPos = setupCentre - map.Center;
            // using colonistPos instead to get edges closest to colonists, rather than the map centre
            IntVec3 quadPos = setupCentre - colonistPos;
            Rot4 rot;
            while (attempts < 10 && setupCentre.CountMineableCellsTo(closestCell, map, true) > 4)
            {
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

                closestCell = carnivalArea.GetEdgeCells(rot).Where(c => c.Walkable(map)).RandomElementWithFallback(closestCell);
                attempts++;

                if (Prefs.DevMode)
                    Log.Warning("[Debug] CarnivalInfo.bannerCell mountain line-of-sight pass #" + attempts + ": " + closestCell);
            }

            if (attempts == 10 && Prefs.DevMode)
                Log.Warning("[Debug] CarnivalInfo.bannerCell mountain line-of-sight passes took too many tries. Leaving it at: " + closestCell);


            // Mountain proximity pass

            attempts = 0;
            IntVec3 nearestMineable;
            while (attempts < 10
                   && closestCell.DistanceSquaredToNearestMineable(map, 12, out nearestMineable) > 16
                   && nearestMineable.IsValid)
            {
                closestCell = CellRect.CenteredOn(closestCell, 2).FurthestCellFrom(nearestMineable);
                attempts++;

                if (Prefs.DevMode)
                    Log.Warning("[Debug] CarnivalInfo.bannerCell mountain proximity pass #" + attempts + ": " + closestCell);
            }

            if (attempts == 10 && Prefs.DevMode)
                Log.Warning("[Debug] CarnivalInfo.bannerCell mountain proximity passes took too many tries. Leaving it at: " + closestCell);


            // Reachability pass

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
                    Log.Warning("[Debug] CarnivalInfo.bannerCell reachability pass: " + closestCell);
            }

            
            // Road pass

            if (map.roadInfo.roadEdgeTiles.Any())
            {
                int searchRadius = (int)(baseRadius * 1.5f) + 10;
                CellRect searchArea = CellRect.CenteredOn(closestCell, searchRadius).ClipInsideMap(map).ContractedBy(10);
                float distSqrd = float.MaxValue;
                float minDistSqrdToCentre = baseRadius * baseRadius * 4f;
                IntVec3 tempClosestCell = closestCell;
                IntVec3 roadCell = IntVec3.Invalid;
                attempts = 0;

                while (attempts < 3 && (!roadCell.IsValid || roadCell.DistanceToSquared(setupCentre) > minDistSqrdToCentre))
                {
                    if (attempts > 0 && roadCell.IsValid)
                    {
                        // after first pass, try an average of closestCell with setupCentre and closest roadCell
                        tempClosestCell = CellsUtil.Average(roadCell, tempClosestCell, setupCentre);
                        searchArea = CellRect.CenteredOn(tempClosestCell, searchRadius - 7*attempts).ClipInsideMap(map).ContractedBy(10);
                    }

                    // Find nearest roadcell in area
                    foreach (var cell in searchArea)
                    {
                        if (cell.GetTerrain(map).HasTag("Road")
                            //&& !cell.InNoBuildEdgeArea(map) // check redundant with ContractedBy(10)
                            && cell.Walkable(map))
                        {
                            float tempDist = tempClosestCell.DistanceToSquared(cell);
                            if (tempDist < distSqrd)
                            {
                                distSqrd = tempDist;
                                roadCell = cell;
                            }
                        }
                    }

                    attempts++;

                    if (Prefs.DevMode)
                        Log.Warning("[Debug] CarnivalInfo.bannerCell road pass #" + attempts + ": " + roadCell);
                }

                if (roadCell.IsValid
                    && roadCell.DistanceToSquared(setupCentre) < minDistSqrdToCentre)
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

                    closestCell = adjustedCell.Walkable(map) ? adjustedCell : roadCell;

                    if (Prefs.DevMode)
                        Log.Warning("[Debug] CarnivalInfo.bannerCell final road pass: " + closestCell);
                }
                else
                {
                    if (Prefs.DevMode)
                        if (!roadCell.IsValid)
                            Log.Warning("[Debug] CarnivalInfo.bannerCell road pass was invalid. Reason: no road cell found in search area.");
                        else
                            Log.Warning("[Debug] CarnivalInfo.bannerCell road pass was invalid. Reason: found road cell too far from setupCentre.");
                }
            }

            if (Prefs.DevMode)
                Log.Warning("[Debug] CarnivalInfo.bannerCell pre-buildability pass: " + closestCell);

            return closestCell;
        }

    }
}
