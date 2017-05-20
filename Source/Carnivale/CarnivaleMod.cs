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
            // Init harmony and patch.
            var harmony = HarmonyInstance.Create(id);
            
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // Manual patching:
            /*
            harmony.Patch(
                typeof(FactionGenerator).GetMethod("GenerateFactionsIntoWorld"),
                null,
                new HarmonyMethod(typeof(Postfix_GenerateFactionsIntoWorld).GetMethod("UnhideCarnivaleFactions"))
            );
            */
        }

    }

}