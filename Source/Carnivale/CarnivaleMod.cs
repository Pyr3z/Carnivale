using HugsLib;
using RimWorld;
using Verse;

namespace Carnivale
{
    [StaticConstructorOnStartup]
    public class CarnivaleMod : ModBase
    {
        public override string ModIdentifier { get { return "Carnivale"; } }

        protected override bool HarmonyAutoPatch { get { return false; } }


        static CarnivaleMod()
        {
            
        }

    }

}