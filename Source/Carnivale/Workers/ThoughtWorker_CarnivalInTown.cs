using RimWorld;
using Verse;

namespace Carnivale
{
    public class ThoughtWorker_CarnivalInTown : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            // No carnies
            if (p.IsCarny()) return false;

            if (CarnUtils.Info.Active)
            {
                // Pessimists / Depressives get reduced benefit
                var naturalMood = p.story.traits.GetTrait(TraitDefOf.NaturalMood);
                if (naturalMood != null && naturalMood.Degree < 0)
                {
                    // for some reason, reasons do not do anything?
                    return ThoughtState.ActiveAtStage(1, "CarnInTownThought1".Translate(naturalMood.Label, p.NameStringShort));
                }
                else
                {
                    // for some reason, reasons do not do anything?
                    return ThoughtState.ActiveAtStage(0, "CarnInTownThought0".Translate());
                }
            }

            return false;
        }

    }
}
