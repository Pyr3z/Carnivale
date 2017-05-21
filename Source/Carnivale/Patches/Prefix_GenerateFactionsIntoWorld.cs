using Harmony;
using RimWorld;
using Verse;
using System.Linq;

namespace Carnivale.Patches
{
    // FactionGenerator.GenerateFactionsIntoWorld() pre patch:
    // hides carnival factions
    [HarmonyPatch(typeof(FactionGenerator), "GenerateFactionsIntoWorld")]
    public static class Prefix_GenerateFactionsIntoWorld
    {
        [HarmonyPrefix]
        public static void HideCarnivalFactions()
        {
            foreach (FactionDef def in (from d in DefDatabase<FactionDef>.AllDefs
                                        where d.defName.StartsWith("Carn_")
                                        select d))
            {
                def.hidden = true;
            }
        }
    }
}