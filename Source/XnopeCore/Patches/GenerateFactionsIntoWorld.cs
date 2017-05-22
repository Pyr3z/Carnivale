using Harmony;
using RimWorld;
using System.Linq;
using Verse;

namespace Xnope.Patches
{
    // Prefix patch:
    // hides roaming factions
    [HarmonyPatch(typeof(FactionGenerator), "GenerateFactionsIntoWorld")]
    public static class Prefix_GenerateFactionsIntoWorld
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            HideRoamingFactions();

        }


        private static void HideRoamingFactions()
        {
            foreach (FactionDef def in (from d in DefDatabase<FactionDef>.AllDefs
                                        where d.IsRoaming()
                                        select d))
            {
                def.hidden = true;
            }
        }
    }



    // Postfix patch:
    // unhides roaming factions
    [HarmonyPatch(typeof(FactionGenerator), "GenerateFactionsIntoWorld")]
    public static class Postfix_GenerateFactionsIntoWorld
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            UnhideRoamingFactions();

        }


        private static void UnhideRoamingFactions()
        {
            foreach (FactionDef def in (from d in DefDatabase<FactionDef>.AllDefs
                                        where d.IsRoaming()
                                        select d))
            {
                def.hidden = false;
            }
        }
    }
}
