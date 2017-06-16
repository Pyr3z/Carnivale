using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Carnivale
{
    public static class Utilities
    {
        // Remember to flush this whenever a carnival exits the map
        public static Dictionary<Pawn, CarnivalRole> cachedRoles = new Dictionary<Pawn, CarnivalRole>();

        public static ThingDef[] simpleFabricStuffs = new ThingDef[]
        {
            _DefOf.Cloth,
            _DefOf.WoolAlpaca,
            _DefOf.WoolCamel,
            _DefOf.WoolMegasloth,
            _DefOf.WoolMuffalo
        };

        public static ThingDef RandomSimpleFabricByValue()
        {
            return simpleFabricStuffs.RandomElementByWeight(d => d.BaseMarketValue);
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


        public static void SetRoofFor(CellRect rect, Map map, RoofDef roof)
        {
            // pass a null RoofDef to clear a roof
            for (int i = rect.minZ; i <= rect.maxZ; i++)
            {
                for (int j = rect.minX; j <= rect.maxX; j++)
                {
                    IntVec3 cell = new IntVec3(j, 0, i);
                    map.roofGrid.SetRoof(cell, roof);
                }
            }
        }



        public static IEnumerable<IntVec3> CornerlessEdgeCells(CellRect rect)
        {
            int x = rect.minX + 1;
            int z = rect.minZ;
            while (x < rect.maxX)
            {
                yield return new IntVec3(x, 0, z);
                x++;
            }
            for (z++; z < rect.maxZ; z++)
            {
                yield return new IntVec3(x, 0, z);
            }
            for (x--; x > rect.minX; x--)
            {
                yield return new IntVec3(x, 0, z);
            }
            for (z--; z > rect.minZ; z--)
            {
                yield return new IntVec3(x, 0, z);
            }
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
                    && map.reachability.CanReachColony(randomCell))
                {
                    // We don't want to be within 12 cells of water
                    bool water = false;
                    foreach (var cell in GenRadial.RadialCellsAround(randomCell, 12, true))
                    {
                        if (cell.GetTerrain(map).HasTag("Water"))
                        {
                            water = true;
                            break;
                        }
                    }

                    if (!water)
                    {
                        result = randomCell;
                        return true;
                    }
                }
            }
            result = IntVec3.Invalid;
            return false;
        }


        public static IntVec3 Average(this IEnumerable<IntVec3> vecs)
        {
            int totalX = 0;
            int totalZ = 0;
            int count = 0;

            foreach (IntVec3 vec in vecs)
            {
                totalX += vec.x;
                totalZ += vec.z;
                count++;
            }

            return new IntVec3(totalX / count, 0, totalZ / count);
        }


        public static IEnumerable<IntVec3> CellsInLineTo(this IntVec3 a, IntVec3 b, bool debug = false)
        {
            if (!a.InBounds(Find.VisibleMap) || !b.InBounds(Find.VisibleMap))
            {
                Log.Error("Cell out of map bounds. a=" + a + " b=" + b);
            }

            if (debug)
                Log.Warning("[Debug] (" + a.x + ", 0, " + a.z + ")");

            yield return a;


            int dx = b.x - a.x;
            int dz = b.z - a.z;

            int x = a.x;
            int z = a.z;

            int d;
            int r;

            int dxa = Mathf.Abs(dx);
            int dza = Mathf.Abs(dz);

            if (dxa > dza)
            {
                d = dxa / (dza + 1);
                r = dxa % (dza + 1);
            }
            else if (dxa < dza)
            {
                d = dza / (dxa + 1);
                r = dza % (dxa + 1);
            }
            else
            {
                d = dxa;
                r = 0;
            }

            while (dx != 0 || dz != 0)
            {
                // handle straight lines
                if (dx == 0 && dz != 0)
                {
                    if (dz > 0)
                    {
                        z++;
                        dz--;
                    }
                    else
                    {
                        z--;
                        dz++;
                    }

                    if (debug)
                        Log.Warning("[Debug] (" + x + ", 0, " + z + ")");

                    yield return new IntVec3(x, 0, z);
                }
                else if (dz == 0 && dx != 0)
                {
                    if (dx > 0)
                    {
                        x++;
                        dx--;
                    }
                    else
                    {
                        x--;
                        dx++;
                    }

                    if (debug)
                        Log.Warning("[Debug] (" + x + ", 0, " + z + ")");

                    yield return new IntVec3(x, 0, z);
                }
                else
                {
                    // non-straight lines
                    for (int i = 0; i < d; i++)
                    {
                        if (dx == -dz && dx != 0)
                        {
                            if (dx > dz)
                            {
                                x++;
                                z--;
                                dx--;
                                dz++;
                            }
                            else
                            {
                                x--;
                                z++;
                                dx++;
                                dz--;
                            }
                        }
                        else if (dx < dz)
                        {
                            if (dx > 0 || dza > dxa)
                            {
                                if (dz > 0)
                                {
                                    z++;
                                    dz--;
                                }
                                else
                                {
                                    z--;
                                    dz++;
                                }
                            }
                            else
                            {
                                x--;
                                dx++;
                            }
                        }
                        else if (dx > dz)
                        {
                            if (dz > 0 || dxa > dza)
                            {
                                if (dx > 0)
                                {
                                    x++;
                                    dx--;
                                }
                                else
                                {
                                    x--;
                                    dx++;
                                }
                            }
                            else
                            {
                                z--;
                                dz++;
                            }
                        }
                        else if (dx == dz && dx != 0)
                        {
                            if (dx > 0)
                            {
                                x++;
                                z++;
                                dx--;
                                dz--;
                            }
                            else
                            {
                                x--;
                                z--;
                                dx++;
                                dz++;
                            }
                        }
                        else // dx == dz && dx == 0
                        {
                            break;
                        }

                        if (debug)
                            Log.Warning("[Debug] (" + x + ", 0, " + z + ")");

                        yield return new IntVec3(x, 0, z);
                    }

                    // handle increment
                    if (dx > dz && dz != 0)
                    {
                        if (dz > 0)
                        {
                            z++;
                            dz--;
                        }
                        else
                        {
                            z--;
                            dz++;
                        }

                        // handle remainder
                        if (r != 0)
                        {
                            if (dx > 0)
                            {
                                x++;
                                dx--;
                                r--;
                            }
                            else if (dx < 0)
                            {
                                x--;
                                dx++;
                                r--;
                            }
                        }

                        if (debug)
                            Log.Warning("[Debug] (" + x + ", 0, " + z + ")");

                        yield return new IntVec3(x, 0, z);
                    }
                    else if (dx < dz && dx != 0)
                    {
                        if (dx > 0)
                        {
                            x++;
                            dx--;
                        }
                        else
                        {
                            x--;
                            dx++;
                        }

                        // handle remainder
                        if (r != 0)
                        {
                            if (dz > 0)
                            {
                                z++;
                                dz--;
                                r--;
                            }
                            else if (dz < 0)
                            {
                                z--;
                                dz++;
                                r--;
                            }
                        }

                        if (debug)
                            Log.Warning("[Debug] (" + x + ", 0, " + z + ")");

                        yield return new IntVec3(x, 0, z);
                    }
                    
                }

                

            } // end while

        }


        public static void ClearThingsFor(Map map, IntVec3 spot, IntVec2 size, Rot4 rot, bool clearPlants = true, bool teleportHaulables = true)
        {
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

                if (teleportHaulables)
                {
                    Thing haulable = cell.GetFirstHaulable(map);
                    if (haulable != null)
                    {
                        IntVec3 moveToSpot = haulable.Position;
                        for (int i = 0; i < 30; i++)
                        {
                            IntVec3 tempSpot = moveToCells.RandomCell;
                            if (!removeCells.Contains(tempSpot))
                            {
                                if (!tempSpot.GetThingList(map).Any())
                                {
                                    moveToSpot = tempSpot;
                                    break;
                                }
                            }
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
            return  carrier.FreeSpaceIfCarried(thing) > 0;
        }


        public static void TryThrowTextMotes(this Pawn pawn)
        {

        }

    }
}
