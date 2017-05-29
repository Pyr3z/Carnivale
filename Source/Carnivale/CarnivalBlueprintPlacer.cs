using Carnivale.AI;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace Carnivale
{
    public static class CarnivalBlueprintPlacer
    {
        private static IntVec3 Centre;

        private static Map Map;

        private static Faction Faction;

        private static List<Thing> UnbuiltThings;



        // The only public method:
        public static IEnumerable<Blueprint> PlaceBlueprints(LordToil_SetupCarnival toil)
        {
            Centre = toil.Data.setupSpot;
            Map = toil.Map;
            Faction = toil.lord.faction;
            UnbuiltThings = toil.Data.unbuiltThings;

            foreach (var bp in PlaceTents())
                yield return bp;

            foreach (var bp in PlaceStalls())
                yield return bp;

            yield return PlaceEntrance();
        }



        private static bool CanPlaceBlueprintAt(Rot4 rot, ThingDef buildingDef)
        {
            return GenConstruct.CanPlaceBlueprintAt(buildingDef, Centre, rot, Map, false, null).Accepted;
        }



        private static IEnumerable<Blueprint> PlaceTents()
        {
            throw new NotImplementedException();
        }


        private static IEnumerable<Blueprint> PlaceStalls()
        {
            throw new NotImplementedException();
        }


        private static Blueprint PlaceEntrance()
        {
            throw new NotImplementedException();
        }

    }
}
