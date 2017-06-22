using Verse;
using Verse.AI.Group;

namespace Carnivale
{
    public abstract class LordToil_Carn : LordToil
    {
        // FIELDS + PROPERTIES //

        [Unsaved]
        private CarnivalInfo infoInt = null;

        protected CarnivalInfo Info
        {
            get
            {
                if (infoInt == null)
                {
                    infoInt = this.Map.GetComponent<CarnivalInfo>();
                }
                return infoInt;
            }
        }

        public override IntVec3 FlagLoc { get { return Info.setupCentre; } }


        // CONSTRUCTORS //

        public LordToil_Carn() { }

    }
}
