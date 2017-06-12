using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Carnivale
{
    public class Building_Tent : Building_Carn
    {
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad); // constructs interior things

            // Build roof
            Utilities.SetRoofFor(base.OccupiedRect, map, _DefOf.Carn_TentRoof);

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

            Building door = ThingMaker.MakeThing(_DefOf.Carn_TentDoor) as Building;
            door.SetFaction(this.Faction);
            GenSpawn.Spawn(door, doorCell, map);
            childBuildings.Add(door);

            // Build invisible walls
            IEnumerable<IntVec3> edges = Utilities.CornerlessEdgeCells(OccupiedRect);
            foreach (var cell in edges)
            {
                if (cell == doorCell) continue;
                Building wall = ThingMaker.MakeThing(_DefOf.Carn_TentWall) as Building;
                wall.SetFaction(this.Faction);
                GenSpawn.Spawn(wall, cell, map);
                childBuildings.Add(wall);
            }
        }


        public override void DeSpawn()
        {
            Utilities.SetRoofFor(OccupiedRect, this.Map, null);

            base.DeSpawn();
        }


    }
}
