using Verse;
using Verse.AI.Group;

namespace Carnivale
{
    public class LordToilData_Carnival : LordToilData
    {
        public CarnivalInfo info;

        public LordToilData_Carnival() { }

        public LordToilData_Carnival(CarnivalInfo carnivalInfo)
        {
            this.info = carnivalInfo;
        }


        public override void ExposeData()
        {
            Scribe_References.Look(ref info, "info");
        }

    }
}
