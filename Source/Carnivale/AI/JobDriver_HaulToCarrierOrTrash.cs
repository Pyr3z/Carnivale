using RimWorld;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Carnivale
{
    public class JobDriver_HaulToCarrierOrTrash : JobDriver
    {
        private const int PlaceInInventoryDuration = 25;

        [Unsaved]
        private HaulLocation destInt = 0;

        public Thing ThingToHaul
        {
            get
            {
                return base.CurJob.GetTarget(TargetIndex.A).Thing;
            }
        }

        private Pawn Carrier
        {
            get
            {
                return (Pawn)base.CurJob.GetTarget(TargetIndex.B).Thing;
            }
        }

        private CarnivalInfo Info
        {
            get
            {
                return CarnUtils.Info;
            }
        }

        private HaulLocation DestType
        {
            get
            {
                if (destInt == 0)
                {
                    destInt = ThingToHaul.DefaultHaulLocation(true);
                }

                return destInt;
            }
        }

        public override string GetReport()
        {
            if (DestType == HaulLocation.ToTrash)
            {
                return "hauling " + ThingToHaul.Label + " to trash.";
            }

            if (DestType == HaulLocation.ToCarriers)
            {
                if (Carrier != null)
                {
                    return "hauling " + ThingToHaul.Label + " to " + Carrier.Label + ".";
                }
                else
                {
                    return "hauling " + ThingToHaul.Label + " to carrier.";
                }
            }

            return base.GetReport();
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(delegate
            {
                return DestType == HaulLocation.None
                       || !Info.ShouldHaulTrash
                       || Info.currentLord != pawn.GetLord()
                       || !Info.AnyCarriersCanCarry(this.ThingToHaul);
            });

            Toil reserve = Toils_Reserve.Reserve(TargetIndex.A);

            yield return reserve; // reserve if not already reserved by this pawn

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            yield return this.DetermineNumToHaul();

            yield return Toils_Haul.StartCarryThing(TargetIndex.A, false, true);

            yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserve, TargetIndex.A, TargetIndex.None, false, t => Info.thingsToHaul.Contains(t));

            if (DestType == HaulLocation.ToCarriers)
            {
                Toil findCarrier = FindCarrier();

                yield return findCarrier;

                //yield return Toils_Reserve.Reserve(TargetIndex.B);

                yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch)
                    .JumpIf(() => !JobDriver_PrepareCaravan_GatherItems.IsUsableCarrier(this.Carrier, this.pawn, false), findCarrier);

                yield return Toils_General.Wait(PlaceInInventoryDuration).WithProgressBarToilDelay(TargetIndex.B, false, -0.5f);

                yield return PlaceTargetInCarrierInventory();
            }
            else if (DestType == HaulLocation.ToTrash)
            {
                Toil findTrashSpot = FindTrashSpot();

                yield return findTrashSpot;

                yield return Toils_Reserve.Reserve(TargetIndex.B);

                yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.Touch);

                yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.B, findTrashSpot, false);
            }

            yield return RemoveThingToHaulFromInfo();
        }




        private Toil DetermineNumToHaul()
        {
            return new Toil
            {
                initAction = delegate
                {
                    int num = Info.UnreservedThingsToHaulOf(this.ThingToHaul.def, this.pawn);

                    if (this.pawn.carryTracker.CarriedThing != null)
                    {
                        num -= this.pawn.carryTracker.CarriedThing.stackCount;
                    }

                    if (num <= 0)
                    {
                        this.pawn.jobs.EndCurrentJob(JobCondition.Succeeded, true);
                    }
                    else
                    {
                        base.CurJob.count = num;
                    }

                },
                defaultCompleteMode = ToilCompleteMode.Instant,
                atomicWithPrevious = true
            };
        }


        private Toil RemoveThingToHaulFromInfo()
        {
            return new Toil
            {
                initAction = delegate
                {
                    if (ThingToHaul != null
                        && Info.thingsToHaul.Remove(ThingToHaul))
                    {
                        if (Prefs.DevMode)
                            Log.Message("\t[Carnivale] thingsToHaul : Removing " + ThingToHaul + ".");
                    }
                }
            };
        }


        private Toil FindCarrier()
        {
            return new Toil
            {
                initAction = delegate
                {
                    Pawn carrier = null;
                    foreach (var car in Info.pawnsWithRole[CarnivalRole.Carrier])
                    {
                        if (car.HasSpaceFor(this.ThingToHaul))
                        {
                            carrier = car;
                            break;
                        }
                    }

                    if (carrier == null)
                    {
                        Log.Error("Could not find a carrier to carry " + ThingToHaul + ". A validation step failed somewhere.");
                    }
                    else
                    {
                        base.CurJob.SetTarget(TargetIndex.B, carrier);
                    }
                }
            };
        }



        private Toil FindTrashSpot()
        {
            // Doubting that this needs to be done this way.
            // Only use case is if this step needs to ever be jumped back to.
            return new Toil
            {
                initAction = delegate
                {
                    var target = Info.GetNextTrashCellFor(this.ThingToHaul, this.pawn);
                    if (!target.IsValid)
                    {
                        base.EndJobWith(JobCondition.Errored);
                    }
                    CurJob.SetTarget(TargetIndex.B, target);
                }
            };
        }



        private Toil PlaceTargetInCarrierInventory()
        {
            return new Toil
            {
                initAction = delegate
                {
                    Pawn_CarryTracker carryTracker = this.pawn.carryTracker;
                    Thing carriedThing = carryTracker.CarriedThing;
                    //this.Transferable.AdjustTo(Mathf.Max(this.Transferable.CountToTransfer - carriedThing.stackCount, 0));
                    if (carryTracker.innerContainer.TryTransferToContainer(carriedThing, this.Carrier.inventory.innerContainer, carriedThing.stackCount, true))
                    {
                        if (Prefs.DevMode)
                            Log.Message("\t[Carnivale] " + this.pawn + " succesfully hauled " + carriedThing + " to " + this.Carrier + ". pos=" + this.Carrier.Position);
                    }
                }
            };
        }

    }
}
