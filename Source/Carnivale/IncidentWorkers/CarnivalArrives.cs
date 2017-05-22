using Carnivale.Defs;
using RimWorld;
using Verse;

namespace Carnivale.IncidentWorkers
{
    class CarnivalArrives : IncidentWorker_PawnsArrive
    {
        protected virtual PawnGroupKindDef PawnGroupKindDef
        { get { return _PawnGroupKindDefOf.Carnival; } }


        protected override bool FactionCanBeGroupSource(Faction f, Map map, bool desperate = false)
        {
            return f.IsCarnival() &&
                base.FactionCanBeGroupSource(f, map, desperate);
        }


    }
}
