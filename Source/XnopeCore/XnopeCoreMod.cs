using Harmony;
using HugsLib;
using System.Reflection;
using Verse;

namespace Xnope
{
    [StaticConstructorOnStartup]
    public class XnopeCoreMod : ModBase
    {
        public static readonly bool debugLog = true;

        public override string ModIdentifier { get { return "XnopeCore"; } }

        protected override bool HarmonyAutoPatch { get { return false; } }

        static XnopeCoreMod()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("com.github.xnope.core");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
