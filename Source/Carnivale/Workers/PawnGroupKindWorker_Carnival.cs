using System.Collections.Generic;
using RimWorld;
using Verse;
using System.Linq;
using UnityEngine;

namespace Carnivale
{
    public class PawnGroupKindWorker_Carnival : PawnGroupKindWorker
    {
        private const int MaxCarnies = 25; // not including carriers

        private const int MaxVendors = 3;

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
                    Log.Error("Cannot generate carnival caravan for " + parms.faction + ". parms=" + parms);
                return;
            }
            // End validation steps

            // New approach
            //int numCarnies = 0;
            //int numVendors = 0;
            //while (parms.points > 1 && numCarnies < MaxCarnies)
            //{

            //}
            // End new approach

            // Old approach
            // Generate vendors (first one is costless)
            for (int i = 0;  i < groupMaker.traders.First().selectionWeight; i++)
            {
                // Get a traderkind by commonality:
                TraderKindDef traderKind = parms.faction.def.caravanTraderKinds.RandomElementByWeight(k => k.commonality);

                // Generate vendor
                if (i == 0) parms.points += _DefOf.CarnyTrader.combatPower;
                else if (parms.points < _DefOf.CarnyTrader.combatPower) break;

                GenerateVendor(parms, groupMaker, traderKind, outPawns, true);

                // Generate wares
                ItemCollectionGeneratorParams waresParms = default(ItemCollectionGeneratorParams);
                waresParms.traderDef = traderKind;
                waresParms.forTile = parms.tile;
                waresParms.forFaction = parms.faction;
                waresParms.validator = delegate (ThingDef def)
                {
                    if (def.stackLimit == 1)
                    {
                        return def.statBases.GetStatValueFromList(StatDefOf.Mass, 0f) < 70f;
                    }

                    return true;
                };
                List<Thing> wares = ItemCollectionGeneratorDefOf.TraderStock.Worker.Generate(waresParms).InRandomOrder(null).ToList();

                // Spawn pawns that are for sale (if any)
                foreach (Pawn sellable in GetPawnsFromWares(parms, wares))
                    outPawns.Add(sellable);

                // Carriers are costless.
                GenerateCarriers(parms, groupMaker, wares, outPawns);
            }

            // Generate one extra carrier carrying nothing
            GenerateCarriers(parms, groupMaker, new List<Thing>(), outPawns);

            // Generate options
            GenerateGroup(parms, groupMaker.options, outPawns, true);

            // Generate guards (first one is costless)
            parms.points += _DefOf.CarnyGuard.combatPower;
            GenerateGroup(parms, groupMaker.guards, outPawns, true);

            // Generate manager (costless)
            GenerateLeader(parms, outPawns);

        }


        /* Private Methods */


        private void GenerateVendor(PawnGroupMakerParms parms, PawnGroupMaker groupMaker, TraderKindDef traderKind, List<Pawn> outPawns, bool subtractPoints = false)
        {
            if (subtractPoints)
                if (parms.points < _DefOf.CarnyTrader.combatPower)
                    return;
                else
                    parms.points -= _DefOf.CarnyTrader.combatPower;

            // Generate new vendor
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

            var vendor = PawnGenerator.GeneratePawn(request);

            vendor.mindState.wantsToTradeWithColony = true;

            PawnComponentsUtility.AddAndRemoveDynamicComponents(vendor, true);
            vendor.trader.traderKind = traderKind;

            outPawns.Add(vendor);
        }



        private void GenerateCarriers(PawnGroupMakerParms parms, PawnGroupMaker groupMaker, List<Thing> wares, List<Pawn> outPawns)
        {
            // disabling this more elegant solution in order to exclude overweight items
            //var waresList = (from x in wares
            //                 where !(x is Pawn)
            //                 select x).ToList();
            //var totalWeight = waresList.Sum(t => t.GetInnerIfMinified().GetStatValue(StatDefOf.Mass) * t.stackCount);

            var carrierList = new List<Pawn>();

            var carrierKind = (from x in groupMaker.carriers
                               where parms.tile == -1
                                     || Find.WorldGrid[parms.tile].biome.IsPackAnimalAllowed(x.kind.race)
                               select x).RandomElementByWeight(o => o.selectionWeight).kind;
            float baseCapacity = carrierKind.RaceProps.baseBodySize * 34f; // Leaving some space for silvah, original calculation is 35f

            var totalWeight = 0f;
            var waresSansPawns = new List<Thing>();

            for (int j = wares.Count - 1; j > -1; j--) // required for iterative removing
            {
                var thing = wares[j];
                if (thing is Pawn) continue;

                var mass = thing.Mass();

                if (mass > baseCapacity)
                {
                    if (Prefs.DevMode)
                    {
                        Log.Warning("[Debug] "
                            + thing
                            + " is too big for any carrier and will be removed from wares. mass="
                            + mass
                            + ", "
                            + carrierKind.label
                            + " capacity="
                            + baseCapacity
                        );
                    }
                    wares.RemoveAt(j);
                    continue;
                }

                totalWeight += mass;
                waresSansPawns.Add(thing);
            }

            int numCarriers = Mathf.CeilToInt(totalWeight / baseCapacity);

            int i = 0;
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
                var carrier = PawnGenerator.GeneratePawn(request);
                if (i < waresSansPawns.Count)
                {
                    // Add initial few items to carrier
                    if (carrier.inventory.innerContainer.TryAdd(waresSansPawns[i], true))
                        i++;
                }
                carrierList.Add(carrier);
                outPawns.Add(carrier);
            }

            // Finally, fill up all the carriers' inventories
            int numFailures = 0;
            while (i < waresSansPawns.Count && numFailures < 15)
            {
                var ware = waresSansPawns[i];
                Pawn randCarrier;
                if (carrierList.TryRandomElementByWeight(
                    c => c.FreeSpaceIfCarried(ware),
                    out randCarrier)
                    && randCarrier.inventory.innerContainer.TryAdd(ware))
                {
                    i++;
                }
                else
                    numFailures++;
            }

            while (i < waresSansPawns.Count)
            {
                // Remove things that could not fit for whatever reason
                if (Prefs.DevMode)
                    Log.Warning("[Debug] Could not fit " + waresSansPawns[i] + " in any carrier. Removing.");
                wares.Remove(waresSansPawns[i++]);
            }
        }



        private void GenerateGroup(PawnGroupMakerParms parms, List<PawnGenOption> options, List<Pawn> outPawns, bool subtractPoints = false)
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
                                delegate (Pawn p)
                                {
                                    if (p.kindDef == _DefOf.CarnyWorker)
                                    {
                                        return !p.story.WorkTypeIsDisabled(WorkTypeDefOf.Construction);
                                    }

                                    return true;
                                },
                                null,
                                null,
                                null,
                                null,
                                null
                            );

                        var pawn = PawnGenerator.GeneratePawn(request);

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
