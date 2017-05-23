using Harmony;
using RimWorld;
using System.Linq;
using Verse;

namespace Xnope.Patches
{
    [HarmonyPatch(typeof(FactionGenerator), "GenerateFactionsIntoWorld")]
    public static class Patch_FactionGenerator_GenerateFactionsIntoWorld
    {
        // Prefix patch:
        // hides roaming factions
        public static void Prefix()
        {
            HideRoamingFactions();

        }

        // Postfix patch:
        // unhides roaming factions
        public static void Postfix()
        {
            UnhideRoamingFactions();

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
