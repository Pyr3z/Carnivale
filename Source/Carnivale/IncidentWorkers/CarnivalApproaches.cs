using Carnivale.Defs;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Carnivale.IncidentWorkers
{
    public class CarnivalApproaches : IncidentWorker_PawnsArrive
    {
        public override float AdjustedChance
        {
            get
            {
                // TODO: Adjust based off proximity to roads
                return base.AdjustedChance;
            }
        }


        protected virtual PawnGroupKindDef PawnGroupKindDef
        { get { return _PawnGroupKindDefOf.Carnival; } }


        protected override bool FactionCanBeGroupSource(Faction f, Map map, bool desperate = false)
        {
            return f.IsCarnival() &&
                base.FactionCanBeGroupSource(f, map, desperate);
        }


        public override bool TryExecute(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            IntVec3 spawnSpot;
            int durationDays = Mathf.RoundToInt(this.def.durationDays.RandomInRange);

            // Attempt to find a spawn spot.
            if (!CellFinder.TryFindRandomEdgeCellWith(
                (IntVec3 c) => map.reachability.CanReachColony(c),
                map,
                CellFinder.EdgeRoadChance_Always,
                out spawnSpot))
            {
                if (CarnivaleMod.debugLog)
                    Log.Message("Tried to execute incident CarnivalApproaches, failed to find reachable spawn spot.");
                return false;
            }

            Faction faction = parms.faction;

            // Main dialog node
            string title = "CarnivalApproachesTitle".Translate(faction.Name);
            DiaNode initialNode = new DiaNode("CarnivalApproachesInitial".Translate(new object[]
            {
                faction.Name,
                durationDays,
                map.info.parent.Label
            }));

            // Accept button
            DiaOption acceptOption = new DiaOption("CarnivalApproachesAccept".Translate());
            //acceptOption.action = delegate { };
            acceptOption.resolveTree = true;
            initialNode.options.Add(acceptOption);

            // Accept thank you message
            DiaNode acceptedNode = new DiaNode("CarnivalApproachesAcceptMessage".Translate(new object[]
            {
                faction.leader.Name.ToStringFull,
                faction.Name
            }));

            // Reject button
            DiaOption rejectOption = new DiaOption("CarnivalApproachesReject".Translate());
            //rejectOption.action = delegate { };
            rejectOption.resolveTree = true;
            initialNode.options.Add(rejectOption);

            // Draw dialog
            Find.WindowStack.Add(new Dialog_NodeTree(initialNode, true, true, title));

            return true;
        }
    }
}
