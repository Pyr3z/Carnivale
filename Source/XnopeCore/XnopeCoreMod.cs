using Harmony;
using HugsLib;
using System.Reflection;
using Verse;

namespace Xnope
{
    [StaticConstructorOnStartup]
    public class XnopeCoreMod : ModBase
    {
        public static readonly string id = "com.github.xnope.core";

        public static readonly bool debugLog = true;

        public override string ModIdentifier { get { return "XnopeCore"; } }

        static XnopeCoreMod()
        {
            // Harmony patching handled by HugsLib

        }
    }
}
