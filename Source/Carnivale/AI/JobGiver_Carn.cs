using Verse;
using Verse.AI;

namespace Carnivale
{
    public abstract class JobGiver_Carn : ThinkNode_JobGiver
    {
        private CarnivalInfo infoInt = null;

        protected CarnivalInfo Info
        {
            get
            {
                if (infoInt == null)
                {
                    infoInt = Find.VisibleMap.GetComponent<CarnivalInfo>();
                }

                return infoInt;
            }
        }

        protected bool Validate()
        {
            return Info != null && Info.Active;
        }
    }
}
