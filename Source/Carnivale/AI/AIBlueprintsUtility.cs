using RimWorld;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Verse;
using Xnope;

namespace Carnivale
{
    public static class AIBlueprintsUtility
    {
        private static CarnivalInfo info;

        private static List<Thing> availableCrates;

        private static List<Pawn> stallUsers;

        private static List<IntVec3> cachedPos = new List<IntVec3>();

        // The only public method; use this
        [DebuggerHidden]
        public static IEnumerable<Blueprint> PlaceCarnivalBlueprints(CarnivalInfo info)
        {
            // Assign necessary values to this singleton (is this technically a singleton, or just static?)
            AIBlueprintsUtility.info = info;
            
            if (info.currentLord.CurLordToil.data is LordToilData_SetupCarnival)
            {
                availableCrates = ((LordToilData_SetupCarnival)info.currentLord.CurLordToil.data).availableCrates;
                stallUsers = ((List<Pawn>)info.pawnsWithRole[CarnivalRole.Vendor]).ListFullCopyOrNull();
            }
            else
            {
                Log.Error("Tried to place carnival blueprints while not in setup toil.");
                yield break;
            }

            cachedPos.Clear();



            // Do the blueprint thing

            if (!availableCrates.NullOrEmpty())
            {
                foreach (var tent in PlaceTentBlueprints())
                {
                    cachedPos.Add(tent.Position);
                    yield return tent;
                }

                if (!stallUsers.NullOrEmpty())
                {
                    foreach (var stall in PlaceStallBlueprints())
                    {
                        cachedPos.Add(stall.Position);
                        yield return stall;
                    }
                }

                var entrance = PlaceEntranceBlueprint();

                if (entrance != null)
                {
                    cachedPos.Add(entrance.Position);
                    yield return entrance;
                }

                foreach (var game in PlaceGameBlueprints())
                {
                    cachedPos.Add(game.Position);
                    yield return game;
                }

                var trashSign = PlaceTrashBlueprint();

                if (trashSign != null)
                {
                    yield return trashSign;
                }
            }

            cachedPos.Clear();
        }


        private static IEnumerable<Blueprint> PlaceTentBlueprints()
        {
            // main chapiteau
            ThingDef tentDef;
            Rot4 rot = info.setupCentre.RotationFacing(info.bannerCell);
            IntVec3 tentSpot;

            if (availableCrates.Any(c => c.def == _DefOf.Carn_Crate_TentHuge))
            {
                tentDef = _DefOf.Carn_TentChap;
                tentSpot = FindRadialPlacementFor(tentDef, rot, info.setupCentre, 11);
                if (tentSpot.IsValid)
                {
                    RemoveFirstCrateOf(_DefOf.Carn_Crate_TentHuge);
                    Utilities.ClearThingsFor(info.map, tentSpot, tentDef.size, rot, false, true);
                    yield return PlaceBlueprint(tentDef, tentSpot, rot);
                }
                else
                {
                    Log.Error("Could not find placement for " + tentDef + ", which is a major attraction. Tell Xnope to get it together.");
                }
            }



            // lodging tents
            tentDef = _DefOf.Carn_TentLodge;
            rot = Rot4.Random;
            tentSpot = FindRadialPlacementFor(tentDef, rot, info.carnivalArea.ContractedBy(9).FurthestCellFrom(info.bannerCell), 7);

            IntVec3 lineDirection = rot.ToIntVec3(1); // shifted clockwise by 1

            int numFailures = 0;
            bool firstNewPass = true;
            while (numFailures < 30 && availableCrates.Any(t => t.def == _DefOf.Carn_Crate_TentLodge))
            {
                // Following works as intended iff size.x == size.y

                if (!firstNewPass)
                {
                    // try adding tent next in a an adjacent line
                    tentSpot += lineDirection * ((tentDef.size.x + 1));
                }

                if (CanPlaceBlueprintAt(tentSpot, tentDef, rot))
                {
                    // bingo
                    firstNewPass = false;
                    RemoveFirstCrateOf(_DefOf.Carn_Crate_TentLodge);
                    Utilities.ClearThingsFor(info.map, tentSpot, tentDef.size, rot, false, true);
                    yield return PlaceBlueprint(tentDef, tentSpot, rot);
                }
                else if (numFailures % 3 != 0)
                {
                    // try different line directions
                    rot.AsByte++;
                    lineDirection = rot.ToIntVec3(1);
                    numFailures++;
                }
                else
                {
                    // Find new placement if next spot and any of its rotations don't work
                    tentSpot = FindRadialPlacementFor(tentDef, rot, tentSpot, (int)info.baseRadius / 2);

                    if (!tentSpot.IsValid)
                    {
                        // suboptimal random placement
                        tentSpot = FindRandomPlacementFor(tentDef, rot, false, 5);
                    }

                    firstNewPass = true;
                }
            }

            if (numFailures == 30 && availableCrates.Any(t => t.def == _DefOf.Carn_Crate_TentLodge))
            {
                Log.Error("Tried too many times to place tents. Some may not be built.");
            }

            // manager tent
            if (!availableCrates.Any(c => c.def == _DefOf.Carn_Crate_TentMan))
                yield break;

            rot = Rot4.Random;
            tentDef = _DefOf.Carn_TentLodgeMan;

            // Try to place near other tents
            tentSpot = FindRadialCardinalPlacementFor(tentDef, rot, tentSpot, 10);

            if (tentSpot.IsValid)
            {
                RemoveFirstCrateOf(_DefOf.Carn_Crate_TentMan);
                Utilities.ClearThingsFor(info.map, tentSpot, tentDef.size, rot, false, true);
                yield return PlaceBlueprint(tentDef, tentSpot, rot);
            }
            else
            {
                // suboptimal placement
                tentSpot = FindRadialPlacementFor(tentDef, rot, info.setupCentre, (int)info.baseRadius / 2);
                if (tentSpot.IsValid)
                {
                    RemoveFirstCrateOf(_DefOf.Carn_Crate_TentMan);
                    Utilities.ClearThingsFor(info.map, tentSpot, tentDef.size, rot, false, true);
                    yield return PlaceBlueprint(tentDef, tentSpot, rot);
                }
                else
                {
                    Log.Error("Found no valid placement for manager tent. It will not be placed.");
                }
            }

        }

        private static IEnumerable<Blueprint> PlaceStallBlueprints()
        {
            // Default stall is food for now
            ThingDef stallDef = _DefOf.Carn_StallFood;
            IntVec3 stallSpot = IntVec3.Invalid;
            Rot4 rot = default(Rot4);
            CellRect stallArea = info.carnivalArea;

            foreach (Pawn stallUser in stallUsers.Where(p => p.TraderKind != null))
            {
                // Handle different kinds of vendor stalls

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



                if (stallSpot.IsValid)
                {
                    // Next spot should be close to last spot
                    stallSpot = FindRadialCardinalPlacementFor(stallDef, rot, stallSpot, 10);
                }
                else
                {
                    // Find random initial spot
                    //stallSpot = FindRandomPlacementFor(stallDef, rot, false, (int)info.baseRadius / 3);
                    
                    // Trying new approach:

                    IntVec3[] points = new IntVec3[]
                    {
                        info.setupCentre,
                        info.bannerCell
                    };

                    stallSpot = FindRadialPlacementFor(stallDef, rot, points.Average(), 10);
                }


                // Finally, spawn the f*cker
                if (stallSpot.IsValid)
                {
                    RemoveFirstCrateOf(stallDef);
                    Utilities.ClearThingsFor(info.map, stallSpot, stallDef.size, rot, false, true);
                    // Add spot to stall user's spot
                    info.rememberedPositions.Add(stallUser, stallSpot);
                    yield return PlaceBlueprint(stallDef, stallSpot, rot);
                }
            }
        }

        private static Blueprint PlaceEntranceBlueprint()
        {
            if (!availableCrates.Any(c => c.def == _DefOf.Carn_Crate_Stall))
                return null;

            ThingDef bannerDef = _DefOf.Carn_SignEntry;
            Rot4 rot = default(Rot4);

            IntVec3 bannerSpot = FindRadialPlacementFor(bannerDef, rot, info.bannerCell, 16);

            if (bannerSpot.IsValid)
            {
                info.bannerCell = bannerSpot;

                if (Prefs.DevMode)
                    Log.Warning("[Debug] bannerCell final pass: " + info.bannerCell.ToString());

                RemoveFirstCrateOf(_DefOf.Carn_Crate_Stall);
                Utilities.ClearThingsFor(info.map, info.bannerCell, bannerDef.size, rot, false, true);
                return PlaceBlueprint(bannerDef, bannerSpot, rot);
            }

            // If that fails, try any spot in the carnival area (suboptimal)

            Log.Error("Couldn't find an optimum place for " + bannerDef + ". Trying random place in carnival area.");
            bannerSpot = FindRandomPlacementFor(bannerDef, rot);
            
            if (bannerSpot.IsValid)
            {
                info.bannerCell = bannerSpot;

                if (Prefs.DevMode)
                    Log.Warning("[Debug] bannerCell final pass: " + info.bannerCell.ToString());

                RemoveFirstCrateOf(_DefOf.Carn_Crate_Stall);
                Utilities.ClearThingsFor(info.map, bannerSpot, bannerDef.size, rot, false, true);
                return PlaceBlueprint(bannerDef, bannerSpot, rot);
            }

            Log.Error("Couldn't find any place for " + bannerDef + ". Not retrying.");
            return null;
        }

        private static IEnumerable<Blueprint> PlaceGameBlueprints()
        {
            var gameMasters = stallUsers.Where(p => p.TraderKind == null).ToList();
            // ListFullCopy() inefficient... but we have to copy because it's not thread-safe otherwise
            var gameCrates = availableCrates.ListFullCopy().Where(c => c.def.entityDefToBuild != null);

            IntVec3[] points = new IntVec3[]
            {
                info.setupCentre,
                info.setupCentre,
                info.bannerCell
            };
            var gameSpot = points.Average();

            ThingDef gameDef;
            int i = 0;
            foreach (var crate in gameCrates)
            {
                gameDef = (ThingDef)crate.def.entityDefToBuild;

                gameSpot = FindRadialPlacementFor(gameDef, default(Rot4), gameSpot, 10);

                if (gameSpot.IsValid)
                {
                    RemoveFirstCrateOf(crate.def);
                    Utilities.ClearThingsFor(info.map, gameSpot, gameDef.size, default(Rot4), false, true);

                    if (i < gameMasters.Count)
                    {
                        IntVec3 gameMasterSpot = gameSpot + gameDef.interactionCellOffset + new IntVec3(-1, 0, 1);
                        info.rememberedPositions.Add(gameMasters[i++], gameMasterSpot);
                    }

                    yield return PlaceBlueprint(gameDef, gameSpot);
                }
                else
                {
                    Log.Error("Found no place for " + gameDef + ". It will not be built.");
                }
            }
        }

        private static Blueprint PlaceTrashBlueprint()
        {
            ThingDef signDef = _DefOf.Carn_SignTrash;
            IntVec3 trashPos = info.carnivalArea.ContractedBy(5).FurthestCellFrom(cachedPos.Average(), true, delegate (IntVec3 c) 
            {
                if (GenRadial.RadialCellsAround(info.bannerCell, 7, false).Contains(c))
                    return false;

                return true;
            });

            if (!CanPlaceBlueprintAt(trashPos, signDef))
            {
                trashPos = FindRadialPlacementFor(signDef, default(Rot4), trashPos, 10);
            }

            if (!trashPos.IsValid)
            {
                Log.Error("Could not find any place for a trash spot. Trash will not be hauled.");
                info.TrashCentre = IntVec3.Invalid;
                return null;
            }

            info.TrashCentre = trashPos;

            RemoveFirstCrateOf(ThingDefOf.WoodLog);
            Utilities.ClearThingsFor(info.map, trashPos, new IntVec2(4,4), default(Rot4));
            return PlaceBlueprint(signDef, trashPos, default(Rot4), ThingDefOf.WoodLog);
        }




        private static bool CanPlaceBlueprintAt(IntVec3 spot, ThingDef def, Rot4 rot = default(Rot4))
        {
            if (!spot.IsValid) return false;

            // Cheaty cheaty
            bool isEdifice = def.IsEdifice();

            def.building.isEdifice = true;

            bool result = GenConstruct.CanPlaceBlueprintAt(def, spot, rot, info.map, false, null).Accepted;

            def.building.isEdifice = isEdifice;

            return result;
        }


        private static Blueprint PlaceBlueprint(ThingDef def, IntVec3 spot, Rot4 rotation = default(Rot4), ThingDef stuff = null)
        {
            return GenConstruct.PlaceBlueprintForBuild(def, spot, info.map, rotation, info.currentLord.faction, stuff);
        }


        private static IntVec3 FindRandomPlacementFor(ThingDef def, Rot4 rot = default(Rot4), bool preferFarFromColony = false, int contractedBy = 0)
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
                    if (CanPlaceBlueprintAt(randomCell, def, rot))
                    {
                        return randomCell;
                    }
                }
            }
            return IntVec3.Invalid;
        }

        private static IntVec3 FindRandomPlacementFor(ThingDef def, Rot4 rot, CellRect searchArea)
        {
            for (int i = 0; i < 200; i++)
            {
                IntVec3 randomCell = searchArea.RandomCell;

                if (info.map.reachability.CanReach(randomCell, info.carnivalArea.CenterCell, Verse.AI.PathEndMode.OnCell, TraverseMode.NoPassClosedDoors, Danger.Deadly))
                {
                    if (CanPlaceBlueprintAt(randomCell, def, rot))
                    {
                        return randomCell;
                    }
                }
            }

            return IntVec3.Invalid;
        }

        private static IntVec3 FindRadialCardinalPlacementFor(ThingDef def, Rot4 rot, IntVec3 centre, int radius)
        {
            byte rotb = (byte)Rand.Range(0, 4);
            
            for (int i = 0; i < 4; i++)
            {
                rotb++;
                var endpoint = centre + (rotb.ToIntVec3() * radius);

                if (!endpoint.InBounds(info.map)) continue;

                foreach (var cell in centre.CellsInLineTo(endpoint))
                {
                    // Try directly on line
                    if (info.map.reachability.CanReach(cell, info.carnivalArea.CenterCell, Verse.AI.PathEndMode.OnCell, TraverseMode.NoPassClosedDoors, Danger.Deadly))
                    {
                        if (CanPlaceBlueprintAt(cell, def, rot))
                        {
                            return cell;
                        }
                    }
                }
            }

            return IntVec3.Invalid;
        }

        private static IntVec3 FindRadialPlacementFor(ThingDef def, Rot4 rot, IntVec3 centre, int radius)
        {
            foreach (var cell in GenRadial.RadialCellsAround(centre, radius, true))
            {
                if (info.map.reachability.CanReach(cell, info.carnivalArea.CenterCell, Verse.AI.PathEndMode.OnCell, TraverseMode.NoPassClosedDoors, Danger.Deadly))
                {
                    if (CanPlaceBlueprintAt(cell, def, rot))
                    {
                        return cell;
                    }
                }
            }

            return IntVec3.Invalid;
        }


        private static void RemoveFirstCrateOf(ThingDef def)
        {
            Thing first = availableCrates.FirstOrDefault(t => t.def == def);
            if (first != null)
            {
                availableCrates.Remove(first);
            }
        }

        
    }
}
