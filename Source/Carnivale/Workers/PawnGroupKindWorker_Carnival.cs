using System.Collections.Generic;
using RimWorld;
using Verse;
using System.Linq;
using UnityEngine;

namespace Carnivale
{
    public class PawnGroupKindWorker_Carnival : PawnGroupKindWorker
    {

        public override float MinPointsToGenerateAnything(PawnGroupMaker groupMaker)
        {
            // NOTE: Just figured out that this would never be used for the
            // Carnival GroupKind. Leaving it here in case I can use it elsewhere.

            return groupMaker.options.Min(o => o.Cost);
        }



        public override bool CanGenerateFrom(PawnGroupMakerParms parms, PawnGroupMaker groupMaker)
        {
            return groupMaker.kindDef == _DefOf.Carnival &&
                parms.faction.IsCarnival() &&
                MinPointsToGenerateAnything(groupMaker) < parms.points &&
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

            if (existingPawns == null)
                existingPawns = new HashSet<Pawn>();


            // Generate vendors (first one is costless)
            for (int i = 0;  i < groupMaker.traders.First().selectionWeight; i++)
            {
                // Get a traderkind by commonality:
                TraderKindDef traderKind = parms.faction.def.caravanTraderKinds.RandomElementByWeight(k => k.commonality);

                // Generate vendor
                if (i == 0) parms.points += _DefOf.CarnyTrader.combatPower;
                else if (parms.points < _DefOf.CarnyTrader.combatPower) break;

                GenerateVendor(parms, groupMaker, traderKind, outPawns, existingPawns, true);

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
            GenerateGroup(parms, groupMaker.options, outPawns, existingPawns, true);

            // Generate guards (first one is costless)
            parms.points += _DefOf.CarnyGuard.combatPower;
            GenerateGroup(parms, groupMaker.guards, outPawns, existingPawns, true);

            // Generate manager (costless)
            GenerateLeader(parms, outPawns);

            // Supply them with tents
            // Actually no don't do this here
            //List<Pawn> builders = (from p in outPawns
            //                      where p.Is(CarnivalRole.Worker)
            //                      select p).ToList();
            //int numBedTents = outPawns.Count() > 9 ? Mathf.RoundToInt(outPawns.Count() / 8f) : 1;

            //for (int i = 0; i < numBedTents; i++)
            //{
            //    Thing newTentCrate = ThingMaker.MakeThing(_DefOf.Carn_Crate_TentMedFurn, GenStuff.RandomStuffFor(_DefOf.Carn_Crate_TentMedFurn));
            //    builders.RandomElement().inventory.TryAddItemNotForSale(newTentCrate);
            //}

        }


        /* Private Methods */


        private void GenerateVendor(PawnGroupMakerParms parms, PawnGroupMaker groupMaker, TraderKindDef traderKind, List<Pawn> outPawns, IEnumerable<Pawn> existingPawns, bool subtractPoints = false)
        {
            if (subtractPoints)
                if (parms.points < _DefOf.CarnyTrader.combatPower)
                    return;
                else
                    parms.points -= _DefOf.CarnyTrader.combatPower;

            // Tries to get existing vendor:
            Pawn vendor = existingPawns.FirstOrDefault(p => traderKind == p.TraderKind);

            if (vendor == null)
            {
                // Generate new vendor if no existing vendor
                PawnGenerationRequest request = new PawnGenerationRequest(
                    groupMaker.traders.RandomElement().kind,
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

                vendor = PawnGenerator.GeneratePawn(request);
            }

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
                                       select x).RandomElementByWeight(o => o.selectionWeight).kind;
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
                carrierList.RandomElement().inventory.innerContainer.TryAdd(waresList[i], true);
                i++;
            }
        }



        private void GenerateGroup(PawnGroupMakerParms parms, List<PawnGenOption> options, List<Pawn> outPawns, IEnumerable<Pawn> existingPawns, bool subtractPoints = false)
        {
            int counter = 0;
            while (counter < options.Max(o => o.selectionWeight))
            {
                foreach (var option in options)
                {
                    if (counter < option.selectionWeight)
                    {
                        if (subtractPoints)
                            if (option.Cost > parms.points)
                                continue;
                            else
                                parms.points -= option.Cost;


                        Pawn pawn = existingPawns.FirstOrDefault(p => p.kindDef == option.kind);
                        if (pawn == null)
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

                            pawn = PawnGenerator.GeneratePawn(request);
                        }

                        outPawns.Add(pawn);
                    }
                }

                counter++;
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
