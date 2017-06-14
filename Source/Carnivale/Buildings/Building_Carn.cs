﻿using RimWorld;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace Carnivale
{
    public class Building_Carn : Building
    {
        [Unsaved]
        private IntVec3 oldPosition = IntVec3.Invalid;

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


        public override string GetInspectString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(base.GetInspectString());

            sb.AppendLine("Owner".Translate() + ": " + factionInt.Name);

            return sb.ToString().TrimEndNewlines();
        }


        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
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

            this.oldPosition = this.Position;
        }


        public override void DeSpawn()
        {
            foreach (var child in childBuildings)
            {
                if (child.Destroyed)
                {
                    childBuildings.Remove(child);
                    continue;
                }
                child.DeSpawn();
            }

            base.DeSpawn();
        }



        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode != LoadSaveMode.Inactive)
            {
                this.childBuildings.RemoveAll(c => c == null || c.Destroyed);
            }

            Scribe_Collections.Look(ref this.childBuildings, "childBuildings", LookMode.Reference);
        }
    }
}