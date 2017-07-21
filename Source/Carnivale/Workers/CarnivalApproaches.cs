using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;
using Xnope;

namespace Carnivale
{
    public class IncidentWorker_CarnivalApproaches : IncidentWorker
    {
        private const int acceptanceBonus = 5;
        private const int rejectionPenalty = -10;

        public override float AdjustedChance
        {
            get
            {
                float baseChance = base.AdjustedChance;
                return Find.VisibleMap.roadInfo.roadEdgeTiles.Any() ? baseChance * 1.5f : baseChance;
            }
        }


        protected override bool CanFireNowSub(IIncidentTarget target)
        {
            Map map = (Map)target;
            if (map.GetComponent<CarnivalInfo>().Active)
            {
                // only one carnival per map
                return false;
            }

            // check incompatible game conditions
            foreach (var condition in map.GameConditionManager.ActiveConditions)
            {
                if (condition.def == GameConditionDefOf.PsychicSoothe
                    || condition.def == GameConditionDefOf.ToxicFallout)
                {
                    return false;
                }
            }

            return true;
        }


        public override bool TryExecute(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            IntVec3 spawnSpot;
            int durationDays = Mathf.RoundToInt(this.def.durationDays.RandomInRange);

            if (Prefs.DevMode)
                Log.Message("[Carnivale] Calculating spawn centre:");

            if (!CarnivalUtils.FindCarnivalSpawnSpot(map, out spawnSpot))
            {
                if (Prefs.DevMode)
                    Log.Warning("[Carnivale] Tried to execute incident CarnivalApproaches, failed to find reachable spawn spot.");
                return false;
            }

            if (!FindCarnivalFaction(out parms.faction))
            {
                if (Prefs.DevMode)
                    Log.Warning("[Carnivale] Tried to execute incident CarnivalApproaches, failed to find valid faction.");
                return false;
            }

            int feePerColonist = CarnivalUtils.CalculateFeePerColonist(parms.points);

            // Main dialog node
            string title = "CarnivalApproachesTitle".Translate(parms.faction.Name);
            DiaNode initialNode = new DiaNode("CarnivalApproachesInitial".Translate(new object[]
            {
                parms.faction.Name,
                durationDays,
                feePerColonist + " " + ThingDefOf.Silver.label,
                map.info.parent.Label == "Colony" ? "your colony" : map.info.parent.Label
            }));

            // Accept button
            DiaOption acceptOption = new DiaOption("CarnivalApproachesAccept".Translate());
            acceptOption.action = delegate
            {
                // Do accept action
                parms.faction.AffectGoodwillWith(Faction.OfPlayer, acceptanceBonus);

                IncidentParms arrivalParms = StorytellerUtility.DefaultParmsNow(Find.Storyteller.def, IncidentCategory.AllyArrival, map);
                //arrivalParms.forced = true; // forcing not necessary
                arrivalParms.faction = parms.faction;
                arrivalParms.spawnCenter = spawnSpot;
                arrivalParms.points = parms.points; // Do this?

                // This is so it can be determined that the spawnpoint was precomputed.
                Rot4 sneakyValueForSpawnpointResolved = Rot4.East;
                arrivalParms.spawnRotation = sneakyValueForSpawnpointResolved;

                // This is super cheaty, but there is no other field to pass this to.
                arrivalParms.raidPodOpenDelay = durationDays;
                // End cheaty.

                // Assign fee per colonist to CarnivalInfo
                map.GetComponent<CarnivalInfo>().feePerColonist = -feePerColonist;

                QueuedIncident qi = new QueuedIncident(new FiringIncident(_DefOf.CarnivalArrives, null, arrivalParms), Find.TickManager.TicksGame + GenDate.TicksPerDay);
                Find.Storyteller.incidentQueue.Add(qi);
            };
            initialNode.options.Add(acceptOption);

            // Accept thank you message
            DiaNode acceptedMessage = new DiaNode("CarnivalApproachesAcceptMessage".Translate(new object[]
            {
                parms.faction.leader.Name.ToStringFull
            }));
            DiaOption ok = new DiaOption("OK".Translate());
            ok.resolveTree = true;
            acceptedMessage.options.Add(ok);
            acceptOption.link = acceptedMessage;

            // Reject button
            DiaOption rejectOption = new DiaOption("CarnivalApproachesReject".Translate());
            rejectOption.action = delegate
            {
                // Do reject action
                parms.faction.AffectGoodwillWith(Faction.OfPlayer, rejectionPenalty);
            };
            initialNode.options.Add(rejectOption);

            // Reject fuck you message (TODO: randomise response)
            DiaNode rejectedMessage = new DiaNode("CarnivalApproachesRejectMessage".Translate(new object[]
            {
                parms.faction.leader.Name.ToStringShort
            }));
            DiaOption hangup = new DiaOption("HangUp".Translate());
            hangup.resolveTree = true;
            rejectedMessage.options.Add(hangup);
            rejectOption.link = rejectedMessage;

            // Draw dialog
            Find.WindowStack.Add(new Dialog_NodeTree(initialNode, true, true, title));

            return true;
        }


        private static bool FindCarnivalFaction(out Faction faction)
        {
            Faction fac = null;
            Find.FactionManager.AllFactionsListForReading
                .Where(f => f.IsCarnival() && !f.HostileTo(Faction.OfPlayer))
                .TryRandomElementByWeight((Faction f) => f.PlayerGoodwill, out fac);

            faction = fac;

            if (faction == null) return false;
            return true;
        }

    }
}
