using HugsLib;
using Verse;

namespace Carnivale
{
    [StaticConstructorOnStartup]
    public class CarnivaleMod : ModBase
    {
        public override string ModIdentifier { get { return "Carnivale"; } }

        public static readonly bool debugLog = true;

        static CarnivaleMod()
        {
            // Harmony patching handled by HugsLib

            
        }

    }

}