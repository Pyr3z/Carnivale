using Carnivale.Defs;
using RimWorld;

namespace Carnivale
{
    public class IncidentWorker_CarnivalApproaches : IncidentWorker_NeutralGroup
    {
        public override float AdjustedChance
        {
            get
            {
                // TODO: Adjust based off proximity to roads
                return base.AdjustedChance;
            }
        }

        protected override PawnGroupKindDef PawnGroupKindDef
        {
            get { return _PawnGroupKindDefOf.Carnival;}
        }
    }
}
