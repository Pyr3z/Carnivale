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
                return Info.baseRadius * 2.5f;
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
            var halfAngle = Mathf.Lerp(30f, 10f, distSqrToColony / map.Size.LengthHorizontalSquared);

            if (Prefs.DevMode)
                Log.Message("\t[Carnivale] triangular setupSpot: MinDistToMapEdge=" + MinDistToMapEdge + ", MinDistToColony=" + MinDistToColony + ", halfAngle=" + halfAngle);

            var candidateTri = CellTriangle
                .FromTarget(colPos, initPos, halfAngle, distFromColony - 10f)
                .ClipInside(mapRect);

            Func<IntVec3, bool> reachable = c => map.reachability.CanReachColony(c);
            Func<IntVec3, bool> validDist = c => c.DistanceToSquared(colPos) >= minDistSqrToColony;
            //Func<IntVec3, bool> buildable = c => c.IsAroundGoodTerrain(map, 3);
            Func<IntVec3, float> weightBuildable = c => 1f / (c.CountBadTerrainInRadius(map, 7) + 1f);
            Func<IntVec3, float> weightDist = c => Rand.Range(0, 20) + distSqrToColony / (c.DistanceToSquared(colPos) + 1f);
            Func<IntVec3, float> weightRoad = c => !map.roadInfo.roadEdgeTiles.Any() ? 0f : Info.baseRadius / (c.DistanceSquaredToNearestRoad(map, Info.baseRadius) + 1f);

            var candidateCells = candidateTri
                .Where(c => validDist(c) && reachable(c));

            try
            {
                result = candidateCells.MaxBy(weightDist + weightRoad + weightBuildable);

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

            var minDistSqrdToCentre = minDistToCentre * minDistToCentre;
            var maxDistSqrdToCentre = maxDistToCentre * maxDistToCentre;

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

            // Triangular spread pass

            //var candidateTri = CellTriangle.FromTarget(closestCell, colonistPos, 55f, (maxDistToCentre));

            //closestCell = candidateTri.B.AverageWith(candidateTri.C);

            //if (Prefs.DevMode)
            //    Log.Message("\t[Carnivale] bannerCell closest to colony pass: " + closestCell);

            // Road pass

            IntVec3 road;
            if (closestCell.TryFindNearestRoadCell(map, baseRadius - 5, out road))
            {
                maxDistSqrdToCentre += 16;

                var found = false;
                foreach (var rcell in GenRadial.RadialCellsAround(road, 7, true))
                {
                    var distSqrdToCentre = rcell.DistanceToSquared(setupCentre);
                    var isRoad = rcell.GetTerrain(map).HasTag("Road");

                    if (isRoad && distSqrdToCentre < maxDistSqrdToCentre && distSqrdToCentre > minDistSqrdToCentre)
                    {
                        // Found the edge of a road, try to centre it
                        var adjustedCell = road;

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
                        found = true;

                        if (Prefs.DevMode)
                            Log.Message("\t[Carnivale] bannerCell road pass: " + closestCell + ". distSqrdToCentre=" + closestCell.DistanceToSquared(setupCentre) + ", minDistSqrdToCentre=" + minDistSqrdToCentre + ", maxDistSqrdToCentre=" + maxDistSqrdToCentre);

                        break;
                    }
                }

                if (!found && Prefs.DevMode)
                {
                    Log.Warning("\t[Carnivale] bannerCell road pass failed. Reason: out of range from setupCentre. Initial road=" + road);
                }
            }
            else if (map.roadInfo.roadEdgeTiles.Any() && Prefs.DevMode)
            {
                Log.Warning("\t[Carnivale] bannerCell road pass failed. Reason: no roads found in search radius. searchRadius=" + (baseRadius - 5));
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

                //IntVec3 tempCell;

                //if (candidateCells.TryRandomElementByWeight(weightBest, out tempCell))
                //{
                //    closestCell = tempCell;
                //    if (Prefs.DevMode)
                //        Log.Message("\t[Carnivale] bannerCell optimal LoS pass: " + closestCell);
                //}
                //else if (candidateCells.TryRandomElementByWeight(weightLoSSetupCentre + weightLoSColony, out tempCell))
                //{
                //    closestCell = tempCell;
                //    if (Prefs.DevMode)
                //        Log.Message("\t[Carnivale] bannerCell sub-optimal LoS pass: " + closestCell);
                //}
                //else
                //{
                //    if (Prefs.DevMode)
                //        Log.Warning("\t[Carnivale] bannerCell failed all LoS passes. Is candidateCells empty? Leaving it at: " + closestCell);
                //}
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
