using Verse;
using Verse.AI.Group;

namespace Carnivale
{
    public class LordToil_RestCarnival : LordToil_Carn
    {
        // FIELDS + PROPERTIES //

        


        // CONSTRUCTORS //

        public LordToil_RestCarnival() { }

        public LordToil_RestCarnival(CarnivalInfo info)
        {
            this.data = new LordToilData_Carnival(info);
        }


        // OVERRIDE METHODS //

        public override void UpdateAllDuties()
        {
            
        }
    }
}
