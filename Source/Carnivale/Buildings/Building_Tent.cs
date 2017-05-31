using Carnivale.Enums;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Carnivale
{
    public class Building_Tent : Building
    {
        private List<Building> childBuildings = new List<Building>();

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




        public override void SpawnSetup(Map map, bool respawnAfterLoad)
        {
            base.SpawnSetup(map, respawnAfterLoad);

            // Build roof
            Utilities.SetRoofFor(OccupiedRect, map, _DefOf.Carn_TentRoof);

            // Build invisible door
            IntVec3 doorCell = InteractionCell;
            if (Rotation == Rot4.North)
                doorCell.z += 1;
            else if (Rotation == Rot4.East)
                doorCell.x += 1;
            else if (Rotation == Rot4.West)
                doorCell.x -= 1;
            else
                doorCell.z -= 1;

            Building door = ThingMaker.MakeThing(_DefOf.Invisible_Door) as Building;
            door.SetFaction(this.Faction);
            GenSpawn.Spawn(door, doorCell, map);
            childBuildings.Add(door);

            // Build invisible walls
            IEnumerable<IntVec3> edges = Utilities.CornerlessEdgeCells(OccupiedRect);
            foreach (var cell in edges)
            {
                if (cell == doorCell) continue;
                Building wall = ThingMaker.MakeThing(_DefOf.Invisible_Wall) as Building;
                wall.SetFaction(this.Faction);
                GenSpawn.Spawn(wall, cell, map);
                childBuildings.Add(wall);
            }

            // Build interior
            BuildInterior(map, respawnAfterLoad);
        }

        public override void DeSpawn()
        {
            Utilities.SetRoofFor(OccupiedRect, this.Map, null);
            foreach (var child in childBuildings)
            {
                // Potential null-pointer if child is destroyed elsewhere?
                child.Destroy();
            }
            childBuildings = null;

            base.DeSpawn();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.childBuildings, "childBuildings", LookMode.Reference);
        }


        // Interior building methods

        private void BuildInterior(Map map, bool respawnAfterLoad)
        {
            foreach (ThingPlacement tp in Props.interiorThings)
            {
                foreach (IntVec3 offset in tp.placementOffsets)
                {
                    IntVec3 cell = Position + offset.RotatedBy(Rotation);
                    ThingDef stuff = tp.thingDef.MadeFromStuff ? GenStuff.DefaultStuffFor(tp.thingDef) : null;

                    Building building = ThingMaker.MakeThing(tp.thingDef, stuff) as Building;
                    building.SetFaction(this.Faction);

                    Utilities.SpawnThingNoWipe(building, cell, map, Rotation.Opposite, respawnAfterLoad);
                    childBuildings.Add(building);
                }
            }
        }

    }
}
