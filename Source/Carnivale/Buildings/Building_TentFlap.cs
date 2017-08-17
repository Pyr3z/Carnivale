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
            return p.Faction == null
                || p.Faction == this.Faction
                || parent.OccupiedRect().Contains(p.Position)
                || (!p.Faction.HostileTo(this.Faction)
                    && availableToNonCarnies
                    /*&& CarnUtils.Info.entertainingNow*/); // WHY THIS FUCKING NOT WORK

            //return CarnUtils.Info.showingNow && CarnUtils.Info.allowedColonists.Contains(p);
        }


        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref this.availableToNonCarnies, "availableToNonCarnies");

            Scribe_References.Look(ref this.parent, "parent"); // this doesn't work either?
        }

    }
}
