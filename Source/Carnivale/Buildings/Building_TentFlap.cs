using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Carnivale
{
    public class Building_TentFlap : Building_Door
    {
        // List is more efficient than HashSet for element count under 20
        private List<int> allowedColonistIDs = new List<int>(Find.VisibleMap.mapPawns.ColonistCount);

        public bool everAvailableToNonCarnies = false;




        public void Notify_ColonistPaidEntry(Pawn col)
        {
            allowedColonistIDs.Add(col.thingIDNumber);
        }

        public override bool PawnCanOpen(Pawn p)
        {
            if (p.Faction == this.Faction) return true;

            if (!everAvailableToNonCarnies) return false;

            if (p.Faction.HostileTo(this.Faction)) return false;

            if (allowedColonistIDs.Contains(p.thingIDNumber)) return true;
            else return false;
        }


        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref this.allowedColonistIDs, "allowedColonistsIDs", LookMode.Value);

            Scribe_Values.Look(ref this.everAvailableToNonCarnies, "availableToNonCarnies", false);
        }

    }
}
