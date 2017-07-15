using RimWorld;
using Verse;

namespace Carnivale
{
    public class CarnivaleGame : GameComponent
    {

        public CarnivaleGame(Game game)
        {
            
        }


        public override void LoadedGame()
        {
            base.LoadedGame();

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
                        Log.Warning("[Debug] Dynamically added new carnival faction " + faction + " to game.");
                }
            }
        }

    }
}
