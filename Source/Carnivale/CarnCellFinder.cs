using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Xnope;

namespace Carnivale
{
    public static class CarnCellFinder
    {
        private static IntVec3 cachedAverageColPos = IntVec3.Invalid;

        private static CarnivalInfo Info
        {
            get
            {
                return CarnUtils.Info;
            }
        }

        private static float MinimumDistToColony
        {
            get
            {
                return Info.baseRadius * 1.5f;
            }
        }

        public static void Cleanup()
        {
            cachedAverageColPos = IntVec3.Invalid;
        }

        public static IntVec3 AverageColonistPosition(Map map, bool cache = true)
        {
            if (cache && cachedAverageColPos.IsValid)
            {
                return cachedAverageColPos;
            }

            var colonistThingsLocList = new List<IntVec3>();

            foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
            {
                colonistThingsLocList.Add(pawn.Position);
            }
            foreach (var building in map.listerBuildings.allBuildingsColonist.Where(b => b.def == ThingDefOf.Wall || b is Building_Bed || b is IAttackTarget))
            {
                colonistThingsLocList.Add(building.Position);
            }

            var ave = colonistThingsLocList.Average();

            if (cache)
            {
                Log.Message("[Carnivale] Cached average colonist position: " + ave);
                cachedAverageColPos = ave;
            }

            return ave;
        }

        public static IntVec3 ApproxClosestColonistBuilding(Map map, IntVec3 from, ThingDef def)
        {
            var result = IntVec3.Invalid;
            var ave = CellsUtil.Average(null, AverageColonistPosition(map), from);

            ave.TryFindNearestColonistBuilding(map, out result, def);

            return result;
        }

        public static bool BestCarnivalSpawnSpot(Map map, out IntVec3 spot)
        {
            Func<IntVec3, bool> reachable = c => map.reachability.CanReachColony(c);
            Func<IntVec3, bool> buildable = c => c.IsAroundGoodTerrain(map, 7);
            Func<IntVec3, float> weightByLoS = c => 1f / (c.CountObstructingCellsTo(AverageColonistPosition(map), map) + 1f);
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

            // Old approach

            //if (!CellFinder.TryFindRandomEdgeCellWith(
            //    c => map.reachability.CanReachColony(c)
            //         && (map.roadInfo.roadEdgeTiles.Any() || c.IsAroundBuildableTerrain(map, 12)),
            //    map,
            //    CellFinder.EdgeRoadChance_Always,
            //    out spot))
            //{
            //    return CellFinder.TryFindRandomEdgeCellWith(
            //        c => map.reachability.CanReachColony(c),
            //        map,
            //        CellFinder.EdgeRoadChance_Always,
            //        out spot);
            //}
            //else
            //{
            //    return true;
            //}
        }


        public static IntVec3 BestCarnivalSetupPosition(IntVec3 initPos, Map map)
        {
            if (initPos.x == 0)
            {
                initPos.x += 10;
            }
            if (initPos.z == 0)
            {
                initPos.z += 10;
            }
            if (initPos.x == map.Size.x)
            {
                initPos.x -= 10;
            }
            if (initPos.z == map.Size.z)
            {
                initPos.z -= 10;
            }

            IntVec3 averageColPos;

            if (!(averageColPos = ApproxClosestColonistBuilding(map, initPos, ThingDefOf.Door)).IsValid
                && !(averageColPos = ApproxClosestColonistBuilding(map, initPos, ThingDefOf.Wall)).IsValid)
            {
                averageColPos = AverageColonistPosition(map);
            }
            else
            {
                averageColPos = averageColPos.AverageWith(AverageColonistPosition(map));
            }

            var distFromColony = initPos.DistanceTo(averageColPos);

            if (Prefs.DevMode)
                Log.Message("[Carnivale] setupSpot: initPos=" + initPos + ", averageColPos=" + averageColPos + ", distFromColony=" + distFromColony);

            var result = initPos;

            if (TryCarnivalSetupPosition_Thirds(initPos, averageColPos, distFromColony, 7, map, out result))
            {
                if (Prefs.DevMode)
                    Log.Message("[Carnivale] setupSpot: final pass: " + result + ". distFromColony=" + result.DistanceTo(averageColPos));

                return result;
            }

            Log.Error(string.Concat(new object[]
            {
                "Could not find carnival setup spot from ",
                initPos,
                ", expect more errors. Using ",
                result
            }));
            return result;
        }

        public static bool TryCarnivalSetupPosition_Triangular(IntVec3 initPos, IntVec3 colPos, float distFromColony, Map map, out IntVec3 result)
        {
            var candidateTri = CellTriangle.FromTarget(initPos, colPos, 55, MinimumDistToColony);

            result = IntVec3.Invalid;
            return false;
        }

        private static bool TryCarnivalSetupPosition_Thirds(IntVec3 initPos, IntVec3 colPos, float distFromColony, int tenth, Map map, out IntVec3 result)
        {
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
            var minDistFromColony = (int)Mathf.Min((distFromColony * tenth / 10f), (distFromColony - Info.baseRadius - 15f));
            var minSqrDistFromColony = minDistFromColony * minDistFromColony;

            var cellRect = CellRect.CenteredOn(initPos, (int)(minDistFromColony - distFromColony / 5f));
            cellRect.ClipInsideMap(map);
            cellRect = cellRect.ContractedBy((int)Info.baseRadius);

            IntVec3 randomCell;
            for (int attempt = 0; attempt < tenth * 10; attempt++)
            {
                randomCell = cellRect.RandomCell;
                if (randomCell.Standable(map)
                    && randomCell.DistanceToSquared(colPos) <= minSqrDistFromColony
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
