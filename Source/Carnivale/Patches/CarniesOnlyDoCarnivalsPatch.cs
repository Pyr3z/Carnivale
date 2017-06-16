using Harmony;
using RimWorld;
using Verse;

namespace Carnivale.Patches
{
    [HarmonyPatch(typeof(IncidentWorker_NeutralGroup), "FactionCanBeGroupSource")]
    public static class CarniesOnlyDoCarnivalsPatch
    {

        public static bool Prefix(ref bool __result, Faction f)
        {
            if (f.IsCarnival())
            {
                __result = false;
                return false;
            }

            return true;
        }

    }
}
