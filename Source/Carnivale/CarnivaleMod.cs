using HugsLib;
using RimWorld;
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
            InjectFrameStuffHack();

            TheOne.Instantiate();
        }


        public override void WorldLoaded()
        {
            base.WorldLoaded();

            DynamicallyAddFactions();
        }



        private static void InjectFrameStuffHack()
        {
            // Inject implied Frame defs with Frame_StuffHacked
            foreach (ThingDef def in from d in DefDatabase<ThingDef>.AllDefs
                                     where d.isFrame
                                        && d.entityDefToBuild.stuffCategories != null
                                        && d.entityDefToBuild.stuffCategories.Contains(_DefOf.StuffedCrate)
                                     select d)
            {
                if (Prefs.DevMode)
                    Log.Message("[Carnivale] Successfully injected stuff hack to implied FrameDef " + def.defName);
                def.thingClass = typeof(Frame_StuffHacked);
                def.tickerType = TickerType.Normal;
            }
        }

        private static void DynamicallyAddFactions()
        {
            // Check if any carnival factions. If not, generate them.
            if (!Find.FactionManager.AllFactionsListForReading.Any(f => f.IsCarnival()))
            {
                var fdef = _DefOf.Carn_Faction_Roaming;
                int num = Rand.RangeInclusive(fdef.requiredCountAtGameStart, fdef.maxCountAtGameStart);
                for (int i = 0; i < num; i++)
                {
                    var faction = FactionGenerator.NewGeneratedFaction(fdef);
                    Find.FactionManager.Add(faction);
                    Find.VisibleMap.pawnDestinationManager.RegisterFaction(faction);

                    if (Prefs.DevMode)
                        Log.Warning("[Carnivale] Dynamically added new carnival faction " + faction + " to game.");
                }
            }
        }
    }

}