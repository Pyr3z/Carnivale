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
            // Build roof
            foreach (var cell in this.OccupiedRect())
            {
                map.roofGrid.SetRoof(cell, _DefOf.Carn_TentRoof);
            }


            if (!childBuildings.Any(b => b.def == _DefOf.Carn_TentDoor))
            {
                // Initial spawn.
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

                Building_TentFlap door = ThingMaker.MakeThing(_DefOf.Carn_TentDoor) as Building_TentFlap;
                door.SetFaction(this.Faction);
                door.Position = doorCell;

                if (Props.type.Is(CarnBuildingType.Attraction))
                    door.everAvailableToNonCarnies = true;

                //GenSpawn.Spawn(door, doorCell, map); // spawned in base method now
                childBuildings.Add(door); 

                // Build invisible walls
                IEnumerable<IntVec3> edges = Utilities.CornerlessEdgeCells(this.OccupiedRect());
                foreach (var cell in edges)
                {
                    if (cell == doorCell) continue;
                    Building wall = ThingMaker.MakeThing(_DefOf.Carn_TentWall) as Building;
                    wall.SetFaction(this.Faction);
                    wall.Position = cell;

                    //GenSpawn.Spawn(wall, cell, map); // spawned in base method now
                    childBuildings.Add(wall);
                }
            }

            // constructs interior things and spawns everything:
            base.SpawnSetup(map, respawningAfterLoad);
        }


        public override void DeSpawn()
        {
            foreach (var cell in this.OccupiedRect())
            {
                Map.roofGrid.SetRoof(cell, null);
            }

            base.DeSpawn();
        }


    }
}
