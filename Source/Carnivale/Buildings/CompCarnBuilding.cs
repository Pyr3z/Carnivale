using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Carnivale
{
    public class CompCarnBuilding : CompUsable
    {
        private CarnivalInfo infoInt = null;

        private CarnivalInfo Info
        {
            get
            {
                if (infoInt == null)
                {
                    infoInt = parent.Map.GetComponent<CarnivalInfo>();
                }

                return infoInt;
            }
        }

        public new CompProperties_CarnBuilding Props
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


        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn pawn)
        {
            if (parent.Faction != pawn.Faction && !Info.entertainingNow)
            {
                yield return new FloatMenuOption(this.FloatMenuOptionLabel + " (Carnival closed)", null);
            }
            else if (!pawn.CanReserve(this.parent))
            {
                yield return new FloatMenuOption(this.FloatMenuOptionLabel + " (" + "Reserved".Translate() + ")", null);
            }
            else
            {
                yield return new FloatMenuOption(this.FloatMenuOptionLabel, delegate
                {
                    if (pawn.CanReserveAndReach(this.parent, PathEndMode.InteractionCell, Danger.None))
                    {
                        this.TryStartUseJob(pawn);
                    }
                });
            }
        }
    }
}
