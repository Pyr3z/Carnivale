using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Carnivale
{
    public class Building_Tent : Building
    {
        private List<Building> walls = new List<Building>();

        private Building door;

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
                // Q: Do tent function instead?
                return this.Faction.Color;
            }
        }




        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            Log.Warning(OccupiedRect.ToString());

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

            door = ThingMaker.MakeThing(_DefOf.Invisible_Door) as Building;
            door.SetFaction(this.Faction);
            GenSpawn.Spawn(door, doorCell, map);

            // Build invisible walls
            IEnumerable<IntVec3> edges = OccupiedRect.EdgeCells;
            foreach (var cell in edges)
            {
                if (cell == doorCell) continue;
                Building wall = ThingMaker.MakeThing(_DefOf.Invisible_Wall) as Building;
                wall.SetFaction(this.Faction);
                GenSpawn.Spawn(wall, cell, map);
                this.walls.Add(wall);
            }

            
            

        }

        public override void DeSpawn()
        {
            Utilities.SetRoofFor(OccupiedRect, this.Map, null);
            door.Destroy();
            foreach (var wall in walls)
            {
                wall.Destroy();
            }
            door = null;
            walls = null;

            base.DeSpawn();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.walls, "walls", LookMode.Reference);
            Scribe_References.Look(ref this.door, "door", false);
        }
    }
}
