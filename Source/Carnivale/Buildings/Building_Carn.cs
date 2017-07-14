using RimWorld;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;
using Verse;

namespace Carnivale
{
    public class Building_Carn : Building
    {
        private IntVec3 oldPosition;

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



        public override Color DrawColorTwo
        {
            get
            {
                // Draw flag colour as faction colour
                // Q: Colour by tent function instead?
                return this.Faction.Color;
            }
        }


        public Building_Carn() { }


        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                this.childBuildings.RemoveAll(c => c == null || c.Destroyed);
            }

            Scribe_Values.Look(ref this.oldPosition, "oldPos");

            Scribe_Collections.Look(ref this.childBuildings, "childBuildings", LookMode.Reference);
        }


        public override string GetInspectString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(base.GetInspectString());

            sb.AppendLine("Owner".Translate() + ": " + factionInt.Name);

            return sb.ToString().TrimEndNewlines();
        }


        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            // Forbid things so colonists can access them after they are hauled to trash
            if (this.factionInt != Faction.OfPlayer)
            {
                foreach (var cell in this.OccupiedRect())
                {
                    foreach (var thing in cell.GetThingList(map).Where(t => t.def.EverHaulable))
                    {
                        thing.SetForbidden(true, false);
                    }
                }
            }

            // Build interior
            if (Props.interiorThings.Any() && !childBuildings.Any(b => Props.interiorThings.Any(t => t.thingDef == b.def)))
            {
                this.oldPosition = Position;

                foreach (ThingPlacement tp in Props.interiorThings)
                {
                    foreach (IntVec3 offset in tp.placementOffsets)
                    {
                        IntVec3 cell = Position + offset.RotatedBy(Rotation);
                        ThingDef stuff = tp.thingDef.MadeFromStuff ? GenStuff.DefaultStuffFor(tp.thingDef) : null;

                        Building building = ThingMaker.MakeThing(tp.thingDef, stuff) as Building;
                        building.SetFaction(this.Faction);
                        building.Position = cell;
                        building.Rotation = this.Rotation.Opposite;

                        // Give manager a specific bed:
                        if (this.Type.Is(CarnBuildingType.Bedroom | CarnBuildingType.ManagerOnly))
                        {
                            if (building is Building_Bed && this.Faction != Faction.OfPlayer)
                            {
                                this.Faction.leader.ownership.ClaimBedIfNonMedical((Building_Bed)building);
                            }
                        }

                        childBuildings.Add(building);

                        //Utilities.SpawnThingNoWipe(building, cell, map, Rotation.Opposite, respawningAfterLoad);
                    }
                }
            }

            foreach (var child in childBuildings)
            {
                // spawn here
                if (!child.Spawned)
                {
                    // offset is necessary for reinstallation
                    IntVec3 offset = child.Position - this.oldPosition;
                    child.Position = this.Position + offset;
                    Utilities.SpawnThingNoWipe(child, map, respawningAfterLoad);
                }
            }

            base.SpawnSetup(map, respawningAfterLoad);

            if (childBuildings.Any())
            {
                // Necessary for reinstalling properly
                this.oldPosition = this.Position;
            }
        }


        public override void DeSpawn()
        {
            for (int i = childBuildings.Count - 1; i > -1; i--)
            {
                if (childBuildings[i].Destroyed)
                {
                    childBuildings.RemoveAt(i);
                    continue;
                }

                childBuildings[i].DeSpawn();
            }

            base.DeSpawn();
        }


        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (mode == DestroyMode.Deconstruct)
            {
                var crateDef = this.def.costList[0].thingDef;
                var crate = ThingMaker.MakeThing(crateDef, Stuff);

                int hitPoints = (HitPoints / MaxHitPoints) * crate.MaxHitPoints;
                crate.HitPoints = hitPoints;

                if (this.factionInt != Faction.OfPlayer)
                {
                    crate.SetForbidden(true);
                }

                GenSpawn.Spawn(crate, Position, Map);
            }

            base.Destroy(mode);
        }


        public override void Tick()
        {
            base.Tick();

            childBuildings.RemoveAll(c => c.DestroyedOrNull() || !c.Spawned);
        }

    }
}
