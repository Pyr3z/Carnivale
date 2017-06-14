using RimWorld;
using System.Collections.Generic;
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



        public static IEnumerable<Pawn> FindVendors(this Lord lord)
        {
            if (!lord.faction.IsCarnival())
            {
                Log.Error("The faction under " + lord + " is not a carnival.");
                yield break;
            }
            foreach (Pawn p in lord.ownedPawns)
            {
                if (p.TraderKind != null)
                    yield return p;
            }
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

            if (pawn.TraderKind != null)
            {
                return CarnivalRole.Vendor;
            }

            switch (pawn.kindDef.defName)
            {
                case "Carny":
                    if (!pawn.story.WorkTagIsDisabled(WorkTags.Artistic))
                        role = CarnivalRole.Entertainer;

                    if (pawn.skills.GetSkill(SkillDefOf.Construction).Level > 5)
                        role |= CarnivalRole.Worker;

                    if (pawn.skills.GetSkill(SkillDefOf.Cooking).Level > 4)
                        role |= CarnivalRole.Cook;

                    if (pawn.skills.GetSkill(SkillDefOf.Melee).Level > 6
                        || pawn.skills.GetSkill(SkillDefOf.Shooting).Level > 6)
                        role |= CarnivalRole.Guard;

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
                    break;
                case "CarnyGuard":
                    role = CarnivalRole.Guard;

                    if (!pawn.story.WorkTypeIsDisabled(WorkTypeDefOf.Construction))
                        role |= CarnivalRole.Worker;

                    break;
                case "CarnyManager":
                    role = CarnivalRole.Manager;

                    if (pawn.skills.GetSkill(SkillDefOf.Shooting).Level > 3)
                        role |= CarnivalRole.Guard;

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
            CellRect cellRect = CellRect.CenteredOn(entrySpot, 60);
            cellRect.ClipInsideMap(map);
            cellRect = cellRect.ContractedBy(14);
            List<IntVec3> colonistThingsLocList = new List<IntVec3>();
            foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
            {
                colonistThingsLocList.Add(pawn.Position);
            }
            foreach (Building building in map.listerBuildings.allBuildingsColonistCombatTargets)
            {
                colonistThingsLocList.Add(building.Position);
            }
            float minDistToColonySquared = minDistToColony * minDistToColony;
            IntVec3 randomCell;
            for (int attempt = 0; attempt < 200; attempt++)
            {
                randomCell = cellRect.RandomCell;
                if (randomCell.Standable(map))
                {
                    if (randomCell.SupportsStructureType(map, TerrainAffordance.Light))
                    {
                        if (map.reachability.CanReach(randomCell, entrySpot, PathEndMode.OnCell, TraverseMode.NoPassClosedDoors, Danger.Some))
                        {
                            if (map.reachability.CanReachColony(randomCell))
                            {
                                bool flag = false;
                                for (int i = 0; i < colonistThingsLocList.Count; i++)
                                {
                                    if ((colonistThingsLocList[i] - randomCell).LengthHorizontalSquared < minDistToColonySquared)
                                    {
                                        flag = true;
                                        break;
                                    }
                                }
                                if (!flag)
                                {
                                    if (!randomCell.Roofed(map))
                                    {
                                        result = randomCell;
                                        return true;
                                    }
                                }
                            }
                        }
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
        

    }
}
