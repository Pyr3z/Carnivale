using Harmony;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Carnivale
{
    [HarmonyPatch(typeof(CostListCalculator), "CostListAdjusted", new[] { typeof(BuildableDef), typeof(ThingDef), typeof(bool) })]
    public static class CostListCalculator_CostListAdjusted
    {

        public static bool Prefix(BuildableDef entDef, ref List<ThingCountClass> __result)
        {
            if (!entDef.costList.NullOrEmpty() && entDef.costList[0].thingDef.defName.StartsWith("Carn_"))
            {
                __result = entDef.costList;

                return false;
            }

            return true;
        }

    }
}
