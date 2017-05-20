using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Carnivale.PawnGroupKindWorkers
{
    public class Carnival : PawnGroupKindWorker
    {
        public override float MinPointsToGenerateAnything(PawnGroupMaker groupMaker)
        { return 0f; }

        public override bool CanGenerateFrom(PawnGroupMakerParms parms, PawnGroupMaker groupMaker)
        {
            return base.CanGenerateFrom(parms, groupMaker) &&
                parms.faction.IsCarnival() &&
                (parms.tile == -1 ||
                    groupMaker.carriers.Any((PawnGenOption x) => Find.WorldGrid[parms.tile].biome.IsPackAnimalAllowed(x.kind.race)));
        }

        protected override void GeneratePawns(PawnGroupMakerParms parms, PawnGroupMaker groupMaker, List<Pawn> outPawns, bool errorOnZeroResults = true)
        {
            if (!this.CanGenerateFrom(parms, groupMaker))
            {
                if (errorOnZeroResults)
                    Log.Error("Cannot generate carnival caravan for " + parms.faction + ".");
                return;
            }


        }

        private Pawn GenerateVendor(PawnGroupMakerParms parms, PawnGroupMaker groupMaker, TraderKindDef traderKind)
        {
            PawnGenerationRequest request = new PawnGenerationRequest(
                groupMaker.traders.RandomElementByWeight(
                    (PawnGenOption x) => (float)x.selectionWeight
                ).kind,
                parms.faction,
                PawnGenerationContext.NonPlayer,
                parms.tile,
                false,
                false,
                false,
                false,
                true,
                false,
                1f,
                false,
                true,
                true,
                parms.inhabitants,
                false,
                null,
                null,
                null,
                null,
                null,
                null
            );

            Pawn vendor = PawnGenerator.GeneratePawn(request);

            vendor.mindState.wantsToTradeWithColony = true;
            PawnComponentsUtility.AddAndRemoveDynamicComponents(vendor, true);
            vendor.trader.traderKind = traderKind;
            parms.points -= vendor.kindDef.combatPower;
            return vendor;
        }

        private void GenerateCarriers(PawnGroupMakerParms parms, PawnGroupMaker group, Pawn trader, List<Thing> wares, List<Pawn> outPawns)
        {

        }

        private void GenerateGuards(PawnGroupMakerParms parms, PawnGroupMaker groupMaker, List<Pawn> outPawns)
        {

        }
    }
}
