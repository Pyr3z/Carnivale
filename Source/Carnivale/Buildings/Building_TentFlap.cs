using RimWorld;
using Verse;

namespace Carnivale
{
    public class Building_TentFlap : Building_Door
    {
        public bool availableToNonCarnies = false;

        public Building_Tent parent;

        public override bool PawnCanOpen(Pawn p)
        {
            if (p.Faction == this.Faction) return true;

            if (parent.OccupiedRect().Contains(p.Position)) return true;

            if (!availableToNonCarnies) return false;

            if (p.Faction.HostileTo(this.Faction)) return false;

            return CarnUtils.Info.entertainingNow;

            // WHY THIS FUCKING NOT WORK
            //return CarnUtils.Info.showingNow && CarnUtils.Info.allowedColonists.Contains(p);
        }


        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref this.availableToNonCarnies, "availableToNonCarnies", false);

            Scribe_References.Look(ref this.parent, "parentTent");
        }

    }
}
