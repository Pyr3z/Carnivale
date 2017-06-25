using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Carnivale
{
    public class LordToil_DefendCarnival : LordToil_DefendPoint
    {

        private LordToil_DefendCarnival() { }

        public LordToil_DefendCarnival(IntVec3 defendPoint, float defendRadius) : base(defendPoint, defendRadius)
        { }

        public override void Init()
        {
            
        }

        public override void UpdateAllDuties()
        {
            foreach (var pawn in lord.ownedPawns)
            {

            }
        }

    }
}
