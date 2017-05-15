using Harmony;
using HugsLib;
using RimWorld;
using System.Reflection;
using Verse;

namespace Carnivale
{
    [StaticConstructorOnStartup]
    public class CarnivaleMod : ModBase
    {
        private static readonly string id = "com.github.xnope.carnivale";

        public override string ModIdentifier { get { return id; } }

        static CarnivaleMod()
        {
            var harmony = HarmonyInstance.Create(id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

    }


    // Harmony patches:

    // FactionGenerator.NewGeneratedFaction(FactionDef) prefix patch:
    // temoporarily hides carnival factions so bases do not generate
    /*
    [HarmonyPatch(typeof(FactionGenerator), "NewGeneratedFaction", new[] { typeof(FactionDef) })]
    public static class Prefix_NewGeneratedFaction
    {
        [HarmonyPrefix]
        public static void TemporarilyHideFaction(ref FactionDef def)
        {
            if (def.defName.StartsWith("Carn_Faction"))
            {
                // hide the faction so bases do not generate
                def.hidden = true;
                // unhide in postfix
            }
        }
    }
    */


    // FactionGenerator.NewGeneratedFaction(FactionDef) postfix patch:
    // unhides carnival factions
    /*
    [HarmonyPatch(typeof(FactionGenerator), "NewGeneratedFaction", new[] { typeof(FactionDef) })]
    public static class Postfix_NewGeneratedFaction
    {
        [HarmonyPostfix]
        public static void UnhideFaction(Faction __result)
        {
            if (__result.def.defName.StartsWith("Carn_Faction"))
            {
                // unhide the faction
                __result.def.hidden = false;
            }
        }
    }
    */

}