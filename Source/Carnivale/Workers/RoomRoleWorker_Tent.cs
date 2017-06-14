using Verse;

namespace Carnivale
{
    public class RoomRoleWorker_Tent : RoomRoleWorker
    {

        public override float GetScore(Room room)
        {
            // simple stuff for now
            var things = room.ContainedAndAdjacentThings;
            foreach (var thing in things)
            {
                if (thing.def == _DefOf.Carn_TentWall || thing is Building_Tent)
                {
                    // 801000 puts this just above barracks with 8 sleeping spots (800800)
                    return 801000f;
                }
            }

            return 0f;
        }

    }
}
