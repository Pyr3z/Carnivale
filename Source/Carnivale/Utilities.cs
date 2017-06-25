using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Xnope;

namespace Carnivale
{
    public static class Utilities
    {
        // Remember to flush this whenever a carnival exits the map
        public static Dictionary<Pawn, CarnivalRole> cachedRoles = new Dictionary<Pawn, CarnivalRole>();

        public static ThingDef[] defaultTrashThings = new ThingDef[]
        {
            ThingDefOf.WoodLog,
            ThingDefOf.Steel
        };



        public static ThingDef RandomFabric()
        {
            return _DefOf.Textiles.childThingDefs.RandomElement();
        }

        public static ThingDef RandomFabricByCheapness()
        {
            return _DefOf.Textiles.childThingDefs.RandomElementByWeight(def => 10f / def.BaseMarketValue);
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

        public static bool IsCarny(this Pawn pawn)
        {
            return pawn.RaceProps.Humanlike && pawn.Faction != null && pawn.Faction.IsCarnival();
        }



        public static CarnivalRole GetCarnivalRole(this Pawn pawn)
        {
            if (!pawn.Faction.IsCarnival())
            {
                Log.Error("Tried to get a CarnivalRole for " + pawn.NameStringShort + ", who is not in a carnival faction.");
                return CarnivalRole.None;
            }

            if (cachedRoles.ContainsKey(pawn))
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

            cachedRoles.Add(pawn, role);
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

        public static bool Is(this Pawn pawn, CarnivalRole role)
        {
            return pawn.GetCarnivalRole().Is(role);
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

        public static bool IsOnly(this Pawn pawn, CarnivalRole role)
        {
            return pawn.GetCarnivalRole() == role;
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


        public static bool IsCrate(this Thing thing)
        {
            return thing.def.defName.StartsWith("Carn_Crate");
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

        

        public static IntVec3 FindCarnivalSetupPositionFrom(IntVec3 entrySpot, Map map)
        {
            // Copy of internal methods from RCellFinder (why the feck are they internal??)
            for (int minDistToColony = 70; minDistToColony >= 20; minDistToColony -= 10)
            {
                IntVec3 result;
                if (TryFindCarnivalSetupPosition(entrySpot, minDistToColony, map, out result))
                {
                    return result;
                }
            }
            Log.Error(string.Concat(new object[]
            {
                "Could not find carnival setup spot from ",
                entrySpot,
                ", using ",
                entrySpot
            }));
            return entrySpot;
        }

        private static bool TryFindCarnivalSetupPosition(IntVec3 entrySpot, float minDistToColony, Map map, out IntVec3 result)
        {
            CellRect cellRect = CellRect.CenteredOn(entrySpot, 80);
            cellRect.ClipInsideMap(map);
            cellRect = cellRect.ContractedBy(16);

            List<IntVec3> colonistThingsLocList = new List<IntVec3>();
            foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
            {
                colonistThingsLocList.Add(pawn.Position);
            }
            foreach (Building building in map.listerBuildings.allBuildingsColonistCombatTargets)
            {
                colonistThingsLocList.Add(building.Position);
            }

            IntVec3 averageColonyPos = colonistThingsLocList.Average();
            float minDistToColonySquared = minDistToColony * minDistToColony;

            IntVec3 randomCell;
            for (int attempt = 0; attempt < 200; attempt++)
            {
                randomCell = cellRect.RandomCell;
                if (randomCell.Standable(map)
                    && (averageColonyPos - randomCell).LengthHorizontalSquared > minDistToColonySquared
                    && !randomCell.Roofed(map)
                    && randomCell.SupportsStructureType(map, TerrainAffordance.Light)
                    && map.reachability.CanReach(randomCell, entrySpot, PathEndMode.OnCell, TraverseMode.NoPassClosedDoors, Danger.Some)
                    && map.reachability.CanReachColony(randomCell)
                    && (map.roadInfo.roadEdgeTiles.Any() || !randomCell.IsAroundTerrainOfTag(map, 12, "Water")))
                {
                    result = randomCell;
                    return true;
                }
            }
            result = IntVec3.Invalid;
            return false;
        }


        /// <summary>
        /// Clears a CellRect of plants and/or haulables, either forcibly or 'gently'.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="spot"></param>
        /// <param name="size"></param>
        /// <param name="rot"></param>
        /// <param name="clearPlants">Forcibly destroys all plants if true, 'gently' designates them for cutting if false.</param>
        /// <param name="teleportHaulables">Forcibly tries to teleport haulables outside the occupied rectangle if true.</param>
        public static void ClearThingsFor(Map map, IntVec3 spot, IntVec2 size, Rot4 rot, bool clearPlants = true, bool teleportHaulables = true)
        {
            if (!clearPlants && !teleportHaulables) return;

            CellRect removeCells = GenAdj.OccupiedRect(spot, rot, size);
            // Not sure if CellRects are immutable upon assignment?
            CellRect moveToCells = removeCells.ExpandedBy(2).ClipInsideMap(map);

            foreach (IntVec3 cell in removeCells)
            {
                if (clearPlants)
                {
                    Plant plant = cell.GetPlant(map);
                    if (plant != null)
                    {
                        plant.Destroy(DestroyMode.KillFinalize);
                    }
                }
                else
                {
                    Plant plant = cell.GetPlant(map);
                    if (plant != null && plant.def.plant.harvestWork >= 200f) // from GenConstruct.BlocksFramePlacement()
                    {
                        Designation des = new Designation(plant, DesignationDefOf.CutPlant);
                        map.designationManager.AddDesignation(des);

                        plant.SetForbiddenIfOutsideHomeArea();
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

        public static void DesignateAllPlantsForCut(this CellRect rect, Map map)
        {
            rect.ClipInsideMap(map);

            foreach (var cell in rect)
            {
                Plant plant = cell.GetPlant(map);
                if (plant != null)
                {
                    Designation des = new Designation(plant, DesignationDefOf.CutPlant);
                    map.designationManager.AddDesignation(des);
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


        public static HaulLocation GetHaulToLocation(Thing thing)
        {
            // there is a more elegant way to do this, but I'll do that later.

            if (defaultTrashThings.Contains(thing.def))
            {
                return HaulLocation.ToTrash;
            }

            if (thing.def.IsWithinCategory(ThingCategoryDefOf.Foods)
                || thing.def.IsWithinCategory(ThingCategoryDefOf.ResourcesRaw)
                || thing.IsCrate())
            {
                return HaulLocation.ToCarriers;
            }

            

            return HaulLocation.None;
        }


        public static void TryThrowTextMotes(this Pawn pawn)
        {

        }


    }
}
