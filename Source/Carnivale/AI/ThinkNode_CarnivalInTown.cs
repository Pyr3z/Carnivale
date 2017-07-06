using RimWorld;
using Verse;

namespace Carnivale
{
    public class ThinkNode_CarnivalInTown : ThinkNode_Conditional
    {

        protected override bool Satisfied(Pawn pawn)
        {
            var info = pawn.MapHeld.GetComponent<CarnivalInfo>();

            return info.Active && info.entertainingNow && !pawn.IsCarny();
        }

    }
}
