using Harmony;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace Carnivale.Patches
{
    // Faction.GenerateNewLeader() prefix patch:
    // generates a new leader from existing pawns
    //[HarmonyPatch(typeof(Faction), "GenerateNewLeader")]
    public static class Prefix_GenerateNewLeader
    {
        //[HarmonyPrefix]
        public static bool GenerateNewLeaderFromExistingPawns(Faction __instance)
        {
            if (__instance.def.defName.StartsWith("Carn_Faction"))
            {
                // TODO code
                List<Pawn> candidates = new List<Pawn>();
                int totalAge = 0;

                foreach (Pawn p in PawnsFinder.AllMaps_SpawnedPawnsInFaction(__instance))
                {
                    if (p.health.State == PawnHealthState.Mobile)
                    {
                        totalAge += p.ageTracker.AgeBiologicalYears;
                        candidates.Add(p);
                    }
                }

                if (totalAge == 0)
                {
                    // Nobody up and alive can lead them.
                    // Consider defeating faction here.
                    return true;
                }

                int averageAge = totalAge / candidates.Count;

                foreach (Pawn p in candidates)
                {
                    // Simply pick the first above-average age pawn for now.
                    if (p.ageTracker.AgeBiologicalYears >= averageAge)
                    {
                        __instance.leader = p;

                        if (!Find.WorldPawns.Contains(p))
                        {
                            // Wouldn't be necessary if all carny pawns were set to this.
                            Find.WorldPawns.PassToWorld(p, PawnDiscardDecideMode.KeepForever);
                        }

                        p.ChangeKind(DefDatabase<PawnKindDef>.GetNamed("Carn_PawnKind_Manager"));

                        break;
                    }
                }

                return false;
            }
            else
                return true;
        }
    }
}
