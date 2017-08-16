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
            // WHY THIS FUCKING NOT WORK
            return p.Faction == this.Faction
                || parent.OccupiedRect().Contains(p.Position)
                || (!p.Faction.HostileTo(this.Faction)
                    && availableToNonCarnies
                    /*&& CarnUtils.Info.entertainingNow*/);

            //return CarnUtils.Info.showingNow && CarnUtils.Info.allowedColonists.Contains(p);
        }


        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref this.availableToNonCarnies, "availableToNonCarnies");

            Scribe_References.Look(ref this.parent, "parentTent");
        }

    }
}
