using System.Collections.Generic;
using RimWorld;
using Verse.AI;
using Xnope;
using Verse;
using System.Linq;

namespace Carnivale
{
    public abstract class JobDriver_PlayCarnGame : JobDriver
    {
        [Unsaved]
        protected bool victory = false;
        [Unsaved]
        protected Thing prize = null;
        [Unsaved]
        private CarnivalInfo infoInt;

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

        protected Building_Carn GameBuilding
        {
            get
            {
                return TargetThingA as Building_Carn;
            }
        }

        protected Pawn AssignedCarny
        {
            get
            {
                return GameBuilding.assignedPawn;
            }
        }

        protected string PrizeLabelShort
        {
            get
            {
                if (prize != null)
                {
                    var label = prize.GetInnerIfMinified().LabelNoCount;
                    int index = label.FirstIndexOf(c => c == '(') - 1;
                    if (index > 0 && index < label.Length - 1)
                    {
                        label = label.Substring(0, index);
                    }

                    return label;
                }

                return "nothing";
            }
        }

        public override string GetReport()
        {
            return "playing " + TargetThingA.LabelShort + ".";
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.EndOnDespawnedOrNull(TargetIndex.A, JobCondition.Incompletable);

            // Reserve the building
            yield return Toils_Reserve.Reserve(TargetIndex.A);

            // Goto interaction cell
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.InteractionCell);

            // "Watch" the building
            yield return WatchBuilding();

            // Check victory
            yield return CheckVictory();

            yield return DropPrize();
        }


        private Toil WatchBuilding()
        {
            var toil = new Toil();

            toil.initAction = delegate
            {
                pawn.Rotation = pawn.Position.RotationFacing(TargetLocA);
            };

            toil.AddPreTickAction(delegate
            {
                if (WatchTickAction())
                {
                    this.victory = true;
                    this.ReadyForNextToil();
                }
            });
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = base.CurJob.def.joyDuration;

            return toil;
        }

        private Toil CheckVictory()
        {
            var toil = new Toil();

            toil.initAction = delegate
            {
                if (this.victory && AssignedCarny != null && ChoosePrize())
                {
                    this.CurJob.SetTarget(TargetIndex.B, AssignedCarny);
                    this.ReadyForNextToil();
                }
                else
                {
                    EndJobWith(JobCondition.Succeeded);
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = 50;

            return toil;
        }

        private Toil DropPrize()
        {
            return new Toil
            {
                initAction = delegate
                {
                    if (this.prize != null)
                    {
                        Messages.Message("PawnWonPrize".Translate(pawn, PrizeLabelShort), MessageSound.Benefit);
                        var carrierInventory = this.prize.holdingOwner;
                        
                        if (!carrierInventory.TryDrop(prize, ThingPlaceMode.Near, out this.prize))
                        {
                            Log.Error("Could not drop " + this.prize);

                            EndJobWith(JobCondition.Errored);
                        }

                        prize.Position = AssignedCarny.Position + IntVec3.West;

                        MoteMaker.ThrowText(AssignedCarny.DrawPos, Map, "YouWon".Translate(PrizeLabelShort), 5);
                    }
                    else
                    {
                        EndJobWith(JobCondition.Succeeded);
                    }
                }
            };
        }

        //private Toil WaitToGivePrize()
        //{
        //    var toil = new Toil
        //    {
        //        initAction = delegate
        //        {
        //            this.pawn.pather.StartPath(AssignedCarny, PathEndMode.Touch);
        //        },
        //        defaultCompleteMode = ToilCompleteMode.Delay,
        //        defaultDuration = 30
        //    };

        //    toil.AddFinishAction(delegate
        //    {
        //        Thing prize;
        //        AssignedCarny.carryTracker.TryDropCarriedThing(pawn.Rotation.FacingCell, ThingPlaceMode.Direct, out prize);
        //        this.pawn.inventory.innerContainer.TryAdd(prize);
        //    });

        //    return toil;
        //}


        protected virtual bool WatchTickAction()
        {
            var extraJoyGainFactor = TargetThingA.GetStatValue(StatDefOf.EntertainmentStrengthFactor);
            JoyUtility.JoyTickCheckEnd(this.pawn, JoyTickFullJoyAction.None, extraJoyGainFactor);

            return false;
        }

        protected virtual bool ChoosePrize()
        {
            return ChooseApparel();
        }

        protected bool ChooseApparel()
        {
            return Info.pawnsWithRole[CarnivalRole.Vendor].First().trader.Goods
                   .Where(t => t is Apparel)
                   .TryRandomElement(out this.prize);
        }
    }
}
