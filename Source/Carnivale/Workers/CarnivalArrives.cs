using Carnivale.AI;
using Carnivale.Enums;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;

namespace Carnivale
{
    class IncidentWorker_CarnivalArrives : IncidentWorker_NeutralGroup
    {
        protected override PawnGroupKindDef PawnGroupKindDef
        { get { return _DefOf.Carnival; } }


        protected override bool CanFireNowSub(IIncidentTarget target)
        {
            // Always true, because this incident is manually fired by
            // another, which is where the ability for this to fire is
            // resolved.
            // ...Or should a double check be made? Case: toxic fallout etc
            return true;
        }


        protected override bool FactionCanBeGroupSource(Faction f, Map map, bool desperate = false)
        {
            return f.IsCarnival() &&
                base.FactionCanBeGroupSource(f, map, desperate);
        }


        protected List<Pawn> SpawnPawns(IncidentParms parms, int spawnPointSpread)
        {
            // Essentially a copy of the base method, however spawn spread is tweakable.
            Map map = (Map)parms.target;
            PawnGroupMakerParms defaultMakerParms = IncidentParmsUtility.GetDefaultPawnGroupMakerParms(parms);

            List<Pawn> list = PawnGroupMakerUtility.GeneratePawns(this.PawnGroupKindDef, defaultMakerParms, false).ToList();

            foreach (Pawn p in list)
            {
                IntVec3 spawnPoint = CellFinder.RandomClosewalkCellNear(parms.spawnCenter, map, spawnPointSpread, null);
                GenSpawn.Spawn(p, spawnPoint, map);
            }

            return list;
        }


        public override bool TryExecute(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            // Cheaty:
            int durationDays = parms.raidPodOpenDelay == 140 ? 3 : parms.raidPodOpenDelay;
            // End cheaty.

            // Resolve parms (currently counting on parent class to handle this)
            if (!base.TryResolveParms(parms))
            {
                if (Prefs.DevMode)
                    Log.Warning("Could not execute CarnivalArrives: the spawn point calculated yesterday is probably no longer valid.");
                return false;
            }

            // Spawn pawns. Counting on you, IncidentWorker_NeutralGroup.
            List<Pawn> pawns = this.SpawnPawns(parms, 17);
            if (pawns.Count < 3)
            {
                if (Prefs.DevMode)
                    Log.Warning("Could not execute CarnivalArrives: could not generate any valid pawns.");
                return false;
            }

            List<Pawn> vendors = new List<Pawn>();
            foreach (Pawn p in pawns)
            {
                if (p.TraderKind != null)
                    // Get list of vendors
                    vendors.Add(p);
                if (p.needs != null && p.needs.food != null)
                    // Also feed the carnies
                    p.needs.food.CurLevel = p.needs.food.MaxLevel;
            }

            string label = "LetterLabelCarnivalArrival".Translate(new object[] {
                parms.faction.Name
            });

            string text = "LetterCarnivalArrival".Translate(new object[] {
                parms.faction.Name,
                durationDays
            });

            if (vendors.Count > 0)
            {
                text += "CarnivalArrivalVendorsList".Translate();
                foreach (Pawn vendor in vendors)
                {
                    text += "\n  " + vendor.NameStringShort + ", " + vendor.TraderKind.label.CapitalizeFirst();
                }
            }

            PawnRelationUtility.Notify_PawnsSeenByPlayer(pawns, ref label, ref text, "LetterRelatedPawnsNeutralGroup".Translate(), true);
            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.Good, pawns[0], null);

            //IntVec3 setupSpot;
            //RCellFinder.TryFindRandomSpotJustOutsideColony(pawns[0], out setupSpot);
            IntVec3 setupSpot = Utilities.FindCarnivalSetupPositionFrom(parms.spawnCenter, map);

            //List<Pawn> workersWithCrates = new List<Pawn>();
            //List<Thing> availableCrates = new List<Thing>();

            //foreach (Pawn p in pawns)
            //{
            //    foreach (Thing crate in from c in p.inventory.innerContainer
            //                            where p.inventory.NotForSale(c)
            //                                && c.def.tradeTags.Contains("Carn_Crate")
            //                            select c)
            //    {
            //        workersWithCrates.Add(p);
            //        availableCrates.Add(crate);
            //    }
            //}


            LordJob_EntertainColony lordJob = new LordJob_EntertainColony(parms.faction, setupSpot, durationDays);
            LordMaker.MakeNewLord(parms.faction, lordJob, map, pawns);

            return true;
        }


        
    }
}
