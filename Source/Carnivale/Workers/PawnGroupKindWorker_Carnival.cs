using System.Collections.Generic;
using RimWorld;
using Verse;
using System.Linq;
using UnityEngine;
using System;

namespace Carnivale
{
    public class PawnGroupKindWorker_Carnival : PawnGroupKindWorker
    {
        private const int MaxCarnies = 15; // not including carriers, guards, manager

        private const int MaxVendors = 5;

        [Unsaved]
        private Predicate<Pawn> genderValidator = null;

        public override float MinPointsToGenerateAnything(PawnGroupMaker groupMaker)
        {
            // NOTE: Just figured out that this would never be used for the
            // Carnival GroupKind. Leaving it here in case I can use it elsewhere.

            return _DefOf.CarnyTrader.combatPower;
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
            genderValidator = null;

            // Validation steps
            if (!CanGenerateFrom(parms, groupMaker) || !ValidateTradersList(groupMaker) || !ValidateCarriers(groupMaker))
            {
                if (errorOnZeroResults)
                    Log.Error("Cannot generate carnival caravan for " + parms.faction + ". parms=" + parms);
                return;
            }
            // End validation steps


            // Restrict gender of entertainers if it is in the name
            if (parms.faction.Name.EndsWith("Boys"))
            {
                genderValidator = delegate (Pawn p)
                {
                    return p.Is(CarnivalRole.Entertainer, false) && p.gender == Gender.Male;
                };
            }
            else if (parms.faction.Name.EndsWith("Girls")
                     || parms.faction.Name.EndsWith("Gals"))
            {
                genderValidator = delegate (Pawn p)
                {
                    return p.Is(CarnivalRole.Entertainer, false) && p.gender == Gender.Female;
                };
            }


            // New approach

            // Spawn manager (costless)
            outPawns.Add(parms.faction.leader);

            // Generate vendors (first is costless)
            var allWares = new List<Thing>();
            int numCarnies = 0;
            int maxVendors = Mathf.Clamp(groupMaker.traders.First().selectionWeight, 1, MaxVendors);

            for (int i = 0; i < maxVendors; i++)
            {
                TraderKindDef traderKind = null;

                int t = i % 3;
                switch (t)
                {
                    case 0:
                        traderKind = _DefOf.Carn_Trader_Food;
                        break;
                    case 1:
                        traderKind = _DefOf.Carn_Trader_Surplus;
                        break;
                    case 2:
                        traderKind = _DefOf.Carn_Trader_Curios;
                        break;
                    default:
                        traderKind = _DefOf.Carn_Trader_Food;
                        Log.Error("PawnGroupKindWorker_Carnival reached a bad place in code.");
                        break;
                }

                // Subtracts points, first costless:
                var vendor = GenerateVendor(parms, groupMaker, traderKind, i > 0);
                if (vendor != null)
                {
                    outPawns.Add(vendor);
                    numCarnies++;
                }
                else
                {
                    break;
                }
                

                // Generate wares
                var waresParms = default(ItemCollectionGeneratorParams);
                waresParms.traderDef = traderKind;
                waresParms.forTile = parms.tile;
                waresParms.forFaction = parms.faction;
                waresParms.validator = delegate (ThingDef def)
                {
                    if (def.stackLimit > 1)
                    {
                        return def.statBases.GetStatValueFromList(StatDefOf.Mass, 1f) * (def.stackLimit / 3f) < 68f;
                    }
                    else
                    {
                        return def.statBases.GetStatValueFromList(StatDefOf.Mass, 1f) < 68f;
                    }
                };

                allWares.AddRange(ItemCollectionGeneratorDefOf.TraderStock.Worker.Generate(waresParms));

                // Generate guards for each trader
                foreach (var guard in GenerateGroup(parms, groupMaker.guards, i > 0))
                {
                    outPawns.Add(guard);
                }

                // Generate carnies for each trader
                foreach (var carny in GenerateGroup(parms, groupMaker.options, i > 0))
                {
                    if (numCarnies++ > MaxCarnies) break;
                    outPawns.Add(carny);
                }
            }

            // Spawn pawns that are for sale (if any)
            foreach (Pawn sellable in GetPawnsFromWares(parms, allWares))
                outPawns.Add(sellable);

            // Generate carriers
            outPawns.AddRange(
                GenerateCarriers(parms, groupMaker, allWares)       // carriers w/ traders' wares
                .Concat(GenerateCarriers(parms, groupMaker, null))  // carrier w/ 100 silver
            );

        }


        /* Private Methods */


        private Pawn GenerateVendor(PawnGroupMakerParms parms, PawnGroupMaker groupMaker, TraderKindDef traderKind,  bool subtractPoints)
        {
            if (subtractPoints)
            {
                if (parms.points < _DefOf.CarnyTrader.combatPower * 2)
                    return null;
                else
                    parms.points -= _DefOf.CarnyTrader.combatPower * 2;
            }

            // Generate new vendor
            PawnGenerationRequest request = new PawnGenerationRequest(
                _DefOf.CarnyTrader,
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

            return vendor;
        }



        private IEnumerable<Pawn> GenerateCarriers(PawnGroupMakerParms parms, PawnGroupMaker groupMaker, List<Thing> wares)
        {
            var carrierList = new List<Pawn>();

            var carrierKind = (from x in groupMaker.carriers
                               where parms.tile == -1
                                     || Find.WorldGrid[parms.tile].biome.IsPackAnimalAllowed(x.kind.race)
                               select x).RandomElementByWeight(o => o.selectionWeight).kind;

            var waresSansPawns = new List<Thing>();
            var numCarriers = 1;

            if (!wares.NullOrEmpty())
            {
                var baseCapacity = carrierKind.RaceProps.baseBodySize * 34f; // Leaving some space for silvah, original calculation is 35f
                var totalWeight = 0f;

                for (int j = wares.Count - 1; j > -1; j--)
                {
                    var thing = wares[j];
                    if (thing is Pawn) continue;

                    var mass = thing.Mass();

                    if (thing.stackCount == 1 && mass > baseCapacity)
                    {
                        if (Prefs.DevMode)
                        {
                            Log.Warning("[Carnivale] "
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

                    if (thing.def.stackLimit > 1)
                    {
                        while (thing.stackCount >= 2 && mass > baseCapacity)
                        {
                            thing.stackCount /= 2;
                            mass = thing.Mass();

                            if (Prefs.DevMode)
                                Log.Message("\t[Carnivale] " + thing.LabelShort + " was to heavy for any carrier. Reducing its stack count to " + thing.stackCount + " and trying again.");
                        }

                        if (mass > baseCapacity)
                        {
                            if (Prefs.DevMode)
                            {
                                Log.Warning("[Carnivale] "
                                    + thing.LabelShort
                                    + " is too heavy for any carrier and will be removed from wares. mass="
                                    + mass
                                    + ", stackCount="
                                    + thing.stackCount
                                    + ", carrierKind="
                                    + carrierKind.label
                                    + ", capacity="
                                    + baseCapacity
                                );
                            }
                            wares.RemoveAt(j);
                            continue;
                        }
                    }

                    totalWeight += mass;
                    waresSansPawns.Add(thing);
                }

                numCarriers = Mathf.CeilToInt(totalWeight / baseCapacity);
            }
            else
            {
                var silver = ThingMaker.MakeThing(ThingDefOf.Silver);
                silver.stackCount = 100;
                waresSansPawns.Add(silver);
            }

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
                yield return carrier;
            }

            // Finally, fill up all the carriers' inventories
            while (i < waresSansPawns.Count)
            {
                var thing = waresSansPawns[i];
                var mass = thing.Mass();

                var carrier = carrierList.MaxBy(c => MassUtility.FreeSpace(c));
                if (thing.stackCount > 1 && !carrier.HasSpaceFor(thing))
                {
                    while (thing.stackCount >= 2 && mass > MassUtility.FreeSpace(carrier))
                    {
                        thing.stackCount /= 2;
                        mass = thing.Mass();

                        if (Prefs.DevMode)
                            Log.Message("\t[Carnivale] " + thing.LabelShort + " was to heavy for any carrier. Reducing its stack count to " + thing.stackCount + " and trying again.");
                    }
                }

                if (carrier.inventory.innerContainer.TryAdd(thing))
                {
                    i++;
                }
                else
                {
                    if (Prefs.DevMode)
                    {
                        Log.Warning("[Carnivale] "
                            + thing.LabelShort
                            + " is too heavy for any carrier and will be removed from wares. mass="
                            + mass
                            + ", stackCount="
                            + thing.stackCount
                            + ", carrierKind="
                            + carrierKind.label
                            + ", freeSpace="
                            + MassUtility.FreeSpace(carrier)
                        );
                    }

                    wares.RemoveAt(i);
                }
            }

            if (i == waresSansPawns.Count)
                yield break;

            var remainingMass = waresSansPawns.Sum(w => w.Mass());
            var remainingFreeSpace = carrierList.Sum(c => MassUtility.FreeSpace(c));

            if (Prefs.DevMode)
                Log.Warning("[Carnivale] Could not fit all wares in carriers. remainingMass=" + remainingMass + ", remainingFreeSpace=" + remainingFreeSpace);

            while (i < waresSansPawns.Count)
            {
                var thing = waresSansPawns[i];

                // Remove things that could not fit for whatever reason
                if (Prefs.DevMode)
                {
                    Log.Warning("\t[Carnivale] removing " + thing);
                }
                wares.Remove(waresSansPawns[i]);

                i++;
            }
        }



        private IEnumerable<Pawn> GenerateGroup(PawnGroupMakerParms parms, List<PawnGenOption> options, bool subtractPoints)
        {
            int counter = 0;
            int maxIterations = options.Max(o => o.selectionWeight);
            // traverses the list of options so at least 1 of each is generated before looping
            while (counter < maxIterations)
            {
                foreach (var option in options)
                {
                    if (counter < option.selectionWeight)
                    {
                        if (subtractPoints)
                        {
                            if (option.Cost > parms.points)
                                continue;
                            else
                                parms.points -= option.Cost;
                        }


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
                            option.kind == _DefOf.CarnyGuard, // must be capable of violence
                            1f,
                            true, // Force free warm layers if needed
                            true,
                            true,
                            parms.inhabitants,
                            false,
                            delegate (Pawn p)
                            {
                                if (p.Is(CarnivalRole.Worker, false))
                                {
                                    return !p.story.WorkTypeIsDisabled(WorkTypeDefOf.Construction);
                                }
                                else if (p.Is(CarnivalRole.Guard, false))
                                {
                                    var shoot = p.skills.GetSkill(SkillDefOf.Shooting).Level;
                                    return shoot > 4;
                                }
                                else if (genderValidator != null)
                                {
                                    return genderValidator(p);
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

                        yield return pawn;
                    }
                }

                counter++;
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
                        p.SetFaction(parms.faction);
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
