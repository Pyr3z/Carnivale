using Carnivale.Enums;
using HugsLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Carnivale
{
    [StaticConstructorOnStartup]
    public class CarnivaleMod : ModBase
    {
        public override string ModIdentifier { get { return "Carnivale"; } }

        protected override bool HarmonyAutoPatch { get { return true; } }


        static CarnivaleMod()
        {
            
        }


        public override void DefsLoaded()
        {
            // Inject implied Frame defs with Frame_Tent
            foreach (ThingDef def in from d in DefDatabase<ThingDef>.AllDefs
                                     where d.defName.StartsWith("Carn")
                                        && d.defName.EndsWith("_Frame")
                                     select d)
            {
                def.thingClass = typeof(Frame_Tent);
                def.tickerType = TickerType.Normal;
            }


            TheOne.Instantiate();
        }


    }

}