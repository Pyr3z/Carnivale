using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Xnope;

namespace Carnivale
{
    public static class CarnivalUtils
    {
        // Remember to flush this whenever a carnival exits the map
        private static Dictionary<Pawn, CarnivalRole> cachedRoles = new Dictionary<Pawn, CarnivalRole>();

        private static IntVec3 cachedAverageColPos = IntVec3.Invalid;

        private static CarnivalInfo cachedInfo = null;

        private static int[] trashThingDefHashes = new int[]
        {
            ThingDefOf.WoodLog.GetHashCode(),
            ThingDefOf.Steel.GetHashCode(),
            ThingDefOf.RawBerries.GetHashCode()
        };

        private static int[] carrierThingCategoryDefHashes = new int[]
        {
            ThingCategoryDefOf.Foods.GetHashCode(),
            ThingCategoryDefOf.Medicine.GetHashCode(),
            ThingCategoryDefOf.Apparel.GetHashCode(),
            ThingCategoryDefOf.Weapons.GetHashCode(),
            ThingCategoryDefOf.Drugs.GetHashCode(),
            ThingCategoryDefOf.Items.GetHashCode(),
            _DefOf.Textiles.GetHashCode(),
            //_DefOf.CarnivalThings.GetHashCode() // was causing crates to be hauled to carriers prematurely
        };


        public static CarnivalInfo Info
        {
            get
            {
                if (cachedInfo == null)
                {
                    cachedInfo = Find.VisibleMap.GetComponent<CarnivalInfo>();
                }

                return cachedInfo;
            }
        }


        public static void Cleanup()
        {
            cachedRoles.Clear();
            cachedAverageColPos = IntVec3.Invalid;
            cachedInfo = null;
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


        public static ThingDef RandomFabric()
        {
            return _DefOf.Textiles.childThingDefs.RandomElement();
        }

        public static ThingDef RandomFabricByCheapness()
        {
            return _DefOf.Textiles.childThingDefs.RandomElementByWeight(def => 1f / def.BaseMarketValue);
        }

        public static ThingDef RandomFabricByExpensiveness()
        {
            return _DefOf.Textiles.childThingDefs.RandomElementByWeight(def => def.BaseMarketValue);
        }


        public static bool IsCarnival(this Faction faction)
        {
            //return faction.def == _FactionDefOf.Carn_Faction_Roaming;
            return faction.def.defName.StartsWith("Carn_");
        }

        public static bool IsCarny(this Pawn pawn, bool checkByFaction = true)
        {
            return pawn.RaceProps.Humanlike
                && (checkByFaction && pawn.Faction != null && pawn.Faction.IsCarnival())
                || (!checkByFaction && pawn.kindDef.defName.StartsWith("Carny"));
        }


        public static bool IsOutdoors(this Pawn pawn)
        {
            var room = pawn.GetRoom();
            return room != null;
        }


        public static CarnivalRole GetCarnivalRole(this Pawn pawn, bool cache = true)
        {
            if (!pawn.Faction.IsCarnival())
            {
                Log.Error("Tried to get a CarnivalRole for " + pawn.NameStringShort + ", who is not in a carnival faction.");
                return CarnivalRole.None;
            }

            if (cache && cachedRoles.ContainsKey(pawn))
            {
                return cachedRoles[pawn];
            }

            CarnivalRole role = 0;

            switch (pawn.kindDef.defName)
            {
                case "Carny":
                    if (!pawn.story.WorkTagIsDisabled(WorkTags.Artistic))
                        role = CarnivalRole.Entertainer;

                    if (pawn.skills.GetSkill(SkillDefOf.Construction).Level > 5)
                        role |= CarnivalRole.Worker;

                    if (pawn.skills.GetSkill(SkillDefOf.Cooking).Level > 4)
                        role |= CarnivalRole.Cook;

                    //if (pawn.skills.GetSkill(SkillDefOf.Melee).Level > 6
                    //    || pawn.skills.GetSkill(SkillDefOf.Shooting).Level > 6)
                    //    role |= CarnivalRole.Guard;

                    break;
                case "CarnyRare":
                    role = CarnivalRole.Entertainer;

                    if (pawn.skills.GetSkill(SkillDefOf.Cooking).Level > 4)
                        role |= CarnivalRole.Cook;

                    break;
                case "CarnyWorker":
                    if (!pawn.story.WorkTypeIsDisabled(WorkTypeDefOf.Construction))
                        role = CarnivalRole.Worker;

                    if (pawn.skills.GetSkill(SkillDefOf.Cooking).Level > 4)
                        role |= CarnivalRole.Cook;

                    break;
                case "CarnyTrader":
                    role = CarnivalRole.Vendor;

                    if (pawn.skills.GetSkill(SkillDefOf.Cooking).Level > 4)
                        role |= CarnivalRole.Cook;

                    break;
                case "CarnyGuard":
                    role = CarnivalRole.Guard;

                    if (!pawn.story.WorkTypeIsDisabled(WorkTypeDefOf.Construction))
                        role |= CarnivalRole.Worker;

                    break;
                case "CarnyManager":
                    role = CarnivalRole.Manager;

                    //if (pawn.skills.GetSkill(SkillDefOf.Shooting).Level > 3)
                    //    role |= CarnivalRole.Guard;

                    break;
                default:
                    role = CarnivalRole.Carrier;
                    break;
            }

            if (cache) cachedRoles.Add(pawn, role);
            return role;
        }

        public static IEnumerable<CarnivalRole> GetCarnivalRolesIndividually(this Pawn pawn)
        {
            byte bitFlaggedRoles = (byte)pawn.GetCarnivalRole();

            if (bitFlaggedRoles == 0)
            {
                yield return CarnivalRole.None;
                yield break;
            }

            for (byte bitPos = 0; bitPos < 8; bitPos++)
            {
                if ((bitFlaggedRoles & (1 << bitPos)) != 0)
                {
                    yield return (CarnivalRole)bitPos;
                }
            }
        }

        public static bool Is(this Pawn pawn, CarnivalRole role, bool cache = true)
        {
            return pawn.GetCarnivalRole(cache).Is(role);
        }

        public static bool Is(this CarnivalRole roles, CarnivalRole role)
        {
            if (role == CarnivalRole.None)
                return roles == 0;
            if (role == CarnivalRole.Any)
                return roles != 0;

            return (roles & role) == role;
        }

        public static bool IsAny(this Pawn pawn, params CarnivalRole[] roles)
        {
            return pawn.GetCarnivalRole().IsAny(roles);
        }

        public static bool IsAny(this CarnivalRole pawnRole, params CarnivalRole[] roles)
        {
            foreach (var role in roles)
            {
                if (pawnRole.Is(role))
                    return true;
            }

            return false;
        }

        public static bool IsOnly(this Pawn pawn, CarnivalRole role, bool cache = true)
        {
            return pawn.GetCarnivalRole(cache) == role;
        }


        public static bool Is(this Building building, CarnBuildingType type)
        {
            return building.def.Is(type);
        }

        public static bool Is(this ThingDef def, CarnBuildingType type)
        {
            CompProperties_CarnBuilding props = def.GetCompProperties<CompProperties_CarnBuilding>();
            if (props == null)
            {
                return false;
            }

            return props.type.Is(type);
        }

        public static bool Is(this CarnBuildingType type, CarnBuildingType other)
        {
            return (type & other) == other;
        }


        public static bool IsCrate(this ThingDef def)
        {
            return def.defName.StartsWith("Carn_Crate");
        }

        public static bool IsCrate(this Thing thing)
        {
            return thing.def.IsCrate();
        }


        public static Thing SpawnThingNoWipe(Thing thing, IntVec3 loc, Map map, Rot4 rot, bool respawningAfterLoad = false)
        {
            if (map == null)
            {
                Log.Error("Tried to spawn " + thing + " in a null map.");
                return null;
            }
            if (!loc.InBounds(map))
            {
                Log.Error(string.Concat(new object[]
                {
                    "Tried to spawn ",
                    thing,
                    " out of bounds at ",
                    loc,
                    "."
                }));
                return null;
            }
            if (thing.Spawned)
            {
                Log.Error("Tried to spawn " + thing + " but it's already spawned.");
                return thing;
            }

            //GenSpawn.WipeExistingThings(loc, rot, thing.def, map, DestroyMode.Vanish);

            if (thing.def.randomizeRotationOnSpawn)
            {
                thing.Rotation = Rot4.Random;
            }
            else
            {
                thing.Rotation = rot;
            }
            thing.Position = loc;
            if (thing.holdingOwner != null)
            {
                thing.holdingOwner.Remove(thing);
            }
            thing.SpawnSetup(map, respawningAfterLoad);
            if (thing.Spawned && thing.stackCount == 0)
            {
                Log.Error("Spawned thing with 0 stackCount: " + thing);
                thing.Destroy(DestroyMode.Vanish);
                return null;
            }
            return thing;
        }

        public static Thing SpawnThingNoWipe(Thing thing, Map map, bool respawningAfterLoad)
        {
            if (map == null)
            {
                Log.Error("Tried to spawn " + thing + " in a null map.");
                return null;
            }
            if (!thing.Position.InBounds(map))
            {
                Log.Error(string.Concat(new object[]
                {
                    "Tried to spawn ",
                    thing,
                    " out of bounds at ",
                    thing.Position,
                    "."
                }));
                return null;
            }
            if (thing.Spawned)
            {
                Log.Error("Tried to spawn " + thing + " but it's already spawned.");
                return thing;
            }

            thing.SpawnSetup(map, respawningAfterLoad);

            if (thing.Spawned && thing.stackCount == 0)
            {
                Log.Error("Spawned thing with 0 stackCount: " + thing);
                thing.Destroy(DestroyMode.Vanish);
                return null;
            }
            return thing;
        }

        
        public static Lord MakeNewCarnivalLord(Faction faction, Map map, IntVec3 spawnCentre, int durationDays, IEnumerable<Pawn> startingPawns)
        {
            // This method was checked against source.
            // It is one of the only places lords should be instantiated directly.

            var lord = new Lord()
            {
                loadID = Find.World.uniqueIDsManager.GetNextLordID(),
                faction = faction
            };

            map.lordManager.AddLord(lord);

            foreach (var pawn in startingPawns)
            {
                lord.ownedPawns.Add(pawn);
                lord.numPawnsEverGained++;
            }

            Info.ReInitWith(lord, spawnCentre);

            var lordJob = new LordJob_EntertainColony(durationDays);
            lord.SetJob(lordJob);
            lord.GotoToil(lord.Graph.StartingToil);

            return lord;
        }


        public static bool FindCarnivalSpawnSpot(Map map, out IntVec3 spot)
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


        public static IntVec3 FindCarnivalSetupPosition(IntVec3 entrySpot, Map map)
        {
            if (entrySpot.x == 0)
            {
                entrySpot.x += 10;
            }
            if (entrySpot.y == 0)
            {
                entrySpot.y += 10;
            }
            if (entrySpot.x == map.Size.x)
            {
                entrySpot.x -= 10;
            }
            if (entrySpot.y == map.Size.y)
            {
                entrySpot.y -= 10;
            }

            IntVec3 averageColPos;

            if (!(averageColPos = ApproxClosestColonistBuilding(map, entrySpot, ThingDefOf.Door)).IsValid
                && !(averageColPos = ApproxClosestColonistBuilding(map, entrySpot, ThingDefOf.Wall)).IsValid)
            {
                averageColPos = AverageColonistPosition(map);
            }
            else
            {
                averageColPos = averageColPos.AverageWith(AverageColonistPosition(map));
            }

            var distFromColony = entrySpot.DistanceTo(averageColPos);

            if (Prefs.DevMode)
                Log.Message("[Carnivale] setupSpot: Initial distFromColony=" + distFromColony);

            var result = entrySpot;

            if (TryFindCarnivalSetupPositionLoS(entrySpot, averageColPos, distFromColony, 7, map, out result))
            {
                if (Prefs.DevMode)
                    Log.Message("[Carnivale] setupSpot final pass: " + result + ". distFromColony=" + result.DistanceTo(averageColPos));

                return result;
            }

            Log.Error(string.Concat(new object[]
            {
                "Could not find carnival setup spot from ",
                entrySpot,
                ", expect more errors. Using ",
                result
            }));
            return result;
        }

        private static bool TryFindCarnivalSetupPositionLoS(IntVec3 initialSpot, IntVec3 averageColPos, float distFromColony, int tenth, Map map, out IntVec3 result)
        {
            var minDistFromStart = Info.baseRadius;
            var maxDistFromStart = Mathf.Min((distFromColony * tenth / 10f), (distFromColony - Info.baseRadius - 15f));

            if (Prefs.DevMode)
                Log.Message("\t[Carnivale] minDistFromStart=" + minDistFromStart + ", maxDistFromStart=" + maxDistFromStart);

            var firstPass = CellsUtil.RandomTriangularBisections(initialSpot, averageColPos, 30f, maxDistFromStart, minDistFromStart, tenth - 2)
                            .Where(c => c.DistanceToMapEdge(map) > minDistFromStart)
                            .MaxBy(c => c.DistanceSquaredToNearestMineable(map, 25));

            IntVec3 secondPass;
            if (!firstPass.TryFindNearestRoadCell(map, Mathf.Min(maxDistFromStart - minDistFromStart, 56), out secondPass))
            {
                secondPass = firstPass;
            }

            var finalPass = CellsUtil.Average(null, firstPass, secondPass, secondPass);

            if (finalPass.Standable(map)
                && !finalPass.Roofed(map)
                && finalPass.IsAroundGoodTerrain(map, tenth * 3)
                && map.reachability.CanReach(initialSpot, finalPass, PathEndMode.OnCell, TraverseMode.NoPassClosedDoors, Danger.Some)
                && map.reachability.CanReachColony(finalPass)
                && finalPass.DistanceSquaredToNearestMineable(map, 16) > tenth * tenth * 4)
            {
                result = firstPass;
                return true;
            }
            else if (tenth > 3)
            {
                if (Prefs.DevMode)
                    Log.Warning("\t[Carnivale] setupSpot: Failed to find by line-of-sight. Will try " + (tenth - 3) + " more times.");

                return TryFindCarnivalSetupPositionLoS(initialSpot, averageColPos, distFromColony, --tenth, map, out result);
            }
            else
            {
                if (Prefs.DevMode)
                    Log.Warning("\t[Carnivale] setupSpot: Failed to find by line-of-sight. Trying old random iterative method.");

                return TryFindCarnivalSetupPositionRandomly(initialSpot, averageColPos, distFromColony, 7, map, out result);
            }
        }

        private static bool TryFindCarnivalSetupPositionRandomly(IntVec3 initialSpot, IntVec3 averageColPos, float distFromColony, int tenth, Map map, out IntVec3 result)
        {
            var minDistFromColony = (int)Mathf.Min((distFromColony * tenth / 10f), (distFromColony - Info.baseRadius - 15f));

            var minSqrDistFromColony = minDistFromColony * minDistFromColony;

            var cellRect = CellRect.CenteredOn(initialSpot, (int)(minDistFromColony - distFromColony / 5f));
            cellRect.ClipInsideMap(map);
            cellRect = cellRect.ContractedBy(10);

            IntVec3 randomCell;
            for (int attempt = 0; attempt < tenth * 10; attempt++)
            {
                randomCell = cellRect.RandomCell;
                if (randomCell.Standable(map)
                    && (averageColPos - randomCell).LengthHorizontalSquared <= minSqrDistFromColony
                    && !randomCell.Roofed(map)
                    && randomCell.IsAroundTerrainAffordances(map, tenth * 2, TerrainAffordance.Light)
                    && map.reachability.CanReach(initialSpot, randomCell, PathEndMode.OnCell, TraverseMode.NoPassClosedDoors, Danger.Some)
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

                return TryFindCarnivalSetupPositionRandomly(initialSpot, averageColPos, distFromColony, --tenth, map, out result);
            }

            result = IntVec3.Invalid;
            return false;
        }


        public static int CalculateFeePerColonist(float points)
        {
            int fee = (int)(points / (20f + Find.VisibleMap.mapPawns.FreeColonistsCount * 2)) + Rand.Range(-5, 5);

            Mathf.Clamp(fee, 9, 30);

            return fee;
        }


        public static void ClearThingsFor(Map map, IntVec3 spot, IntVec2 size, Rot4 rot, int expandedBy = 0, bool cutPlants = true, bool teleportHaulables = true)
        {
            if (!cutPlants && !teleportHaulables) return;

            CellRect removeCells = GenAdj.OccupiedRect(spot, rot, size).ExpandedBy(expandedBy);
            CellRect moveToCells = removeCells.ExpandedBy(2).ClipInsideMap(map);

            foreach (IntVec3 cell in removeCells)
            {
                if (cutPlants)
                {
                    Plant plant = cell.GetPlant(map);
                    if (plant != null && plant.def.plant.harvestWork >= 200f) // from GenConstruct.BlocksFramePlacement()
                    {
                        Designation des = new Designation(plant, DesignationDefOf.CutPlant);
                        map.designationManager.AddDesignation(des);

                        plant.SetForbiddenIfOutsideHomeArea(); // does nothing
                    }
                }

                if (teleportHaulables)
                {
                    Thing haulable = cell.GetFirstHaulable(map);
                    if (haulable != null)
                    {
                        IntVec3 moveToSpot = haulable.Position;
                        IntVec3 tempSpot;
                        for (int i = 0; i < 50; i++)
                        {
                            if (i < 15)
                            {
                                 tempSpot = moveToCells.EdgeCells.RandomElement();
                            }
                            else
                            {
                                tempSpot = moveToCells.RandomCell;
                            }

                            if (!removeCells.Contains(tempSpot))
                            {
                                if (tempSpot.Standable(map))
                                {
                                    moveToSpot = tempSpot;
                                    break;
                                }
                            }
                        }
                        if (moveToSpot == haulable.Position)
                        {
                            Log.Error("Found no spot to teleport " + haulable + " to.");
                            continue;
                        }

                        // Teleport the fucker
                        //haulable.Position = moveToSpot;
                        GenPlace.TryMoveThing(haulable, moveToSpot, map);
                    }
                }
            }
        }


        public static float Mass(this Thing thing)
        {
            return thing.GetInnerIfMinified().GetStatValue(StatDefOf.Mass) * thing.stackCount;
        }

        public static float FreeSpaceIfCarried(this Pawn carrier, Thing thing)
        {
            return Mathf.Max(0f, MassUtility.FreeSpace(carrier) - thing.Mass());
        }

        public static bool HasSpaceFor(this Pawn carrier, Thing thing)
        {
            return carrier.FreeSpaceIfCarried(thing) > 0;
        }


        public static HaulLocation DefaultHaulLocation(this ThingDef thingDef, bool haulCrates = false)
        {
            // there is a more elegant way to do this, but I'll do that later.

            if (haulCrates && thingDef.IsCrate())
            {
                return HaulLocation.ToCarriers;
            }

            if (!thingDef.EverHaulable)
            {
                return HaulLocation.None;
            }

            if (trashThingDefHashes.Contains(thingDef.GetHashCode()))
            {
                return HaulLocation.ToTrash;
            }

            var categories = thingDef.thingCategories;

            foreach (var cat in categories)
            {
                if (carrierThingCategoryDefHashes.Contains(cat.GetHashCode()))
                {
                    return HaulLocation.ToCarriers;
                }

                foreach (var parentCat in cat.Parents)
                {
                    if (carrierThingCategoryDefHashes.Contains(parentCat.GetHashCode()))
                    {
                        return HaulLocation.ToCarriers;
                    }
                }
            }

            return HaulLocation.None;
        }

        public static HaulLocation DefaultHaulLocation(this Thing thing, bool haulCrates = false)
        {
            return thing.def.DefaultHaulLocation(haulCrates);
        }


        public static Thing FindClosestThings(Pawn pawn, ThingCountClass things)
        {
            if (!Find.VisibleMap.itemAvailability.ThingsAvailableAnywhere(things, pawn))
            {
                return null;
            }

            Predicate<Thing> validator = t => !t.IsForbidden(pawn) && pawn.CanReserve(t, 1);
            return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(things.thingDef), PathEndMode.InteractionCell, TraverseParms.For(pawn, pawn.NormalMaxDanger()), 9999f, validator);
        }
    }
}
