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


        [Unsaved]
        private bool moteArgs = false;

        [Unsaved]
        private CarnivalInfo infoInt = null;

        [Unsaved]
        private CarnivalRole typeInt = 0;



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

            if (Type.Is(CarnivalRole.Vendor))
            {
                if (pawn.TraderKind == _DefOf.Carn_Trader_Curios)
                {
                    yield return VendorStandWithMotes(curiosVendorMotes0Arg, curiosVendorMotes1Arg);
                }
                else if (pawn.TraderKind == _DefOf.Carn_Trader_Surplus)
                {
                    yield return VendorStandWithMotes(surplusVendorMotes0Arg, surplusVendorMotes1Arg);
                }
                else
                {
                    yield return VendorStandWithMotes(foodVendorMotes0Arg, foodVendorMotes1Arg);
                }
            }
            else if (Type.Is(CarnivalRole.Entertainer))
            {
                // TODO: differentiate between announcers and game masters
                yield return StandWithMotes(announcerMotes0Arg);
            }
            else
            {
                yield return CarrierStand();
            }
            
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
                if (Find.TickManager.TicksGame % 307 == 0
                    && Info.colonistsInArea.Any()
                    && Rand.Chance(0.314f))
                {
                    if (!moteArgs)
                    {
                        MoteMaker.ThrowText(
                            toil.actor.DrawPos,
                            Map,
                            strings0Arg.RandomElement().Translate(),
                            5f
                        );

                        moteArgs = true;
                    }
                    else
                    {
                        var randomWare = toil.actor.trader.Goods
                            .RandomElementByWeight(t => t.GetInnerIfMinified().MarketValue)
                            .GetInnerIfMinified();

                        MoteMaker.ThrowText(
                            toil.actor.DrawPos,
                            Map,
                            strings1Arg.RandomElement().Translate(randomWare.LabelNoCount),
                            5f
                        );

                        moteArgs = false;
                    }
                }
            };
            toil.AddFinishAction(delegate
            {
                toil.actor.mindState.wantsToTradeWithColony = false;
            });
            toil.defaultDuration = GenDate.TicksPerHour;
            toil.defaultCompleteMode = ToilCompleteMode.Delay;

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
                if (Find.TickManager.TicksGame % 307 == 0
                    && Info.colonistsInArea.Any()
                    && Rand.Chance(0.314f))
                {
                    MoteMaker.ThrowText(
                        toil.actor.DrawPos,
                        Map,
                        strings0Arg.RandomElement().Translate(),
                        5f
                    );
                }
            };
            toil.defaultDuration = GenDate.TicksPerHour;
            toil.defaultCompleteMode = ToilCompleteMode.Delay;

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
                if (Find.TickManager.TicksGame % 307 == 0
                    && Rand.Chance(0.4f))
                {
                    toil.actor.Rotation = Rot4.Random;
                }
            };
            toil.defaultDuration = GenDate.TicksPerHour;
            toil.defaultCompleteMode = ToilCompleteMode.Delay;

            return toil;
        }


    }
}
