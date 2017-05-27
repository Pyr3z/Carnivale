using System.Collections.Generic;
using RimWorld;
using Verse;
using System.Linq;
using UnityEngine;
using Carnivale.Defs;
using RimWorld.Planet;

namespace Carnivale
{
    public class PawnGroupKindWorker_Carnival : PawnGroupKindWorker
    {
        private float minPointsInt = 0f;



        public override float MinPointsToGenerateAnything(PawnGroupMaker groupMaker)
        {
            // NOTE: Just figured out that this would never be used for the
            // Carnival GroupKind. Leaving it here in case I can use it elsewhere.
            if (minPointsInt > 1f)
                return minPointsInt;

            float minPoints = 0;

            foreach (PawnGenOption option in groupMaker.traders)
            {
                minPoints += option.kind.combatPower * option.selectionWeight;
            }

            foreach (PawnGenOption option in groupMaker.guards)
            {
                minPoints += option.kind.combatPower * option.selectionWeight;
            }

            return (minPointsInt = minPoints);
        }



        public override bool CanGenerateFrom(PawnGroupMakerParms parms, PawnGroupMaker groupMaker)
        {
            return groupMaker.kindDef == _DefOf.Carnival &&
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
                    Log.Error("Cannot generate carnival caravan for " + parms.faction);
                return;
            }
            // End validation steps

            IEnumerable<Pawn> existingPawns = from p in Find.WorldPawns.AllPawnsAlive
                                              where p.Faction == parms.faction
                                                    && parms.faction.leader != p
                                              select p;


            // Generate vendors (costless)
            for (int i = 0; i < groupMaker.traders.FirstOrDefault().selectionWeight; i++)
            {
                TraderKindDef traderKind = parms.faction.def.caravanTraderKinds.RandomElementByWeight(k => k.commonality);
                Pawn vendor;

                if (existingPawns.Any() && (vendor = existingPawns.FirstOrDefault(p => p.TraderKind == traderKind)) != null)
                {
                    // Tries to get a previously seen vendor
                    vendor.mindState.wantsToTradeWithColony = true;
                    PawnComponentsUtility.AddAndRemoveDynamicComponents(vendor, true);

                    outPawns.Add(vendor);
                }
                else
                {
                    // Generate new vendor
                    GenerateVendor(parms, groupMaker, traderKind, outPawns);
                }

                // Generate wares
                ItemCollectionGeneratorParams waresParms = default(ItemCollectionGeneratorParams);
                waresParms.traderDef = traderKind;
                waresParms.forTile = parms.tile;
                waresParms.forFaction = parms.faction;
                List<Thing> wares = ItemCollectionGeneratorDefOf.TraderStock.Worker.Generate(waresParms).InRandomOrder(null).ToList();

                // Spawn pawns that are for sale (if any)
                foreach (Pawn sellable in GetPawnsFromWares(parms, wares))
                    outPawns.Add(sellable);

                // Carriers are costless.
                GenerateCarriers(parms, groupMaker, wares, outPawns);
            }

            // Generate options
            GenerateGroup(parms, groupMaker.options, outPawns);
            // Generate guards
            GenerateGroup(parms, groupMaker.guards, outPawns);
            // Generate manager (costless)
            GenerateLeader(parms, outPawns);
        }


        /* Private Methods */


        private void GenerateVendor(PawnGroupMakerParms parms, PawnGroupMaker groupMaker, TraderKindDef traderKind, List<Pawn> outPawns)
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

            outPawns.Add(vendor);
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



        private void GenerateGroup(PawnGroupMakerParms parms, List<PawnGenOption> group, List<Pawn> outPawns)
        {
            if (!group.Any())
                return;

            foreach (PawnGenOption option in group)
            {
                // TODO: points scaling curve
                for (int i = 0; i < option.selectionWeight; i++)
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

                    if (option.kind.combatPower < parms.points)
                    {
                        Pawn pawn = PawnGenerator.GeneratePawn(request);
                        parms.points -= option.kind.combatPower;
                        outPawns.Add(pawn);
                    }
                }
            }
        }


        private void GenerateLeader(PawnGroupMakerParms parms, List<Pawn> outPawns)
        {
            if (parms.faction.leader != null)
                outPawns.Add(parms.faction.leader);
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
