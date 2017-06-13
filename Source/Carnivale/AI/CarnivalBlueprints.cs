using RimWorld;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Verse;

namespace Carnivale
{
    public static class CarnivalBlueprints
    {
        private static CarnivalInfo info;

        private static CellRect area;

        private static IntVec3 bannerCell;

        private static List<Thing> availableCrates;

        private static List<Pawn> stallUsers;

        private static Faction faction;

        // The only public method; use this
        [DebuggerHidden]
        public static IEnumerable<Blueprint> PlaceCarnivalBlueprints(CarnivalInfo info)
        {
            // Assign necessary values to this singleton (is this technically a singleton?)
            CarnivalBlueprints.info = info;
            area = info.carnivalArea;
            bannerCell = info.bannerCell;
            availableCrates = ((LordToilData_Setup)info.currentLord.CurLordToil.data).availableCrates;
            stallUsers = ((List<Pawn>)info.pawnsWithRole[CarnivalRole.Vendor]).ListFullCopyOrNull();
            faction = info.currentLord.faction;



            // Do the blueprint thing

            foreach (Blueprint tent in PlaceTentBlueprints(info.map))
            {
                yield return tent;
            }

            foreach (Blueprint stall in PlaceStallBlueprints(info.map))
            {
                yield return stall;
            }

            yield return PlaceEntranceBlueprint(info.map);
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
                    RemovePlantsAndTeleportHaulablesFor(tentSpot, tentDef.size, rot, map);
                    yield return GenConstruct.PlaceBlueprintForBuild(tentDef, tentSpot, map, rot, faction, null);
                }
                else
                {
                    // Find new placement if next spot doesn't work
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
                RemovePlantsAndTeleportHaulablesFor(tentSpot, tentDef.size, rot, map);
                yield return GenConstruct.PlaceBlueprintForBuild(tentDef, tentSpot, map, rot, faction, null);
            }

        }

        private static IEnumerable<Blueprint_Build> PlaceStallBlueprints(Map map)
        {
            // Default stall is food for now
            ThingDef stallDef = _DefOf.Carn_StallFood;
            IntVec3 stallSpot = IntVec3.Invalid;
            Rot4 rot = default(Rot4);
            CellRect stallArea = area;

            foreach (Pawn stallUser in stallUsers)
            {
                if (stallUser.TraderKind != null)
                {
                    // Handle vendor stalls

                    if (stallUser.TraderKind == _DefOf.Carn_Trader_Food)
                    {
                        stallDef = _DefOf.Carn_StallFood;
                    }
                    else if (stallUser.TraderKind == _DefOf.Carn_Trader_Surplus)
                    {
                        stallDef = _DefOf.Carn_StallSurplus;
                    }
                    else if (stallUser.TraderKind == _DefOf.Carn_Trader_Curios)
                    {
                        stallDef = _DefOf.Carn_StallCurios;
                    }
                    else
                    {
                        Log.Error("Trader " + stallUser.NameStringShort + " is not a carnival vendor and will get no stall.");
                        continue;
                    }
                }



                if (stallSpot.IsValid)
                {
                    // Next spot should be close to last spot
                    stallSpot = FindRandomPlacementFor(stallDef, rot, map, stallArea, stallSpot);
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
                    RemovePlantsAndTeleportHaulablesFor(stallSpot, stallDef.size, rot, map);
                    // Add spot to stall user's spot
                    info.rememberedPositions.Add(stallUser, stallSpot);
                    yield return GenConstruct.PlaceBlueprintForBuild(stallDef, stallSpot, map, rot, faction, null);
                }
            }
        }

        private static Blueprint PlaceEntranceBlueprint(Map map)
        {
            ThingDef bannerDef = _DefOf.Carn_SignEntry;
            Rot4 rot = default(Rot4);

            if (CanPlaceBlueprintAt(bannerCell, rot, bannerDef, map))
            {
                RemovePlantsAndTeleportHaulablesFor(bannerCell, bannerDef.size, rot, map);
                return GenConstruct.PlaceBlueprintForBuild(bannerDef, bannerCell, map, rot, faction, null);
            }

            // If cannot place on bannerCell, try in a small area around it

            CellRect tryArea = CellRect.CenteredOn(bannerCell, 8).ClipInsideRect(area);
            IntVec3 bannerSpot = FindRandomPlacementFor(bannerDef, rot, map, tryArea);

            if (bannerSpot.IsValid)
            {
                RemovePlantsAndTeleportHaulablesFor(bannerSpot, bannerDef.size, rot, map);
                return GenConstruct.PlaceBlueprintForBuild(bannerDef, bannerSpot, map, rot, faction, null);
            }

            // If that fails, try any area in the carnival area (suboptimal)

            bannerSpot = FindRandomPlacementFor(bannerDef, rot, map);
            RemovePlantsAndTeleportHaulablesFor(bannerSpot, bannerDef.size, rot, map);
            return GenConstruct.PlaceBlueprintForBuild(bannerDef, bannerSpot, map, rot, faction, null);
        }




        private static bool CanPlaceBlueprintAt(IntVec3 spot, Rot4 rot, ThingDef def, Map map)
        {
            if (!spot.IsValid) return false;

            // Cheaty cheaty
            bool isEdifice = def.IsEdifice();

            def.building.isEdifice = true;

            bool result = GenConstruct.CanPlaceBlueprintAt(def, spot, rot, map, false, null).Accepted;

            def.building.isEdifice = isEdifice;

            return result;
        }


        private static IntVec3 FindRandomPlacementFor(ThingDef def, Rot4 rot, Map map, bool preferFarFromColony = false, int contractedBy = 0)
        {
            CellRect noGo = CellRect.CenteredOn(bannerCell, area.Width / 2);

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

        private static IntVec3 FindRandomPlacementFor(ThingDef def, Rot4 rot, Map map, CellRect otherArea, IntVec3 preferCardinalAdjacentTo = default(IntVec3))
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
                            if (preferCardinalAdjacentTo != default(IntVec3))
                            {
                                return randomCell;
                            }

                            if (randomCell.AdjacentToCardinal(preferCardinalAdjacentTo))
                            {
                                return randomCell;
                            }
                        }
                    }
                }
            }
            return IntVec3.Invalid;
        }



        private static void RemovePlantsAndTeleportHaulablesFor(IntVec3 spot, IntVec2 size, Rot4 rot, Map map)
        {
            CellRect removeCells = GenAdj.OccupiedRect(spot, rot, size);
            // Not sure if CellRects are immutable upon assignment but w/e
            CellRect moveToCells = new CellRect(removeCells.minX, removeCells.minZ, removeCells.maxX, removeCells.maxZ).ExpandedBy(2).ClipInsideMap(map);

            foreach (IntVec3 cell in removeCells)
            {
                Plant plant = cell.GetPlant(map);
                if (plant != null)
                {
                    plant.Destroy(DestroyMode.KillFinalize);
                }

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
                    haulable.Position = moveToSpot;
                }
            }
        }
        
    }
}
