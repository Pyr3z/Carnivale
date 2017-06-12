using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Carnivale
{
    public class Building_Carn : Building
    {
        protected List<Building> childBuildings = new List<Building>();

        [Unsaved]
        private CompProperties_CarnBuilding propsInt = null;

        public CompProperties_CarnBuilding Props
        {
            get
            {
                if (propsInt == null)
                {
                    propsInt = GetComp<CompCarnBuilding>().Props;
                }
                return propsInt;
            }
        }

        public CarnBuildingType Type
        {
            get
            {
                return Props.type;
            }
        }

        [Unsaved]
        private CellRect occupiedRectInt;

        public CellRect OccupiedRect
        {
            get
            {
                if (this.occupiedRectInt == default(CellRect))
                    occupiedRectInt = this.OccupiedRect();
                return occupiedRectInt;
            }
        }



        public override Color DrawColorTwo
        {
            get
            {
                // Draw flag colour as faction colour
                // Q: Colour by tent function instead?
                return this.Faction.Color;
            }
        }


        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            // Build interior
            if (Props.interiorThings.Any())
            {
                foreach (ThingPlacement tp in Props.interiorThings)
                {
                    foreach (IntVec3 offset in tp.placementOffsets)
                    {
                        IntVec3 cell = Position + offset.RotatedBy(Rotation);
                        ThingDef stuff = tp.thingDef.MadeFromStuff ? GenStuff.DefaultStuffFor(tp.thingDef) : null;

                        Building building = ThingMaker.MakeThing(tp.thingDef, stuff) as Building;
                        building.SetFaction(this.Faction);

                        // Give manager a specific bed:
                        if (this.Type.Is(CarnBuildingType.Bedroom | CarnBuildingType.ManagerOnly))
                        {
                            if (building is Building_Bed && this.Faction != Faction.OfPlayer)
                            {
                                this.Faction.leader.ownership.ClaimBedIfNonMedical((Building_Bed)building);
                            }
                        }

                        Utilities.SpawnThingNoWipe(building, cell, map, Rotation.Opposite, respawningAfterLoad);
                        childBuildings.Add(building);
                    }
                }
            }
        }


        public override void DeSpawn()
        {
            Utilities.SetRoofFor(OccupiedRect, this.Map, null);
            foreach (var child in childBuildings)
            {
                // Potential null-pointer if child is destroyed elsewhere?
                child.Destroy();
            }
            childBuildings.Clear();

            base.DeSpawn();
        }



        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                this.childBuildings.RemoveAll(c => c.Destroyed);
            }
            Scribe_Collections.Look(ref this.childBuildings, "childBuildings", LookMode.Reference);
        }
    }
}
