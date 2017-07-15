using RimWorld;
using Verse;

namespace Carnivale
{
    public class Building_TentFlap : Building_Door
    {
        public bool everAvailableToNonCarnies = false;

        public Building_Tent parent;

        public override bool PawnCanOpen(Pawn p)
        {
            if (p.Faction == this.Faction) return true;

            if (parent.OccupiedRect().Contains(p.Position)) return true;

            if (!everAvailableToNonCarnies) return false;

            if (p.Faction.HostileTo(this.Faction)) return false;

            if (this.Map.GetComponent<CarnivalInfo>().allowedColonists.Contains(p)) return true;

            return false;
        }


        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref this.everAvailableToNonCarnies, "availableToNonCarnies", false);
        }

    }
}
