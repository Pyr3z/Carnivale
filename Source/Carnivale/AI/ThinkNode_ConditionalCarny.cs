using RimWorld;
using Verse;

namespace Carnivale
{
    public class ThinkNode_ConditionalCarny : ThinkNode_Conditional
    {

        protected override bool Satisfied(Pawn pawn)
        {
            return pawn.IsCarny();
        }

    }
}
