using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Carnivale
{
    public class CarnivalInfo : MapComponent, ILoadReferenceable
    {
        private const int RADIUS_MIN = 25;

        private const int RADIUS_MAX = 50;


        public Lord currentLord;

        public IntVec3 setupCentre;

        public float baseRadius;

        public CellRect carnivalArea;

        public IntVec3 bannerCell;

        public Dictionary<CarnivalRole, DeepReferenceableList<Pawn>> pawnsWithRole = new Dictionary<CarnivalRole, DeepReferenceableList<Pawn>>();

        public Dictionary<Pawn, IntVec3> rememberedPositions = new Dictionary<Pawn, IntVec3>();

        [Unsaved]
        List<Pawn> pawnWorkingList = null;
        [Unsaved]
        List<IntVec3> vec3WorkingList = null;

        public CarnivalInfo(Map map) : base(map)
        {

        }

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                // Clean up unusable elements in collections

                foreach (var list in pawnsWithRole.Values)
                {
                    foreach (var pawn in list)
                    {
                        if (pawn.DestroyedOrNull() || !pawn.Spawned || pawn.Dead)
                        {
                            list.Remove(pawn);
                        }
                    }
                }

                foreach (var pawn in rememberedPositions.Keys)
                {
                    if (pawn.DestroyedOrNull() || !pawn.Spawned || pawn.Dead)
                    {
                        rememberedPositions.Remove(pawn);
                    }
                }
            }

            Scribe_References.Look(ref this.currentLord, "currentLord");

            Scribe_Values.Look(ref this.setupCentre, "setupCentre", default(IntVec3), false);

            Scribe_Values.Look(ref this.baseRadius, "baseRadius", 0f, false);

            Scribe_Values.Look(ref this.carnivalArea, "carnivalArea", default(CellRect), false);

            Scribe_Values.Look(ref this.bannerCell, "bannerCell", default(IntVec3), false);

            Scribe_Collections.Look(ref this.pawnsWithRole, "pawnsWithRoles", LookMode.Value, LookMode.Deep);

            Scribe_Collections.Look(ref this.rememberedPositions, "rememberedPositions", LookMode.Reference, LookMode.Value, ref pawnWorkingList, ref vec3WorkingList);
        }



        public string GetUniqueLoadID()
        {
            return "CarnivalInfo_" + map.uniqueID;
        }



        public CarnivalInfo ReInitWith(Lord lord, IntVec3 centre)
        {
            this.currentLord = lord;
            this.setupCentre = centre;

            // Set radius for carnies to stick to
            baseRadius = Mathf.InverseLerp(RADIUS_MIN, RADIUS_MAX, currentLord.ownedPawns.Count / RADIUS_MAX);
            baseRadius = Mathf.Clamp(baseRadius, RADIUS_MIN, RADIUS_MAX);

            // Set carnival area
            carnivalArea = CellRect.CenteredOn(setupCentre, (int)baseRadius).ClipInsideMap(map).ContractedBy(10);

            // Set banner spot
            bannerCell = CalculateBannerCell();

            // Moved to LordToil_SetupCarnival.. dunno why but it doesn't work here
            //foreach (CarnivalRole role in Enum.GetValues(typeof(CarnivalRole)))
            //{
            //    List<Pawn> pawns = (from p in currentLord.ownedPawns
            //                        where p.Is(role)
            //                        select p).ToList();
            //    pawnsWithRole.Add(role, pawns);
            //}

            // The rest is assigned as the carnival goes along

            return this;
        }


        

        private IntVec3 CalculateBannerCell()
        {
            IntVec3 colonistPos = map.listerBuildings.allBuildingsColonist.NullOrEmpty() ?
                map.mapPawns.FreeColonistsSpawned.RandomElement().Position : map.listerBuildings.allBuildingsColonist.RandomElement().Position;

            IntVec3 bannerCell = carnivalArea.ClosestCellTo(colonistPos);

            if (map.roadInfo.roadEdgeTiles.Any())
            {
                // Prefer to place banner on nearest road
                CellRect searchArea = CellRect.CenteredOn(bannerCell, 75).ClipInsideMap(map).ContractedBy(10);
                float distance = 999999;
                IntVec3 roadCell = IntVec3.Invalid;

                foreach (var cell in searchArea)
                {
                    if (cell.GetTerrain(map).HasTag("Road") && !cell.InNoBuildEdgeArea(map))
                    {
                        float tempDist = bannerCell.DistanceTo(cell);
                        if (tempDist < distance)
                        {
                            distance = tempDist;
                            roadCell = cell;
                        }
                    }
                }

                if (roadCell.IsValid)
                {
                    // Found the edge of a road, try to centre it
                    IntVec3 centredCell = roadCell + IntVec3.East * 2;
                    if (centredCell.GetTerrain(map).HasTag("Road") && !centredCell.InNoBuildEdgeArea(map))
                    {
                        roadCell = centredCell;
                    }
                    else
                    {
                        roadCell += IntVec3.West * 2;
                    }

                    bannerCell = roadCell;
                }
            }

            return bannerCell;
        }
    }
}
