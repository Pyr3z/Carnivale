﻿using RimWorld;
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


        

        private IntVec3 PreCalculateBannerCell()
        {
            IntVec3 colonistPos = map.listerBuildings.allBuildingsColonist.NullOrEmpty() ?
                map.mapPawns.FreeColonistsSpawned.RandomElement().Position : map.listerBuildings.allBuildingsColonist.RandomElement().Position;

            IntVec3 closestCell = carnivalArea.ClosestCellTo(colonistPos);

            if (Prefs.DevMode)
                Log.Warning("CarnivalInfo.bannerCell first pre pass: " + closestCell.ToString());

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
                    Log.Warning("CarnivalInfo.bannerCell reachability pre pass: " + closestCell.ToString());
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
                    Log.Warning("CarnivalInfo.bannerCell first road pass: " + closestCell.ToString());

                if (roadCell.IsValid
                    && roadCell.DistanceToSquared(setupCentre) < baseRadius * baseRadius * 1.25f)
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
                        Log.Warning("CarnivalInfo.bannerCell final road pass: " + closestCell.ToString());
                }
            }

            if (Prefs.DevMode)
                Log.Warning("CarnivalInfo.bannerCell final pre pass: " + closestCell.ToString());

            return closestCell;
        }
    }
}
