using Verse;
using Verse.AI.Group;

namespace Carnivale
{
    public class LordToilData_AttendShow : LordToilData
    {
        public CellRect audienceRect;

        public Pawn entertainer;

        public IntVec3 entertainerSpot;

        public LordToilData_AttendShow(CellRect rect, Pawn entertainer, IntVec3 entertainerSpot)
        {
            this.audienceRect = rect;
            this.entertainer = entertainer;
            this.entertainerSpot = entertainerSpot;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.audienceRect, "audienceRect");
            Scribe_References.Look(ref this.entertainer, "entertainer");
            Scribe_Values.Look(ref this.entertainerSpot, "entertainerSpot");
        }
    }
}
