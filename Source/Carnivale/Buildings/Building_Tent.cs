using System.Collections.Generic;
using System.Linq;
using Verse;
using Xnope;

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
                door.parent = this;
                door.Position = doorCell;

                if (Props.type.Is(CarnBuildingType.Attraction))
                    door.availableToNonCarnies = true;

                //GenSpawn.Spawn(door, doorCell, map); // spawned in base method now
                childBuildings.Add(door); 

                // Build invisible walls
                IEnumerable<IntVec3> edges = this.OccupiedRect().CornerlessEdgeCells();
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


        public Building_TentFlap GetTentFlap()
        {
            var flap = childBuildings.First(c => c is Building_TentFlap);
            return flap as Building_TentFlap;
        }
    }
}
