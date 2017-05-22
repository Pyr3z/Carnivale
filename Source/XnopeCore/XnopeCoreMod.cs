using Harmony;
using HugsLib;
using RimWorld;
using System.Reflection;
using Verse;
using Xnope.Patches;

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

            //// Manual patching
            //harmony.Patch(
            //    typeof(Faction).GetMethod("GenerateNewLeader"),
            //    new HarmonyMethod(
            //        typeof(Prefix_GenerateNewLeader).GetMethod("Prefix")
            //    ),
            //    new HarmonyMethod(
            //        typeof(Postfix_GenerateNewLeader).GetMethod("Postfix")
            //    ),
            //    null
            //);

        }
    }
}
