using HugsLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

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
                                     where d.isFrame
                                        && d.entityDefToBuild.stuffCategories != null
                                        && d.entityDefToBuild.stuffCategories.Contains(_DefOf.StuffedCrate)
                                     select d)
            {
                if (Prefs.DevMode)
                    Log.Message("Successfully injected stuff hack to implied FrameDef " + def.defName);
                def.thingClass = typeof(Frame_StuffHacked);
                def.tickerType = TickerType.Normal;
            }


            TheOne.Instantiate();
        }


    }

}