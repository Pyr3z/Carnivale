using RimWorld;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;

namespace Carnivale
{
    public static class CarnivalBlueprints
    {
        private static CellRect area;

        private static IntVec3 entryCell;

        private static List<Thing> availableCrates;

        private static List<Pawn> stallUsers;

        private static Faction faction;

        // The only public method; use this
        [DebuggerHidden]
        public static IEnumerable<Blueprint> PlaceCarnivalBlueprints(LordToilData_Carnival data, Map map, Faction faction)
        {
            area = CellRect.CenteredOn(data.setupSpot, (int)data.baseRadius).ClipInsideMap(map);

            IntVec3 colonistPos = map.listerBuildings.allBuildingsColonist.NullOrEmpty() ?
                map.mapPawns.FreeColonistsSpawned.RandomElement().Position : map.listerBuildings.allBuildingsColonist.RandomElement().Position;

            entryCell = area.ClosestCellTo(colonistPos);
            availableCrates = data.availableCrates;
            stallUsers = ((List<Pawn>)data.pawnsWithRole[CarnivalRole.Vendor]).ListFullCopyOrNull();
            CarnivalBlueprints.faction = faction;

            foreach (Blueprint_StuffHacked tent in PlaceTentBlueprints(map))
            {
                yield return tent;
            }

            foreach (Blueprint_Build stall in PlaceStallBlueprints(map))
            {
                yield return stall;
            }

            //yield return PlaceEntranceBlueprint(map);
        }


        private static IEnumerable<Blueprint> PlaceTentBlueprints(Map map)
        {
            int numTents = 0;
            foreach (Thing crate in availableCrates)
            {
                if (crate.def == _DefOf.Carn_Crate_TentLodge)
                    numTents++;
            }

            ThingDef tentDef = _DefOf.Carn_TentMedBed;
            Rot4 rot = Rot4.Random;
            IntVec3 tentSpot = FindRandomPlacementFor(tentDef, rot, map, true);

            IntVec3 lineDirection;

            switch (rot.AsByte)
            {
                // Want to draw an even line of tents with the same rotation
                case 0: // North
                    lineDirection = IntVec3.East;
                    break;
                case 1: // East
                    lineDirection = IntVec3.North;
                    break;
                case 2: // South
                    lineDirection = IntVec3.West;
                    break;
                case 3: // West
                    lineDirection = IntVec3.South;
                    break;
                default:
                    lineDirection = IntVec3.Invalid;
                    break;
            }

            // Place lodging tents (8 pawns per medium sized tent)
            for (int i = 0; i < numTents; i++)
            {
                // Following works as intended iff size.x == size.y

                // Distance between tents is 1 cell
                tentSpot += lineDirection * ((tentDef.size.x + 1) * i);

                if (CanPlaceBlueprintAt(tentSpot, rot, tentDef, map))
                {
                    // Insta-cut plants (potentially OP?)
                    RemovePlantsFor(tentSpot, tentDef.size, rot, map);
                    yield return GenConstruct.PlaceBlueprintForBuild(tentDef, tentSpot, map, rot, faction, null);
                }
                else
                {
                    // Find new placement
                    tentSpot = FindRandomPlacementFor(tentDef, rot, map);
                    i--;
                }
            }

            // Place manager tent
            if (!availableCrates.Any(c => c.def == _DefOf.Carn_Crate_TentMan))
                yield break;

            rot = Rot4.Random;
            tentDef = _DefOf.Carn_TentSmallMan;
            tentSpot = FindRandomPlacementFor(tentDef, rot, map, true);

            if (tentSpot.IsValid)
            {
                // Insta-cut plants (potentially OP?)
                RemovePlantsFor(tentSpot, tentDef.size, rot, map);
                yield return (Blueprint_StuffHacked)GenConstruct.PlaceBlueprintForBuild(tentDef, tentSpot, map, rot, faction, null);
            }

        }

        private static IEnumerable<Blueprint_Build> PlaceStallBlueprints(Map map)
        {
            // Default stall is food for now
            ThingDef stallDef = _DefOf.Carn_StallFood;
            IntVec3 stallSpot = IntVec3.Invalid;
            Rot4 rot = default(Rot4);
            CellRect stallArea = area;

            foreach (Pawn pawn in stallUsers)
            {
                if (pawn.TraderKind != null)
                {
                    if (pawn.TraderKind == _DefOf.Carn_Trader_Food)
                    {
                        stallDef = _DefOf.Carn_StallFood;
                    }
                    else if (pawn.TraderKind == _DefOf.Carn_Trader_Surplus)
                    {
                        // TODO
                    }
                    else if (pawn.TraderKind == _DefOf.Carn_Trader_Curios)
                    {
                        // TODO
                    }
                    else
                    {
                        Log.Error("Trader " + pawn.NameStringShort + " is not a carnival trader and will get no stall.");
                        continue;
                    }
                }

                if (stallSpot.IsValid)
                {
                    // Next spot should be close to last spot
                    stallSpot = FindRandomPlacementFor(stallDef, rot, map, stallArea);
                }
                else
                {
                    // Find random initial spot
                    stallSpot = FindRandomPlacementFor(stallDef, rot, map, false, 8);
                    stallArea = CellRect.CenteredOn(stallSpot, 8);
                }


                // Finally, spawn the fucker
                if (stallSpot.IsValid)
                {
                    RemovePlantsFor(stallSpot, stallDef.size, rot, map);
                    yield return GenConstruct.PlaceBlueprintForBuild(stallDef, stallSpot, map, rot, faction, null);
                }
            }
        }

        private static Blueprint_Build PlaceEntranceBlueprint(Map map)
        {

            return null;
        }




        private static bool CanPlaceBlueprintAt(IntVec3 spot, Rot4 rot, ThingDef def, Map map)
        {
            if (!spot.IsValid) return false;
            return GenConstruct.CanPlaceBlueprintAt(def, spot, rot, map, false, null).Accepted;
        }


        private static IntVec3 FindRandomPlacementFor(ThingDef def, Rot4 rot, Map map, bool preferFarFromColony = false, int contractedBy = 0)
        {
            CellRect noGo = CellRect.CenteredOn(entryCell, area.Width / 4);

            CellRect adjustedArea = area.ContractedBy(contractedBy);

            for (int i = 0; i < 200; i++)
            {
                IntVec3 randomCell = adjustedArea.RandomCell;

                if (preferFarFromColony && noGo.Contains(randomCell))
                    continue;

                if (map.reachability.CanReach(randomCell, area.CenterCell, Verse.AI.PathEndMode.OnCell, TraverseMode.NoPassClosedDoors, Danger.Deadly))
                {
                    if (!randomCell.Roofed(map))
                    {
                        if (CanPlaceBlueprintAt(randomCell, rot, def, map))
                        {
                            return randomCell;
                        }
                    }
                }
            }
            return IntVec3.Invalid;
        }

        private static IntVec3 FindRandomPlacementFor(ThingDef def, Rot4 rot, Map map, CellRect otherArea)
        {
            for (int i = 0; i < 200; i++)
            {
                IntVec3 randomCell = otherArea.RandomCell;

                if (map.reachability.CanReach(randomCell, area.CenterCell, Verse.AI.PathEndMode.OnCell, TraverseMode.NoPassClosedDoors, Danger.Deadly))
                {
                    if (!randomCell.Roofed(map))
                    {
                        if (CanPlaceBlueprintAt(randomCell, rot, def, map))
                        {
                            return randomCell;
                        }
                    }
                }
            }
            return IntVec3.Invalid;
        }


        private static void RemovePlantsFor(IntVec3 spot, IntVec2 size, Rot4 rot, Map map)
        {
            CellRect cutCells = GenAdj.OccupiedRect(spot, rot, size);
            foreach (IntVec3 cell in cutCells)
            {
                Plant p = cell.GetPlant(map);
                if (p != null)
                {
                    p.Destroy(DestroyMode.KillFinalize);
                }
            }
        }


        
    }
}
