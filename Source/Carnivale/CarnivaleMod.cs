using HugsLib;
using Verse;

namespace Carnivale
{
    [StaticConstructorOnStartup]
    public class CarnivaleMod : ModBase
    {
        public static readonly bool debugLog = true;

        public override string ModIdentifier { get { return "Carnivale"; } }

        protected override bool HarmonyAutoPatch { get { return false; } }


        static CarnivaleMod()
        {
            // Harmony patching handled by HugsLib

            
        }

    }

}