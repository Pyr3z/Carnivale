using System.Collections.Generic;
using RimWorld;
using Verse;
using System.Linq;
using UnityEngine;

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
            // Validation steps
            if (!CanGenerateFrom(parms, groupMaker) || !ValidateTradersList(groupMaker) || !ValidateCarriers(groupMaker))
            {
                if (errorOnZeroResults)
                    Log.Error("Cannot generate carnival caravan for " + parms.faction + ": zero results.");
                return;
            }
            // End validation steps

            foreach (TraderKindDef traderKind in parms.faction.def.caravanTraderKinds)
            {
                // For now, generates one vendor of each kind. TODO: Generate based on commonality.
                Pawn vendor = GenerateVendor(parms, groupMaker, traderKind);
                outPawns.Add(vendor);

                // Generate wares
                ItemCollectionGeneratorParams waresParms = default(ItemCollectionGeneratorParams);
                waresParms.traderDef = traderKind;
                waresParms.forTile = parms.tile;
                waresParms.forFaction = parms.faction;
                List<Thing> wares = ItemCollectionGeneratorDefOf.TraderStock.Worker.Generate(waresParms).InRandomOrder(null).ToList();

                // Spawn parns that are for sale
                foreach (Pawn sellable in GetPawnsFromWares(parms, wares))
                    outPawns.Add(sellable);

                GenerateCarriers(parms, groupMaker, wares, outPawns);
            }

            GenerateGuards(parms, groupMaker, outPawns);
        }


        /* Private Methods */


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
                true, // Force free warm layers if needed
                true,
                true,
                parms.inhabitants,
                false,
                null, // Consider adding predicate for backstory here
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



        private void GenerateCarriers(PawnGroupMakerParms parms, PawnGroupMaker groupMaker, List<Thing> wares, List<Pawn> outPawns)
        {
            List<Thing> waresList = (from x in wares
                                         where !(x is Pawn)
                                         select x).ToList();
            List<Pawn> carrierList = new List<Pawn>();
            PawnKindDef carrierKind = (from x in groupMaker.carriers
                                       where parms.tile == -1
                                       || Find.WorldGrid[parms.tile].biome.IsPackAnimalAllowed(x.kind.race)
                                       select x).RandomElementByWeight((PawnGenOption o) => o.selectionWeight).kind;
            int i = 0;
            int numCarriers = Mathf.CeilToInt(waresList.Count / 8f);

            for (int j = 0; j < numCarriers; j++)
            {
                // Generate carrier
                PawnGenerationRequest request = new PawnGenerationRequest(
                    carrierKind,
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
                Pawn carrier = PawnGenerator.GeneratePawn(request);
                if (i < waresList.Count)
                {
                    // Add initial few items to carrier
                    carrier.inventory.innerContainer.TryAdd(waresList[i], true);
                    i++;
                }
                carrierList.Add(carrier);
                outPawns.Add(carrier);
            }

            // Finally, fill up all the carriers' inventories
            while (i < waresList.Count)
            {
                carrierList.RandomElement<Pawn>().inventory.innerContainer.TryAdd(waresList[i], true);
                i++;
            }
        }



        private void GenerateGuards(PawnGroupMakerParms parms, PawnGroupMaker groupMaker, List<Pawn> outPawns)
        {
            if (!groupMaker.guards.Any())
                return;

            // TODO: adjust points for guards?
            float points = parms.points;

            foreach (PawnGenOption option in PawnGroupMakerUtility.ChoosePawnGenOptionsByPoints(points, groupMaker.guards, parms))
            {
                PawnGenerationRequest request = new PawnGenerationRequest(
                    option.kind,
                    parms.faction,
                    PawnGenerationContext.NonPlayer,
                    parms.tile,
                    false,
                    false,
                    false,
                    false,
                    true,
                    true,
                    1f,
                    true, // Force free warm layers if needed
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
                Pawn guard = PawnGenerator.GeneratePawn(request);
                outPawns.Add(guard);
            }
        }



        private IEnumerable<Pawn> GetPawnsFromWares(PawnGroupMakerParms parms, List<Thing> wares)
        {
            foreach (var thing in wares)
            {
                Pawn p = thing as Pawn;
                if (p != null)
                {
                    if (p.Faction != parms.faction)
                        p.SetFaction(parms.faction, null);
                    yield return p;
                }
            }
        }



        /* Validation Private Methods */



        private bool ValidateCarriers(PawnGroupMaker groupMaker)
        {
            PawnGenOption pawnGenOption = groupMaker.carriers.FirstOrDefault((PawnGenOption x) => !x.kind.RaceProps.packAnimal);
            if (pawnGenOption != null)
            {
                Log.Error(string.Concat(new object[]
                {
                    "Cannot generate arriving carnival for ",
                    "Carn_Faction_Roaming",
                    " because there is a pawn kind (",
                    pawnGenOption.kind.LabelCap,
                    ") who is not a carrier but is in a carriers list."
                }));
                return false;
            }

            return true;
        }

        private bool ValidateTradersList(PawnGroupMaker groupMaker)
        {
            // Returns false if there is an error in PawnGenOption XML (in Faction def)
            PawnGenOption pawnGenOption = groupMaker.traders.FirstOrDefault((PawnGenOption x) => !x.kind.trader);
            if (pawnGenOption != null)
            {
                Log.Error(string.Concat(new object[]
                {
                    "Cannot generate arriving carnival for ",
                    "Carn_Faction_Roaming",
                    " because there is a pawn kind (",
                    pawnGenOption.kind.LabelCap,
                    ") who is not a trader but is in a traders list."
                }));
                return false;
            }

            return true;
        }
    }
}
