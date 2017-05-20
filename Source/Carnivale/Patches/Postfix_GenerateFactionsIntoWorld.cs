using Harmony;
using RimWorld;
using Verse;
using System.Linq;

namespace Carnivale.Patches
{
    // FactionGenerator.GenerateFactionsIntoWorld() postfix patch:
    // unhides carnival factions
    [HarmonyPatch(typeof(FactionGenerator), "GenerateFactionsIntoWorld")]
    public static class Postfix_GenerateFactionsIntoWorld
    {
        [HarmonyPostfix]
        public static void UnhideCarnivalFactions()
        {
            foreach (FactionDef def in (from d in DefDatabase<FactionDef>.AllDefs
                                        where d.defName.StartsWith("Carn_")
                                        select d))
            {
                def.hidden = false;
            }
        }
    }
}
