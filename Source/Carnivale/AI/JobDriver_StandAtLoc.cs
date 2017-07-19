using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Carnivale
{
    public class JobDriver_StandAtLoc : JobDriver
    {
        private static string[] foodVendorMotes0Arg = new string[]
        {
            "YouThere",
            "FoodHere",
            "YouLookHungry"
        };

        private static string[] foodVendorMotes1Arg = new string[]
        {
            "HowAboutA",
            "GetYour"
        };

        private static string[] surplusVendorMotes0Arg = new string[]
        {
            "YouThere",
            "BargainPrices",
            "LightlyUsed",
            "ThisCouldBeYours"
        };

        private static string[] surplusVendorMotes1Arg = new string[]
        {
            "LikeNew",
            "CheckOut"
        };

        private static string[] curiosVendorMotes0Arg = new string[]
        {
            "BargainPrices",
            "ThisCouldBeYours",
            "RareItems"
        };

        private static string[] curiosVendorMotes1Arg = new string[]
        {
            "CheckOut",
            "GetItWhile"
        };

        private static string[] announcerMotes0Arg = new string[]
        {
            "StepRightUp",
            "YouThere",
            "WelcomeCarnival"
        };

        private static IntRange tickRange = new IntRange(400, 650);

        [Unsaved]
        private bool moteArgs = false;

        [Unsaved]
        private CarnivalRole typeInt = 0;

        [Unsaved]
        private int tick = tickRange.RandomInRange;


        private CarnivalInfo Info
        {
            get
            {
                return Utilities.CarnivalInfo;
            }
        }

        private CarnivalRole Type
        {
            get
            {
                if (typeInt == 0)
                {
                    typeInt = this.pawn.GetCarnivalRole();
                }
                return typeInt;
            }
        }


        public override string GetReport()
        {
            // TODO: translations
            if (Type.Is(CarnivalRole.Vendor))
            {
                return "peddling goods.";
            }
            else if (Type.Is(CarnivalRole.Entertainer))
            {
                return "announcing.";
            }
            else
            {
                return "resting in place.";
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Go to cell
            Toil gotoCell = Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
            yield return gotoCell;

            Toil standToil;

            if (Type.Is(CarnivalRole.Vendor))
            {
                if (pawn.TraderKind == _DefOf.Carn_Trader_Curios)
                {
                    standToil = VendorStandWithMotes(curiosVendorMotes0Arg, curiosVendorMotes1Arg);
                }
                else if (pawn.TraderKind == _DefOf.Carn_Trader_Surplus)
                {
                    standToil = VendorStandWithMotes(surplusVendorMotes0Arg, surplusVendorMotes1Arg);
                }
                else
                {
                    standToil = VendorStandWithMotes(foodVendorMotes0Arg, foodVendorMotes1Arg);
                }
            }
            else if (Type.Is(CarnivalRole.Entertainer))
            {
                // TODO: differentiate between announcers and game masters
                standToil = StandWithMotes(announcerMotes0Arg);
            }
            else
            {
                standToil = CarrierStand();
            }

            standToil.socialMode = RandomSocialMode.SuperActive;
            standToil.defaultDuration = GenDate.TicksPerHour / 2;
            standToil.defaultCompleteMode = ToilCompleteMode.Delay;

            yield return standToil;
        }


        private Toil VendorStandWithMotes(string[] strings0Arg, string[] strings1Arg)
        {
            // Stand, no rotation, vendors want to trade, motes
            Toil toil = new Toil().FailOn(delegate(Toil t)
            {
                return t.actor.TraderKind == null;
            });

            toil.initAction = delegate
            {
                toil.actor.pather.StopDead();
                toil.actor.Rotation = Rot4.South;
                toil.actor.mindState.wantsToTradeWithColony = true;
            };
            toil.tickAction = delegate
            {
                if (toil.actor.IsHashIntervalTick(tick)
                    && Info.colonistsInArea.Any())
                {
                    if (!moteArgs)
                    {
                        MoteMaker.ThrowText(
                            toil.actor.DrawPos,
                            Map,
                            strings0Arg.RandomElement().Translate(),
                            3f
                        );
                    }
                    else
                    {
                        var randomWareLabel = toil.actor.trader.Goods
                            .RandomElementByWeight(t => t.GetInnerIfMinified().MarketValue)
                            .GetInnerIfMinified()
                            .LabelNoCount;
                        int index = randomWareLabel.FirstIndexOf(c => c == '(') - 1;
                        if (index > 0 && index < randomWareLabel.Length - 1)
                        {
                            randomWareLabel = randomWareLabel.Substring(0, index);
                        }

                        MoteMaker.ThrowText(
                            toil.actor.DrawPos,
                            Map,
                            strings1Arg.RandomElement().Translate(randomWareLabel).CapitalizeFirst(),
                            5f
                        );
                    }

                    moteArgs = !moteArgs;
                }
            };
            toil.AddFinishAction(delegate
            {
                toil.actor.mindState.wantsToTradeWithColony = false;
            });

            return toil;
        }

        private Toil StandWithMotes(string[] strings0Arg)
        {
            // Stand, no rotation, motes
            Toil toil = new Toil();

            toil.initAction = delegate
            {
                toil.actor.pather.StopDead();
                toil.actor.Rotation = Rot4.South;
            };
            toil.tickAction = delegate
            {
                if (toil.actor.IsHashIntervalTick(tick)
                    && Info.colonistsInArea.Any())
                {
                    MoteMaker.ThrowText(
                        toil.actor.DrawPos,
                        Map,
                        strings0Arg.RandomElement().Translate(pawn.Faction),
                        3f
                    );
                }
            };

            return toil;
        }

        private Toil CarrierStand()
        {
            // stand, rotate randomly
            Toil toil = new Toil();

            toil.initAction = delegate
            {
                toil.actor.pather.StopDead();
            };
            toil.tickAction = delegate
            {
                if (toil.actor.IsHashIntervalTick(tick))
                {
                    toil.actor.Rotation = Rot4.Random;
                }
            };

            return toil;
        }


    }
}
