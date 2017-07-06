using RimWorld;
using Verse;
using Verse.AI;

namespace Carnivale
{
    public class ThinkNode_Priority_GotoCarnival : ThinkNode_Priority
    {
        public override float GetPriority(Pawn pawn)
        {
            Log.Warning("Reached ThinkNode_Priority_GotoCarnival");

            if (pawn.needs.joy == null
                || JoyUtility.LordPreventsGettingJoy(pawn))
                return 0f;

            var timeAssignment = pawn.timetable != null ? pawn.timetable.CurrentAssignment : TimeAssignmentDefOf.Anything;

            if (!timeAssignment.allowJoy)
                return 0f;

            if (timeAssignment == TimeAssignmentDefOf.Anything)
                return ThinkNodePriority.AnythingJoy;
            else if (timeAssignment == TimeAssignmentDefOf.Joy)
                return ThinkNodePriority.AssignedJoy;
            else
                if (pawn.needs.joy.CurLevel < 0.95f) return ThinkNodePriority.AvoidIdle;

            return 0f;
        }
    }
}
