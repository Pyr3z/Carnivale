using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Carnivale
{
    public class Building_TentFlap : Building_Door
    {
        // List is more efficient than HashSet for element count under 20
        private List<Pawn> allowedColonists = new List<Pawn>();

        public bool everAvailableToNonCarnies = false;




        public void Notify_ColonistPaidEntry(Pawn col)
        {
            allowedColonists.Add(col);
        }

        public override bool PawnCanOpen(Pawn p)
        {
            if (p.Faction == this.Faction) return true;

            if (!everAvailableToNonCarnies) return false;

            if (p.Faction.HostileTo(this.Faction)) return false;

            if (allowedColonists.Contains(p)) return true;

            return false;
        }


        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref this.allowedColonists, "allowedColonists", LookMode.Reference);

            Scribe_Values.Look(ref this.everAvailableToNonCarnies, "availableToNonCarnies", false);
        }

    }
}
