using RimWorld;
using Verse;

namespace Carnivale
{
    public class Building_TentFlap : Building_Door
    {

        public override bool PawnCanOpen(Pawn p)
        {
            if (p.Faction != this.Faction)
                return false;

            return base.PawnCanOpen(p);
        }

    }
}
