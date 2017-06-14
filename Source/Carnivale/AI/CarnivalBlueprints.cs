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

        private static List<Thing> availableCrates;

        private static List<Pawn> stallUsers;

        // The only public method; use this
        [DebuggerHidden]
        public static IEnumerable<Blueprint> PlaceCarnivalBlueprints(CarnivalInfo info)
        {
            // Assign necessary values to this singleton (is this technically a singleton?)
            CarnivalBlueprints.info = info;
            
            if (info.currentLord.CurLordToil.data is LordToilData_Setup)
            {
                availableCrates = ((LordToilData_Setup)info.currentLord.CurLordToil.data).availableCrates;
                stallUsers = ((List<Pawn>)info.pawnsWithRole[CarnivalRole.Vendor]).ListFullCopyOrNull();
            }
            else
            {
                Log.Error("Tried to place carnival blueprints while not in setup toil.");
                availableCrates = null;
                stallUsers = null;
            }



            // Do the blueprint thing

            if (!availableCrates.NullOrEmpty())
            {
                foreach (Blueprint tent in PlaceTentBlueprints())
                {
                    yield return tent;
                }

                if (!stallUsers.NullOrEmpty())
                {
                    foreach (Blueprint stall in PlaceStallBlueprints())
                    {
                        yield return stall;
                    }
                }

                yield return PlaceEntranceBlueprint();
            }
        }


        private static IEnumerable<Blueprint> PlaceTentBlueprints()
        {
            int numTents = 0;
            foreach (Thing crate in availableCrates)
            {
                if (crate.def == _DefOf.Carn_Crate_TentLodge)
                    numTents++;
            }

            ThingDef tentDef = _DefOf.Carn_TentMedBed;
            Rot4 rot = Rot4.Random;
            IntVec3 tentSpot = FindRandomPlacementFor(tentDef, rot, true);

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

                if (CanPlaceBlueprintAt(tentSpot, rot, tentDef))
                {
                    // Insta-cut plants (potentially OP?)
                    RemovePlantsAndTeleportHaulablesFor(tentSpot, tentDef.size, rot);
                    yield return GenConstruct.PlaceBlueprintForBuild(tentDef, tentSpot, info.map, rot, info.currentLord.faction, null);
                }
                else
                {
                    // Find new placement if next spot doesn't work
                    tentSpot = FindRandomPlacementFor(tentDef, rot);
                    i--;
                }
            }

            // Place manager tent
            if (!availableCrates.Any(c => c.def == _DefOf.Carn_Crate_TentMan))
                yield break;

            rot = Rot4.Random;
            tentDef = _DefOf.Carn_TentSmallMan;
            tentSpot = FindRandomPlacementFor(tentDef, rot, true);

            if (tentSpot.IsValid)
            {
                RemovePlantsAndTeleportHaulablesFor(tentSpot, tentDef.size, rot);
                yield return GenConstruct.PlaceBlueprintForBuild(tentDef, tentSpot, info.map, rot, info.currentLord.faction, null);
            }

        }

        private static IEnumerable<Blueprint> PlaceStallBlueprints()
        {
            // Default stall is food for now
            ThingDef stallDef = _DefOf.Carn_StallFood;
            IntVec3 stallSpot = IntVec3.Invalid;
            Rot4 rot = default(Rot4);
            CellRect stallArea = info.carnivalArea;

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
                    stallSpot = FindRandomPlacementFor(stallDef, rot, stallArea, stallSpot);
                }
                else
                {
                    // Find random initial spot
                    stallSpot = FindRandomPlacementFor(stallDef, rot, false, 8);
                    stallArea = CellRect.CenteredOn(stallSpot, 8);
                }


                // Finally, spawn the fucker
                if (stallSpot.IsValid)
                {
                    RemovePlantsAndTeleportHaulablesFor(stallSpot, stallDef.size, rot);
                    // Add spot to stall user's spot
                    info.rememberedPositions.Add(stallUser, stallSpot);
                    yield return GenConstruct.PlaceBlueprintForBuild(stallDef, stallSpot, info.map, rot, info.currentLord.faction, null);
                }
            }
        }

        private static Blueprint PlaceEntranceBlueprint()
        {
            ThingDef bannerDef = _DefOf.Carn_SignEntry;
            Rot4 rot = default(Rot4);

            if (CanPlaceBlueprintAt(info.bannerCell, rot, bannerDef))
            {
                RemovePlantsAndTeleportHaulablesFor(info.bannerCell, bannerDef.size, rot);
                return GenConstruct.PlaceBlueprintForBuild(bannerDef, info.bannerCell, info.map, rot, info.currentLord.faction, null);
            }

            // If cannot place on bannerCell, try in a small area around it

            CellRect tryArea = CellRect.CenteredOn(info.bannerCell, 8).ClipInsideRect(info.carnivalArea);
            IntVec3 bannerSpot = FindRandomPlacementFor(bannerDef, rot, tryArea);

            if (bannerSpot.IsValid)
            {
                info.bannerCell = bannerSpot;
                RemovePlantsAndTeleportHaulablesFor(bannerSpot, bannerDef.size, rot);
                return GenConstruct.PlaceBlueprintForBuild(bannerDef, bannerSpot, info.map, rot, info.currentLord.faction, null);
            }

            // If that fails, try any area in the carnival area (suboptimal)

            bannerSpot = FindRandomPlacementFor(bannerDef, rot);
            info.bannerCell = bannerSpot;
            RemovePlantsAndTeleportHaulablesFor(bannerSpot, bannerDef.size, rot);
            return GenConstruct.PlaceBlueprintForBuild(bannerDef, bannerSpot, info.map, rot, info.currentLord.faction, null);
        }

        private static Blueprint PlaceJoyBuildings()
        {
            return null;
        }




        private static bool CanPlaceBlueprintAt(IntVec3 spot, Rot4 rot, ThingDef def)
        {
            if (!spot.IsValid) return false;

            // Cheaty cheaty
            bool isEdifice = def.IsEdifice();

            def.building.isEdifice = true;

            bool result = GenConstruct.CanPlaceBlueprintAt(def, spot, rot, info.map, false, null).Accepted;

            def.building.isEdifice = isEdifice;

            return result;
        }


        private static IntVec3 FindRandomPlacementFor(ThingDef def, Rot4 rot, bool preferFarFromColony = false, int contractedBy = 0)
        {
            CellRect noGo = CellRect.CenteredOn(info.bannerCell, info.carnivalArea.Width / 2);

            CellRect adjustedArea = info.carnivalArea.ContractedBy(contractedBy);

            for (int i = 0; i < 200; i++)
            {
                IntVec3 randomCell = adjustedArea.RandomCell;

                if (preferFarFromColony && noGo.Contains(randomCell))
                    continue;

                if (info.map.reachability.CanReach(randomCell, info.carnivalArea.CenterCell, Verse.AI.PathEndMode.OnCell, TraverseMode.NoPassClosedDoors, Danger.Deadly))
                {
                    if (!randomCell.Roofed(info.map))
                    {
                        if (CanPlaceBlueprintAt(randomCell, rot, def))
                        {
                            return randomCell;
                        }
                    }
                }
            }
            return IntVec3.Invalid;
        }

        private static IntVec3 FindRandomPlacementFor(ThingDef def, Rot4 rot, CellRect otherArea, IntVec3 preferCardinalAdjacentTo = default(IntVec3))
        {
            for (int i = 0; i < 200; i++)
            {
                IntVec3 randomCell = otherArea.RandomCell;

                if (info.map.reachability.CanReach(randomCell, info.carnivalArea.CenterCell, Verse.AI.PathEndMode.OnCell, TraverseMode.NoPassClosedDoors, Danger.Deadly))
                {
                    if (!randomCell.Roofed(info.map))
                    {
                        if (CanPlaceBlueprintAt(randomCell, rot, def))
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



        private static void RemovePlantsAndTeleportHaulablesFor(IntVec3 spot, IntVec2 size, Rot4 rot)
        {
            CellRect removeCells = GenAdj.OccupiedRect(spot, rot, size);
            // Not sure if CellRects are immutable upon assignment but w/e
            CellRect moveToCells = new CellRect(removeCells.minX, removeCells.minZ, removeCells.maxX, removeCells.maxZ).ExpandedBy(2).ClipInsideMap(info.map);

            foreach (IntVec3 cell in removeCells)
            {
                Plant plant = cell.GetPlant(info.map);
                if (plant != null)
                {
                    plant.Destroy(DestroyMode.KillFinalize);
                }

                Thing haulable = cell.GetFirstHaulable(info.map);
                if (haulable != null)
                {
                    IntVec3 moveToSpot = haulable.Position;
                    for (int i = 0; i < 30; i++)
                    {
                        IntVec3 tempSpot = moveToCells.RandomCell;
                        if (!removeCells.Contains(tempSpot))
                        {
                            if (!tempSpot.GetThingList(info.map).Any())
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
