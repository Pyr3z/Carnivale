using RimWorld;

namespace Carnivale
{
    public static class Utilities
    {
        public static bool IsCarnival(this Faction faction)
        {
            //return faction.def == _FactionDefOf.Carn_Faction_Roaming;
            return faction.def.defName.StartsWith("Carn_");
        }
    }
}
