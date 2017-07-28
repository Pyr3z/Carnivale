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

        private static float MinDistToColony
        {
            get
            {
                return Mathf.Min(Info.baseRadius * 2.5f, 70f);
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
            //if (initPos.x == 0)
            //{
            //    initPos.x += MinDistToMapEdge;
            //}
            //if (initPos.z == 0)
            //{
            //    initPos.z += MinDistToMapEdge;
            //}
            //if (initPos.x == map.Size.x - 1)
            //{
            //    initPos.x -= MinDistToMapEdge;
            //}
            //if (initPos.z == map.Size.z - 1)
            //{
            //    initPos.z -= MinDistToMapEdge;
            //}

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

            var distFromColony = initPos.DistanceTo(averageColPos);

            if (Prefs.DevMode)
                Log.Message("[Carnivale] setupSpot: initPos=" + initPos + ", averageColPos=" + averageColPos + ", distFromColony=" + distFromColony);

            var result = initPos;

            if (TryCarnivalSetupPosition_Triangular(initPos, averageColPos, distFromColony, map, out result))
            {
                if (Prefs.DevMode)
                    Log.Message("[Carnivale] setupSpot: final pass: " + result + ". distFromColony=" + result.DistanceTo(averageColPos));

                return result;
            }
            else
            {
                if (Prefs.DevMode)
                    Log.Warning("\t[Carnivale] setupSpot: triangular algorithm failed. Using old random iteration algorithm.");

                if (TryCarnivalSetupPosition_Random(initPos, averageColPos, distFromColony, 7, map, out result))
                {
                    if (Prefs.DevMode)
                        Log.Message("[Carnivale] setupSpot: final pass: " + result + ". distFromColony=" + result.DistanceTo(averageColPos));

                    return result;
                }
            }

            Log.Error(string.Concat(new object[]
            {
                "[Carnivale] Could not find carnival setup spot from ",
                initPos,
                ", expect more errors. Using ",
                initPos
            }));
            return initPos;
        }

        private static bool TryCarnivalSetupPosition_Triangular(IntVec3 initPos, IntVec3 colPos, float distFromColony, Map map, out IntVec3 result)
        {
            var minDistSqrToColony = MinDistToColony * MinDistToColony;
            var distSqrToColony = distFromColony * distFromColony;

            var mapRect = CellRect.WholeMap(map).ContractedBy(MinDistToMapEdge);
            var halfAngle = Mathf.Lerp(15f, 75f, distSqrToColony / map.Size.LengthHorizontalSquared);

            if (Prefs.DevMode)
                Log.Message("\t[Carnivale] triangular setupSpot: MinDistToMapEdge=" + MinDistToMapEdge + ", MinDistToColony=" + MinDistToColony + ", halfAngle=" + halfAngle);

            var candidateTri = CellTriangle
                .FromTarget(colPos, initPos, halfAngle, MinDistToColony)
                .ClipInside(mapRect);

            Func<IntVec3, bool> reachable = c => map.reachability.CanReachColony(c);
            Func<IntVec3, bool> validDist = c => c.DistanceToSquared(colPos) >= minDistSqrToColony;
            Func<IntVec3, float> weightBuildable = c => c.IsAroundGoodTerrain(map, 5) ? 10f : 0f;
            Func<IntVec3, float> weightDist = c => Rand.Range(0, 20) + distSqrToColony / (c.DistanceToSquared(colPos) + 1f);
            Func<IntVec3, float> weightRoad = c => !map.roadInfo.roadEdgeTiles.Any() ? 1f : 0.75f / (c.DistanceSquaredToNearestRoad(map, halfAngle / 2f) + 1f);

            var candidateCells = candidateTri
                .Where(c => reachable(c) && validDist(c));

            try
            {
                result = candidateCells.MaxBy(weightBuildable + weightDist + weightRoad);

                if (Prefs.DevMode)
                    Log.Message("\t[Carnivale] triangular setupSpot: successfully found cell. result=" + result);

                return true;
            }
            catch (InvalidOperationException e)
            {
                if (Prefs.DevMode)
                    Log.Warning("\t[Carnivale] triangular setupSpot: unable to find cell in candidates.");

                result = IntVec3.Invalid;
                return false;
            }
        }

        private static bool TryCarnivalSetupPosition_Thirds(IntVec3 initPos, IntVec3 colPos, float distFromColony, int tenth, Map map, out IntVec3 result)
        {
            // This algorithm sucks.

            var minDistFromEdge = Info.baseRadius / 2;
            var minDistFromColony = Mathf.Lerp(Info.baseRadius + 25, distFromColony - minDistFromEdge, tenth / 10f);
            var minSqrDistFromColony = minDistFromColony * minDistFromColony;


            if (Prefs.DevMode)
                Log.Message("\t[Carnivale] setupSpot: minDistFromEdge=" + minDistFromEdge + ", minDistFromColony =" + minDistFromColony + ", tenth=" + tenth);

            // Find candidate thirds

            var midpoint = initPos.AverageWith(colPos);

            var startpoint = midpoint.AverageWith(initPos);
            while (startpoint != midpoint && (startpoint.DistanceToMapEdge(map) < minDistFromEdge || startpoint.DistanceToSquared(initPos) < minDistFromEdge * minDistFromEdge))
            {
                startpoint = startpoint.AverageWith(midpoint);

                if (Prefs.DevMode)
                    Log.Message("\t\t[Carnivale] setupSpot: adjusting startpoint: " + startpoint);
            }

            var endpoint = midpoint.AverageWith(colPos);
            while (endpoint != midpoint && endpoint.DistanceToSquared(colPos) < minSqrDistFromColony)
            {
                endpoint = endpoint.AverageWith(midpoint);

                if (Prefs.DevMode)
                    Log.Message("\t\t[Carnivale] setupSpot: adjusting endpoint: " + endpoint);
            }

            if (Prefs.DevMode)
                Log.Message("\t[Carnivale] setupSpot: thirds startpoint=" + startpoint + ", midpoint=" + midpoint + ", endpoint=" + endpoint);

            // select best third

            IntVec3 bestPos;
            if (tenth < 4)
            {
                bestPos = startpoint;
            }
            if (tenth < 5)
            {
                bestPos = midpoint;

                if (Prefs.DevMode)
                    Log.Message("\t[Carnivale] setupSpot: furthest from colony bestThird=" + bestPos);
            }
            else if (midpoint.DistanceToSquared(colPos) >= minSqrDistFromColony)
            {
                bestPos = endpoint;

                if (Prefs.DevMode)
                    Log.Message("\t[Carnivale] setupSpot: closest to colony bestThird=" + bestPos);
            }
            else
            {
                goto Error;
            }

            // Find average between bestThird and nearest road

            IntVec3 roadNear;
            IntVec3 roadFar;
            if (bestPos.TryFindNearestAndFurthestRoadCell(map, tenth * tenth, out roadNear, out roadFar))
            {
                if (roadFar.IsValid)
                {
                    bestPos = new CellTriangle(bestPos, roadNear, roadFar).Centre;

                    if (Prefs.DevMode)
                        Log.Message("\t[Carnivale] setupSpot: road triangular average bestPos=" + bestPos);
                }
                else
                {
                    bestPos = bestPos.AverageWith(roadNear);

                    if (Prefs.DevMode)
                        Log.Message("\t[Carnivale] setupSpot: road simple average bestPos=" + bestPos);
                }
            }

            // Find cell furthest from mountains

            var candidates = CellRect.CenteredOn(bestPos, tenth * 2).Cells
                .Where(c => c.InBounds(map)
                       && c.DistanceToMapEdge(map) > minDistFromEdge
                       && c.DistanceToSquared(colPos) >= minSqrDistFromColony
                       && !c.Roofed(map)
                       && c.IsAroundGoodTerrain(map, tenth * 3));

            if (candidates.TryRandomElement(out bestPos))
            {
                if (Prefs.DevMode)
                    Log.Message("\t[Carnivale] setupSpot: final pass bestPos=" + bestPos);
            }
            else
            {
                goto Error;
            }

            // Reachability pass

            if (map.reachability.CanReach(initPos, bestPos, PathEndMode.ClosestTouch, TraverseMode.NoPassClosedDoors, Danger.Some)
                && map.reachability.CanReachColony(bestPos)
                && bestPos.DistanceSquaredToNearestMineable(map, 16) > tenth * tenth * 4)
            {
                result = bestPos;
                return true;
            }

            Error:

            if (tenth > 3)
            {
                if (Prefs.DevMode)
                    Log.Warning("\t[Carnivale] setupSpot: Failed to find by thirds. Will try " + (tenth - 3) + " more times.");

                return TryCarnivalSetupPosition_Thirds(initPos, colPos, distFromColony, --tenth, map, out result);
            }
            else
            {
                if (Prefs.DevMode)
                    Log.Warning("\t[Carnivale] setupSpot: Failed to find by thirds. Trying old random iterative method.");

                return TryCarnivalSetupPosition_Random(initPos, colPos, distFromColony, 7, map, out result);
            }
        }

        private static bool TryCarnivalSetupPosition_Random(IntVec3 initPos, IntVec3 colPos, float distFromColony, int tenth, Map map, out IntVec3 result)
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

                return TryCarnivalSetupPosition_Random(initPos, colPos, distFromColony, --tenth, map, out result);
            }

            result = IntVec3.Invalid;
            return false;
        }
    }
}
