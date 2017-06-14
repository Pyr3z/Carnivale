using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace Carnivale
{
    public class ThinkNode_ConditionalCanInteractWithAnimals : ThinkNode_Conditional
    {

        public override float GetPriority(Pawn pawn)
        {
            return ThinkNodePriority.AnythingWork;
        }

        protected override bool Satisfied(Pawn pawn)
        {
            if (pawn.story.WorkTypeIsDisabled(WorkTypeDefOf.Handling)) return false;

            if (pawn.mindState.duty != null)
            {
                IntVec3 centre = pawn.mindState.duty.focus.IsValid ? (IntVec3)pawn.mindState.duty.focus : pawn.Position;
                int radius = pawn.mindState.duty.radius < 1 ? 6 : (int)pawn.mindState.duty.radius;
                CellRect rect = CellRect.CenteredOn(centre, radius);

                return ((from p in pawn.Map.mapPawns.PawnsInFaction(pawn.Faction)
                         where p.RaceProps.Animal
                            && rect.Contains(p.Position)
                         select p).Any());
            }

            return false;
        }
    }
}
