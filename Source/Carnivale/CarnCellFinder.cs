using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Xnope;

namespace Carnivale
{
    public static class CarnCellFinder
    {
        private static CarnivalInfo Info
        {
            get
            {
                return CarnUtils.Info;
            }
        }

        private static IntVec3 AverageColPos
        {
            get
            {
                return CellsUtil.AverageColonistPosition(Info.map);
            }
        }

        private static int MinDistToMapEdge
        {
            get
            {
                return (int)Info.baseRadius;
            }
        }

        private static int MinDistToColony
        {
            get
            {
                return (int)Info.baseRadius * 2;
            }
        }


        public static bool BestCarnivalSpawnSpot(Map map, out IntVec3 spot)
        {
            Func<IntVec3, bool> reachable = c => map.reachability.CanReachColony(c);
            Func<IntVec3, bool> buildable = c => c.IsAroundGoodTerrain(map, 7);
            Func<IntVec3, float> weightByLoS = c => 1f / (c.CountObstructingCellsTo(AverageColPos, map) + 1f);
            Func<IntVec3, float> weightBest = c => (weightByLoS(c) == 1f ? 1f : 0f) + (buildable(c) ? 2f : 0f);

            IEnumerable<IntVec3> roadEdges = map.roadInfo.roadEdgeTiles.Where(reachable);

            if (roadEdges.Any())
            {
                if (roadEdges.TryRandomElementByWeight(weightBest, out spot))
                {
                    if (Prefs.DevMode)
                        Log.Message("\t[Carnivale] Calculated spawn centre: " + spot + "; optimal road pass");

                    return true;
                }
                else if (roadEdges.TryRandomElementByWeight(weightByLoS, out spot))
                {
                    if (Prefs.DevMode)
                        Log.Message("\t[Carnivale] Calculated spawn centre: " + spot + "; sub-optimal road pass");

                    return true;
                }
            }

            IEnumerable<IntVec3> edges = CellRect.WholeMap(map).CornerlessEdgeCells().Where(reachable);

            if (edges.TryRandomElementByWeight(weightBest, out spot))
            {
                if (Prefs.DevMode)
                    Log.Message("\t[Carnivale] Calculated spawn centre: " + spot + "; optimal random pass");

                return true;
            }
            else if (edges.TryRandomElementByWeight(weightByLoS, out spot))
            {
                if (Prefs.DevMode)
                    Log.Message("\t[Carnivale] Calculated spawn centre: " + spot + "; sub-optimal random pass");

                return true;
            }

            for (int i = 0; i < 100; i++)
            {
                spot = CellFinder.RandomEdgeCell(map);
                if (reachable(spot))
                {
                    if (Prefs.DevMode)
                        Log.Message("\t[Carnivale] Calculated spawn centre: " + spot + "; worst random pass");

                    return true;
                }
            }

            spot = IntVec3.Invalid;
            return false;
        }


        public static IntVec3 BestCarnivalSetupPosition(IntVec3 initPos, Map map)
        {
            IntVec3 averageColPos;

            if (!(averageColPos = CellsUtil.ApproxClosestColonistBuilding(map, initPos, ThingDefOf.Door)).IsValid
                && !(averageColPos = CellsUtil.ApproxClosestColonistBuilding(map, initPos, ThingDefOf.Wall)).IsValid)
            {
                averageColPos = AverageColPos;
            }
            else
            {
                averageColPos = averageColPos.AverageWith(AverageColPos);
            }

            var distSqrdToColony = initPos.DistanceToSquared(averageColPos);

            if (Prefs.DevMode)
                Log.Message("[Carnivale] setupSpot: initPos=" + initPos + ", averageColPos=" + averageColPos + ", distSqrdToColony=" + distSqrdToColony);

            var result = initPos;

            if (!TryCarnivalSetupPosition_Triangular(initPos, averageColPos, distSqrdToColony, map, out result))
            {
                if (Prefs.DevMode)
                    Log.Warning("\t[Carnivale] setupSpot: triangular algorithm failed. Using old random iteration algorithm.");

                TryCarnivalSetupPosition_Random(initPos, averageColPos, 7, map, out result);
            }

            if (Prefs.DevMode)
            {
                if (result == initPos)
                {
                    Log.Error(string.Concat(new object[]
                    {
                    "[Carnivale] Could not find carnival setup spot from ",
                    initPos,
                    ", expect more errors. Using ",
                    initPos
                    }));
                }

                Log.Message("[Carnivale] setupSpot: final pass: " + result + ". distToColony=" + result.DistanceTo(averageColPos));

                map.debugDrawer.FlashCell(result, 0.5f, "Setup Spot");
            }

            return result;
        }

        private static bool TryCarnivalSetupPosition_Triangular(IntVec3 initPos, IntVec3 colPos, int distSqrToColony, Map map, out IntVec3 result)
        {
            var minDistSqrToColony = MinDistToColony * MinDistToColony * 2;

            var halfAngle = Mathf.Lerp(30f, 10f, distSqrToColony / map.Size.LengthHorizontalSquared);

            IntVec3 roadPos;
            if (!map.AnyRoads()
                || !initPos
                    .TranslateToward(colPos, c => !c.InNoBuildEdgeArea(map))
                    .TryFindNearestRoadCell(map, 15f, out roadPos, c => !c.InNoBuildEdgeArea(map)))
            {
                roadPos = initPos;
            }

            var mapRect = CellRect.WholeMap(map).ContractedBy(MinDistToMapEdge);

            var candidateTri = CellTriangle
                .FromTarget(colPos, roadPos, halfAngle, map.Size.x / 2)
                .ClipInside(mapRect);

            if (Prefs.DevMode)
            {
                Log.Message("\t[Carnivale] triangular setupSpot: MinDistToMapEdge=" + MinDistToMapEdge + ", minDistSqrToColony=" + minDistSqrToColony + ", halfAngle=" + halfAngle + ", roadPos=" + roadPos);

                candidateTri.DebugFlashDraw();
            }

            Func<IntVec3, bool> reachable = c => map.reachability.CanReachColony(c);
            Func<IntVec3, bool> validDist = c => c.DistanceToSquared(colPos) >= minDistSqrToColony;
            Func<IntVec3, bool> validRoad = c => !map.AnyRoads() || c.DistanceSquaredToNearestRoad(map, Info.baseRadius * 1.5f) < int.MaxValue;
            Func<IntVec3, float> weightBuildable = c => 1f / (c.CountBadTerrainInRadius(map, 14) + 1f);
            Func<IntVec3, float> weightDist = c => Rand.Range(0, 20) + distSqrToColony / (c.DistanceToSquared(colPos) + 1f);

            var maxWeight = 0f;
            foreach (var cell in candidateTri.InRandomOrder().Where(c => validDist(c) && reachable(c) && validRoad(c)))
            {
                var weight = weightBuildable(cell) + weightDist(cell);
                var thresh = Rand.Range(19, 24);

                if (weight > maxWeight) maxWeight = weight;

                if (weight >= thresh)
                {
                    result = cell;

                    if (Prefs.DevMode)
                    {
                        Log.Message("\t[Carnivale] triangular setupSpot: successfully found cell. result=" + result + ", weight=" + weight);
                    }
                    return true;
                }
            }

            if (Prefs.DevMode)
                Log.Warning("\t[Carnivale] triangular setupSpot: unable to find cell in candidates. maxWeight=" + maxWeight);

            result = IntVec3.Invalid;
            return false;
        }

        private static bool TryCarnivalSetupPosition_Random(IntVec3 initPos, IntVec3 colPos, int tenth, Map map, out IntVec3 result)
        {
            var minDistToColony = (int)MinDistToColony;
            var minSqrDistToColony = minDistToColony * minDistToColony;

            if (Prefs.DevMode)
                Log.Message("\t[Carnivale] random setupSpot: minDistToColony=" + minDistToColony);

            var cellRect = CellRect.CenteredOn(initPos, (minDistToColony * tenth / 10));
            cellRect.ClipInsideMap(map);

            IntVec3 randomCell;
            for (int attempt = 0; attempt < tenth * 10; attempt++)
            {
                randomCell = cellRect.RandomCell;
                if (randomCell.Standable(map)
                    && randomCell.DistanceToSquared(colPos) >= minSqrDistToColony
                    && !randomCell.Roofed(map)
                    && randomCell.IsAroundTerrainAffordances(map, tenth * 2, TerrainAffordance.Light)
                    && map.reachability.CanReach(initPos, randomCell, PathEndMode.OnCell, TraverseMode.NoPassClosedDoors, Danger.Some)
                    && map.reachability.CanReachColony(randomCell)
                    && attempt > tenth * 5
                    || (map.roadInfo.roadEdgeTiles.Any() && randomCell.IsAroundTerrainOfTag(map, 16, "Road")
                    || (!randomCell.IsAroundTerrainOfTag(map, 16, "Water") && randomCell.DistanceSquaredToNearestMineable(map, 16) > tenth * tenth * 4)))
                {
                    result = randomCell;
                    return true;
                }
            }

            if (tenth > 4)
            {
                if (Prefs.DevMode)
                    Log.Warning("\t[Carnivale] setupSpot: Failed to find by random iteration. Will try " + (tenth - 4) + " more times.");

                return TryCarnivalSetupPosition_Random(initPos, colPos,  --tenth, map, out result);
            }

            result = IntVec3.Invalid;
            return false;
        }


        public static IntVec3 PreCalculateBannerCell()
        {
            if (!Info.Active)
            {
                Log.Error("[Carnivale] Tried to perform a cell calculation while current carnival is inactive.");
                return IntVec3.Invalid;
            }

            var map = Info.map;
            var setupCentre = Info.setupCentre;
            var baseRadius = Info.baseRadius;

            var minDistToCentre = baseRadius / 2;
            var maxDistToCentre = baseRadius + 3f;

            var minDistSqrdToCentre = (int)(minDistToCentre * minDistToCentre * 2);
            var maxDistSqrdToCentre = (int)(maxDistToCentre * maxDistToCentre * 2);

            IntVec3 colonistPos;

            if (!(colonistPos = CellsUtil.ApproxClosestColonistBuilding(map, setupCentre, ThingDefOf.Door)).IsValid)
            {
                colonistPos = CellsUtil.AverageColonistPosition(map);
            }
            else
            {
                colonistPos = colonistPos.AverageWith(CellsUtil.AverageColonistPosition(map));
            }

            // Initial pass
            var closestCell = CellRect.CenteredOn(setupCentre, (int)maxDistToCentre).EdgeCells.ClosestCellTo(colonistPos, map);

            if (Prefs.DevMode)
                Log.Message("[Carnivale] bannerCell initial pass: " + closestCell);

            // Road pass

            foreach (var rcell in closestCell.TryFindNearestRoadCells(map, baseRadius - 5))
            {
                var dist = rcell.DistanceToSquared(setupCentre);

                if (dist >= minDistSqrdToCentre && dist <= maxDistSqrdToCentre)
                {
                    var adjustedCell = rcell;

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

                    closestCell = adjustedCell;

                    if (Prefs.DevMode)
                        Log.Message("\t[Carnivale] bannerCell road pass: " + closestCell + ". distSqrdToCentre=" + dist + ", minDistSqrdToCentre=" + minDistSqrdToCentre + ", maxDistSqrdToCentre=" + maxDistSqrdToCentre);

                    break;
                }
                else
                {
                    if (Prefs.DevMode)
                        Log.Warning("\t[Carnivale] bannerCell road pass failure: " + rcell + ". distSqrdToCentre=" + dist + ", minDistSqrdToCentre=" + minDistSqrdToCentre + ", maxDistSqrdToCentre=" + maxDistSqrdToCentre);
                }
            }

            // line of sight pass

            if (!GenSight.LineOfSight(setupCentre, closestCell, map))
            {
                Func<IntVec3, float> weightLoSSetupCentre = c => 2f / (setupCentre.CountObstructingCellsTo(c, map) + 1f);
                Func<IntVec3, float> weightLoSColony = c => 1f / (c.CountObstructingCellsTo(colonistPos, map) + 1f);
                Func<IntVec3, float> weightBest = c => (weightLoSSetupCentre(c) == 2f ? 2f : 0f) + (weightLoSColony(c) == 1f ? 1f : 0f);

                var candidateCells = CellTriangle
                    .FromTarget(setupCentre, closestCell, 45f, maxDistToCentre)
                    .Where(c => c.InBounds(map) && c.DistanceToSquared(setupCentre) >= minDistSqrdToCentre);

                try
                {
                    closestCell = candidateCells.MaxBy(weightBest);

                    if (Prefs.DevMode)
                        Log.Message("\t[Carnivale] bannerCell optimal LoS pass: " + closestCell);
                }
                catch (InvalidOperationException e)
                {
                    IntVec3 tempCell;

                    if (candidateCells.TryRandomElementByWeight(weightLoSSetupCentre + weightLoSColony, out tempCell))
                    {
                        closestCell = tempCell;

                        if (Prefs.DevMode)
                            Log.Message("\t[Carnivale] bannerCell sub-optimal LoS pass: " + closestCell);
                    }
                    else if (Prefs.DevMode)
                    {
                        Log.Warning("\t[Carnivale] bannerCell failed LoS passes. Is candidateCells empty? Leaving it at: " + closestCell);
                    }
                }
            }

            // Mountain proximity pass

            var attempts = 0;
            IntVec3 nearestMineable;
            while (attempts < 10
                   && closestCell.DistanceSquaredToNearestMineable(map, 12, out nearestMineable) <= 36)
            {
                closestCell = CellRect.CenteredOn(closestCell, 5).FurthestCellFrom(nearestMineable);
                attempts++;

                if (Prefs.DevMode)
                    Log.Message("\t[Carnivale] bannerCell mountain proximity pass #" + attempts + ": " + closestCell);
            }

            if (attempts == 10 && Prefs.DevMode)
                Log.Warning("\t[Carnivale] bannerCell mountain proximity passes took too many tries. Leaving it at: " + closestCell);

            // End passes

            if (Prefs.DevMode)
                Log.Message("[Carnivale] bannerCell pre-buildability pass: " + closestCell);

            return closestCell;
        }
    }
}
