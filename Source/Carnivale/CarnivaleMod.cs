using Harmony;
using HugsLib;
using Verse;
using System.Reflection;

namespace Carnivale
{
    [StaticConstructorOnStartup]
    public class CarnivaleMod : ModBase
    {
        public static readonly string id = "com.github.xnope.carnivale";

        public override string ModIdentifier { get { return "Carnivale"; } }

        public static readonly bool debugLog = true;

        static CarnivaleMod()
        {
            // Harmony patching handled by HugsLib

            
        }

    }

}