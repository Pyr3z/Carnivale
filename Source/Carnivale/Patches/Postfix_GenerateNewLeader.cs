using Harmony;
using RimWorld;

namespace Carnivale.Patches
{
    [HarmonyPatch(typeof(Faction), "GenerateNewLeader")]
    public static class Postfix_GenerateNewLeader
    {
        [HarmonyPostfix]
        public static void ResolveFactionName(Faction __instance)
        {
            if (__instance.IsCarnival())
            {
                string oldName = __instance.Name;
                __instance.Name = oldName.Replace("LNAME", __instance.leader.NameStringShort);
            }

        }

        
    }
}
