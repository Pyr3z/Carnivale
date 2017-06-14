using Harmony;
using RimWorld;
using Verse;

namespace Carnivale.Patches
{
    /// <summary>
    /// Carnies get joy
    /// </summary>
    [HarmonyPatch(typeof(Pawn_NeedsTracker), "ShouldHaveNeed")]
    public static class CarnyJoyNeedPatch
    {

        public static bool Prefix(Pawn_NeedsTracker __instance, ref bool __result, NeedDef nd)
        {
            var pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();

            if ((nd == NeedDefOf.Joy) && pawn.IsCarny())
            {
                __result = true;
                return false;
            }
            return true;
        }

    }
}
