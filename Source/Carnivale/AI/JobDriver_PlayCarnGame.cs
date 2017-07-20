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

        private CarnivalInfo Info
        {
            get
            {
                return CarnivalUtils.Info;
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
                if (WatchTickAction() && GameBuilding.Faction != pawn.Faction)
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
                    Messages.Message("PawnWonPrize".Translate(pawn, PrizeLabelShort, GameBuilding.LabelShort), MessageSound.Benefit);

                    if (this.prize != null)
                    {
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


        /// <summary>
        /// What happens while the game is being played. Make it return true if the player wins.
        /// </summary>
        /// <returns></returns>
        protected virtual bool WatchTickAction()
        {
            var extraJoyGainFactor = TargetThingA.GetStatValue(StatDefOf.EntertainmentStrengthFactor);
            JoyUtility.JoyTickCheckEnd(this.pawn, JoyTickFullJoyAction.None, extraJoyGainFactor);

            return false;
        }

        protected virtual bool ChoosePrize()
        {
            return ChooseApparel() || ChooseBeer();
        }

        protected bool ChooseApparel()
        {
            return Info.pawnsWithRole[CarnivalRole.Vendor].First().trader.Goods
                   .Where(t => t is Apparel && t.MarketValue < 150)
                   .TryRandomElementByWeight(e => 1 / e.MarketValue, out this.prize);
        }

        protected bool ChooseBeer()
        {
            return Info.pawnsWithRole[CarnivalRole.Vendor].First().trader.Goods
                   .Where(t => t.def == ThingDefOf.Beer)
                   .TryRandomElementByWeight(e => 1 / e.stackCount, out this.prize);
        }
    }
}
