using Verse;

namespace Carnivale
{
    public class CompCarnBuilding : ThingComp
    {

        public CompProperties_CarnBuilding Props
        {
            get
            {
                return (CompProperties_CarnBuilding)this.props;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            // todo : move Building_Carn spawn setup here?
        }
    }
}
