using Harmony;
using RimWorld;
using Verse;

namespace Carnivale.Patches
{
    [HarmonyPatch(typeof(PawnComponentsUtility), "AddAndRemoveDynamicComponents")]
    public static class CarnyDrugPolicyPatch
    {

        public static void Postfix(ref Pawn pawn)
        {
            if (pawn.IsCarny())
            {
                if (pawn.drugs == null)
                {
                    pawn.drugs = new Pawn_DrugPolicyTracker(pawn);
                }
            }
        }

    }
}
