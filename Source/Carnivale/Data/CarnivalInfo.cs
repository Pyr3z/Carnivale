using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public ZoneManager zoneManager;

        public IntVec3 setupCentre;

        public float baseRadius;

        public CellRect carnivalArea;

        public IntVec3 bannerCell;

        public IntVec3 trashCell; // Assigned when blueprint is placed

        //public Stack<Thing> thingsToHaul = new Stack<Thing>();

        public List<Thing> thingsToHaul = new List<Thing>();

        public List<Building> carnivalBuildings = new List<Building>();

        public Dictionary<CarnivalRole, DeepReferenceableList<Pawn>> pawnsWithRole = new Dictionary<CarnivalRole, DeepReferenceableList<Pawn>>();

        public Dictionary<Pawn, IntVec3> rememberedPositions = new Dictionary<Pawn, IntVec3>();

        [Unsaved]
        private List<Pawn> pawnWorkingList = null;
        [Unsaved]
        private List<IntVec3> vec3WorkingList = null;


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

            Scribe_Values.Look(ref this.trashCell, "trashCell", default(IntVec3), false);

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

            try
            {
                foreach (var fi in this.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    var oldVal = fi.GetValue(this);

                    if (oldVal == null) continue;

                    if (typeof(ICollection<object>).IsAssignableFrom(fi.FieldType))
                    {
                        var coll = (ICollection<object>)oldVal;
                        coll.Clear();
                    }
                    else if (fi.FieldType == typeof(ZoneManager))
                    {
                        var zm = (ZoneManager)oldVal;

                        var zonelist = zm.AllZones;
                        for (int i = zonelist.Count - 1; i > -1; i--)
                        {
                            zonelist[i].Delete();
                            zonelist[i].Deregister();
                        }
                        fi.SetValue(this, null);

                    }
                    else if (fi.FieldType == typeof(float))
                    {
                        fi.SetValue(this, 0f);
                    }
                    else
                    {
                        fi.SetValue(this, null);
                    }

                    if (debug)
                        Log.Message("[Debug] successfully flushed CarnivalInfo." + fi.Name + ".");
                }
            }
            catch (Exception e)
            {
                Log.Error("Exception during CarnivalInfo Cleanup(). Xnope is garbage with reflection. ex:\n" + e);
            }
        }


        public string GetUniqueLoadID()
        {
            return "CarnivalInfo_" + map.uniqueID;
        }



        public CarnivalInfo ReInitWith(Lord lord, IntVec3 centre)
        {
            // MUST BE CALLED AFTER LordMaker.MakeNewLord()

            this.currentLord = lord;
            this.zoneManager = new ZoneManager(map);
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

            // Initial pass
            IntVec3 closestCell = carnivalArea.ClosestCellTo(colonistPos);

            if (Prefs.DevMode)
                Log.Warning("[Debug] CarnivalInfo.bannerCell first pre pass: " + closestCell);



            // Mountain line of sight pass

            int attempts = 0;
            IntVec3 quadPos = setupCentre - map.Center;
            Rot4 rot;
            while ( attempts < 10 && Utilities.CountMineableCellsTo(setupCentre, closestCell, map, true) > 4)
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
                        tempClosestCell = Utilities.Average(roadCell, tempClosestCell, setupCentre);
                        searchArea = CellRect.CenteredOn(tempClosestCell, searchRadius - 7*attempts).ClipInsideMap(map).ContractedBy(10);
                    }

                    // Find nearest roadcell in area
                    foreach (var cell in searchArea)
                    {
                        if (cell.GetTerrain(map).HasTag("Road")
                            //&& !cell.InNoBuildEdgeArea(map) // check redundant with ContractedBy(10)
                            && cell.Standable(map))
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
                Log.Warning("[Debug] CarnivalInfo.bannerCell final pre pass: " + closestCell);

            return closestCell;
        }

    }
}
