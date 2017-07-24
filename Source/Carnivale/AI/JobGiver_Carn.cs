using Verse;
using Verse.AI;

namespace Carnivale
{
    public abstract class JobGiver_Carn : ThinkNode_JobGiver
    {
        protected CarnivalInfo Info
        {
            get
            {
                return CarnUtils.Info;
            }
        }

        protected bool Validate()
        {
            return Info != null && Info.Active;
        }
    }
}
