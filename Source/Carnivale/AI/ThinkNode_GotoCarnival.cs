using RimWorld;
using Verse;

namespace Carnivale
{
    public class ThinkNode_GotoCarnival : ThinkNode_Conditional
    {

        protected override bool Satisfied(Pawn pawn)
        {

            if (pawn.needs.joy == null
                || JoyUtility.LordPreventsGettingJoy(pawn))
                return false;

            var info = pawn.MapHeld.GetComponent<CarnivalInfo>();
            var timeAssignment = pawn.timetable != null ? pawn.timetable.CurrentAssignment : TimeAssignmentDefOf.Anything;

            return info.Active && info.entertainingNow && !pawn.IsCarny() && timeAssignment.allowJoy;
        }

    }
}
