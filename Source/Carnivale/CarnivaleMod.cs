using HugsLib;
using HugsLib.Settings;
using RimWorld;
using RimWorld.Planet;
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

        private SettingHandle<bool> nuke;


        static CarnivaleMod()
        {
            
        }


        public override void DefsLoaded()
        {
            base.DefsLoaded();

            nuke = Settings.GetHandle<bool>("doNuke", "DoNuke".Translate(), "DoNukeDesc".Translate(), false);
            nuke.Unsaved = true;
            nuke.OnValueChanged = delegate (bool n)
            {
                if (n)
                {
                    NukeEverything(Current.Game);
                }
            };

            InjectFrameStuffHack();

            TheOne.Instantiate();
        }


        public override void WorldLoaded()
        {
            base.WorldLoaded();

            DynamicallyAddFactions();
        }

        public override void MapLoaded(Map map)
        {
            base.MapLoaded(map);

            CarnUtils.Cleanup();
        }


        public static void NukeEverything(Game game)
        {
            ProfilerThreadCheck.BeginSample("CarnivaleNuke");

            var facList = game.World.factionManager.AllFactionsListForReading;
            for (int i = facList.Count - 1; i > 0; i--)
            {
                var fac = facList[i];
                if (fac.IsCarnival())
                {
                    foreach (var map in game.Maps)
                    {
                        var lord = map.lordManager.lords.FirstOrDefault(l => l.LordJob is LordJob_EntertainColony);

                        if (lord != null)
                        {
                            map.lordManager.RemoveLord(lord);
                        }

                        foreach (var pawn in map.mapPawns.AllPawns)
                        {
                            if (fac == pawn.Faction || pawn.IsCarny(false))
                            {
                                pawn.DeSpawn();
                                game.World.worldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
                            }
                            else if (pawn.needs != null && pawn.needs.mood != null)
                            {
                                pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(_DefOf.Thought_AttendedShow);
                                pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(_DefOf.Thought_MissCarnival);
                            }
                        }

                        foreach (var thing in map.listerThings.AllThings.Where(t => fac == t.Faction))
                        {
                            thing.Destroy();
                        }
                    }

                    fac.RemoveAllRelations();
                    facList.RemoveAt(i);
                }
            }

            List<Pawn> tmpPawnsToRemove = new List<Pawn>();
            foreach (var pawn in game.World.worldPawns.AllPawnsAliveOrDead)
            {
                if (pawn.IsCarny(false))
                {
                    tmpPawnsToRemove.Add(pawn);
                }
                else if (pawn.needs != null && pawn.needs.mood != null)
                {
                    pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(_DefOf.Thought_AttendedShow);
                    pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(_DefOf.Thought_MissCarnival);
                }
            }

            foreach (var pawn in tmpPawnsToRemove)
            {
                game.World.worldPawns.RemovePawn(pawn);
            }

            foreach (var map in game.Maps)
            {
                var info = map.GetComponent<CarnivalInfo>();

                if (info != null)
                {
                    info.Cleanup();
                    map.components.Remove(info);
                }

                map.pawnDestinationManager = new PawnDestinationManager();
            }

            game.storyteller.incidentQueue.Clear();

            string fileName;
            if (game.Info.permadeathMode)
            {
                fileName = game.Info.permadeathModeUniqueName;
            }
            else
            {
                fileName = game.AnyPlayerHomeMap.info.parent.LabelCap;
            }

            GameDataSaveLoader.SaveGame(fileName + "_Nuked");

            ProfilerThreadCheck.EndSample();
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
                        Log.Message("[Carnivale] Dynamically added new carnival faction " + faction + " to game.");
                }
            }
        }
    }

}