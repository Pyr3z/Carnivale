using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace Carnivale
{
    public class JobDriver_PayEntryFee : JobDriver
    {
        [Unsaved]
        private CarnivalInfo infoInt = null;

        private CarnivalInfo Info
        {
            get
            {
                if (infoInt == null)
                {
                    infoInt = Map.GetComponent<CarnivalInfo>();
                }
                return infoInt;
            }
        }

        public Thing SilverStack
        {
            get
            {
                return base.CurJob.GetTarget(TargetIndex.A).Thing;
            }
        }

        public Pawn TicketTaker
        {
            get
            {
                return base.CurJob.GetTarget(TargetIndex.B).Thing as Pawn;
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //this.EndOnDespawnedOrNull(TargetIndex.A, JobCondition.Incompletable);

            var reserve = Toils_Reserve.Reserve(TargetIndex.A);
            yield return reserve;

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            yield return this.DetermineNumToHaul();

            yield return Toils_Haul.StartCarryThing(TargetIndex.A, false, true);

            yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserve, TargetIndex.A, TargetIndex.None);


            var findTicketTaker = FindTicketTaker();
            yield return findTicketTaker;

            yield return Toils_Reserve.Reserve(TargetIndex.B);

            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch)
                    .JumpIf(() => !JobDriver_PrepareCaravan_GatherItems.IsUsableCarrier(TicketTaker, this.pawn, false), findTicketTaker);

            yield return GiveSilver();

            yield return Notify_ColonistPayedEntry();
        }


        private Toil DetermineNumToHaul()
        {
            return new Toil
            {
                initAction = delegate
                {
                    CurJob.count = Info.feePerColonist;
                },
                defaultCompleteMode = ToilCompleteMode.Instant,
                atomicWithPrevious = true
            };
        }

        private Toil FindTicketTaker()
        {
            return new Toil
            {
                initAction = delegate
                {
                    var ticketTaker = Info.GetBestAnnouncer(false);

                    if (ticketTaker == null)
                    {
                        Log.Error("Found no ticket taker to give silver to.");
                        base.EndJobWith(JobCondition.Errored);
                    }

                    DutyUtility.HitchToSpot(ticketTaker, ticketTaker.Position);

                    CurJob.SetTarget(TargetIndex.B, ticketTaker);
                }
            };
        }

        private Toil GiveSilver()
        {
            return new Toil
            {
                initAction = delegate
                {
                    var carryTracker = this.pawn.carryTracker;
                    var carriedThing = carryTracker.CarriedThing;
                    
                    if (carryTracker.innerContainer.TryTransferToContainer(carriedThing, TicketTaker.inventory.innerContainer, carriedThing.stackCount, true))
                    {
                        MoteMaker.ThrowText(
                            TicketTaker.DrawPos,
                            Map,
                            "EnjoyCarnival".Translate(),
                            3f
                        );

                        if (Prefs.DevMode)
                            Log.Warning("[Debug] " + this.pawn + " succesfully payed " + carriedThing.stackCount + " silver to " + TicketTaker.NameStringShort);
                    }
                }
            };
        }

        private Toil Notify_ColonistPayedEntry()
        {
            return new Toil
            {
                initAction = delegate
                {
                    //TargetThingB.SetForbidden(true, true);
                    Info.allowedColonists.Add(this.pawn);
                    EndJobWith(JobCondition.Succeeded);
                },
                defaultCompleteMode = ToilCompleteMode.Instant,
                atomicWithPrevious = true
            };
        }
    }
}
