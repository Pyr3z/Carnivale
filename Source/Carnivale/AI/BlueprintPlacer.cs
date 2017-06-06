using RimWorld;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;

namespace Carnivale.AI
{
    public static class BlueprintPlacer
    {
        private static IntVec3 centre;

        private static int radius;

        private static Faction faction;

        private static List<Thing> availableCrates;

        // The only public method; use this
        [DebuggerHidden]
        public static IEnumerable<Blueprint> PlaceCarnivalBlueprints(IntVec3 centre, int radius, Map map, Faction faction, List<Thing> availableCrates)
        {
            BlueprintPlacer.centre = centre;
            BlueprintPlacer.radius = radius;
            BlueprintPlacer.faction = faction;
            BlueprintPlacer.availableCrates = availableCrates;

            foreach (Blueprint_Tent tent in PlaceTentBlueprints(map))
            {
                yield return tent;
            }

            //foreach (Blueprint_Build stall in PlaceStallBlueprints(map))
            //{
            //    yield return stall;
            //}

            //yield return PlaceEntranceBlueprint(map);
        }


        private static IEnumerable<Blueprint_Tent> PlaceTentBlueprints(Map map)
        {
            int numTents = 0;
            foreach (Thing crate in availableCrates)
            {
                if (crate.def == _DefOf.Carn_Crate_TentLodge)
                    numTents++;
            }

            ThingDef tentDef = _DefOf.Carn_TentMedBed;
            Rot4 rot = Rot4.Random;
            IntVec3 tentSpot = FindPlacementFor(tentDef, rot, map);

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
                    RemovePlantsFor(tentSpot, (tentDef.size.x - 1) / 2, map);
                    yield return (Blueprint_Tent)GenConstruct.PlaceBlueprintForBuild(tentDef, tentSpot, map, rot, faction, null);
                }
                else
                {
                    // Find new placement
                    tentSpot = FindPlacementFor(tentDef, rot, map);
                    i--;
                }
            }

            // Place manager tent
            if (!availableCrates.Any(c => c.def == _DefOf.Carn_Crate_TentMan))
                yield break;

            rot = Rot4.Random;
            tentDef = _DefOf.Carn_TentSmallMan;
            tentSpot = FindPlacementFor(tentDef, rot, map);

            if (tentSpot.IsValid)
            {
                // Insta-cut plants (potentially OP?)
                RemovePlantsFor(tentSpot, ((tentDef.size.x - 1) / 2) + 1, map);
                yield return (Blueprint_Tent)GenConstruct.PlaceBlueprintForBuild(tentDef, tentSpot, map, rot, faction, null);
            }

        }

        private static IEnumerable<Blueprint_Build> PlaceStallBlueprints(Map map)
        {

            yield break;
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


        private static IntVec3 FindPlacementFor(ThingDef def, Rot4 rot, Map map)
        {
            CellRect cellRect = CellRect.CenteredOn(centre, radius);
            cellRect.ClipInsideMap(map);
            for (int i = 0; i < 200; i++)
            {
                IntVec3 randomCell = cellRect.RandomCell;
                if (map.reachability.CanReach(randomCell, centre, Verse.AI.PathEndMode.OnCell, TraverseMode.NoPassClosedDoors, Danger.Deadly))
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


        private static void RemovePlantsFor(IntVec3 spot, int radius, Map map)
        {
            CellRect cutCells = CellRect.CenteredOn(spot, radius);
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
