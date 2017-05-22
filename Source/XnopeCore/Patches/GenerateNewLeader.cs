using Harmony;
using RimWorld;
using System.Linq;
using Verse;

namespace Xnope.Patches
{
    // Prefix patch:
    // stores the old (dead) leader's name for use in postfix
    [HarmonyPatch(typeof(Faction), "GenerateNewLeader")]
    public static class Prefix_GenerateNewLeader
    {
        // use static var until __state is figured out
        public static string oldLeaderName = "";

        [HarmonyPrefix]
        public static void Prefix(Faction __instance)
        {
            if (__instance.IsDynamicallyNamed())
            {
                // Store old leader's name in static variable
                if (__instance.leader != null)
                    oldLeaderName = __instance.leader.NameStringShort;
                else
                    oldLeaderName = "LNAME";

            }
        }
    }



    // Postfix patch:
    // dynamically adjusts a faction's name with its leader's name
    [HarmonyPatch(typeof(Faction), "GenerateNewLeader")]
    public static class Postfix_GenerateNewLeader
    {
        [HarmonyPostfix]
        public static void Postfix(Faction __instance)
        { 
            if (__instance.IsDynamicallyNamed())
            {
                // Resolve name with new leader name
                ResolveFactionName(__instance, Prefix_GenerateNewLeader.oldLeaderName);

            }

        }


        private static void RegenerateFactionName(Faction fac)
        {
            fac.Name = NameGenerator.GenerateName(
                fac.def.factionNameMaker,
                from f in Find.FactionManager.AllFactions select f.Name,
                false
            );
        }

        private static void ResolveFactionName(Faction f, string oldLeaderName)
        {
            f.Name = f.Name.Replace(oldLeaderName, f.leader.NameStringShort);
        }
    }
}
