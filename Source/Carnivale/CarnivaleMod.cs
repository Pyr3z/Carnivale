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

        static CarnivaleMod()
        {
            var harmony = HarmonyInstance.Create(id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

    }

}