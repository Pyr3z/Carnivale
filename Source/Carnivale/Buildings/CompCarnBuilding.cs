using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Carnivale
{
    public class CompCarnBuilding : CompUsable
    {
        [Unsaved]
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
            if (Props.useJob == null)
            {
                // no useJob specified
                yield break;
            }

            if (parent.Faction != pawn.Faction)
            {
                if (!Info.Active)
                {
                    // carnival is not in town for whatever reason
                    yield break;
                }
                else if (!Info.entertainingNow)
                {
                    // Carnival is closed
                    yield return new FloatMenuOption(this.FloatMenuOptionLabel + " (Carnival closed)", null);
                }
                else if (Props.useJob == _DefOf.Job_PayEntryFee)
                {
                    // Pay entry fee
                    yield return new FloatMenuOption(this.FloatMenuOptionLabel + " (" + Info.feePerColonist + ")", delegate
                    {
                        var silverCount = new ThingCountClass(ThingDefOf.Silver, Info.feePerColonist);
                        var silverStack = Utilities.FindClosestThings(pawn, silverCount);

                        if (silverStack != null && pawn.CanReserveAndReach(silverStack, PathEndMode.Touch, pawn.NormalMaxDanger()))
                        {
                            var job = new Job(Props.useJob, silverStack);
                            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                        }
                    });
                }
                else if (!Info.allowedColonists.Contains(pawn))
                {
                    // Pawn hasn't payed entry fee
                    yield return new FloatMenuOption(this.FloatMenuOptionLabel + " (Must pay fee at entrance)", null);
                }
            }
            
            if (!pawn.CanReserve(this.parent))
            {
                // Already reserved
                yield return new FloatMenuOption(this.FloatMenuOptionLabel + " (" + "Reserved".Translate() + ")", null);
            }
            else if (Info.entertainingNow && Props.type.Is(CarnBuildingType.Stall | CarnBuildingType.Attraction))
            {
                // Do use job (mostly for games)
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
