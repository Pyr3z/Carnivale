using RimWorld;
using Verse;

namespace Carnivale
{
    public class ThinkNode_InCarnivalArea : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            CarnivalInfo info = pawn.Map.GetComponent<CarnivalInfo>();
            if (info != null && info.currentLord != null)
            {
                return info.carnivalArea.Contains(pawn.PositionHeld);
            }
            return false;
        }
    }
}
