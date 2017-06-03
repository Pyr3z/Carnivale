using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Carnivale.AI
{
    public class LordToil_SetupCarnival : LordToil
    {
        private const float RADIUS_MIN = 25f;

        private const float RADIUS_MAX = 50f;

        public LordToilData_Carnival Data
        { get { return (LordToilData_Carnival)this.data; } }

        public override IntVec3 FlagLoc
        {
            get
            {
                return Data.setupSpot;
            }
        }

        private IEnumerable<Frame> Frames
        {
            get
            {
                List<Thing> framesList = Map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingFrame);
                foreach (var thing in framesList)
                {
                    Frame frame = thing as Frame;
                    if (frame.Faction == this.lord.faction)
                        yield return frame;
                }
            }
        }


        public LordToil_SetupCarnival(IntVec3 setupSpot)
        {
            LordToilData_Carnival dat = new LordToilData_Carnival(setupSpot);

            this.data = dat;
        }



        public override void Init()
        {
            base.Init();

            LordToilData_Carnival data = Data; // more efficient to cast once

            // Set radius for carnies to stick to
            data.baseRadius = Mathf.InverseLerp(RADIUS_MIN, RADIUS_MAX, this.lord.ownedPawns.Count / 50f);
            data.baseRadius = Mathf.Clamp(data.baseRadius, RADIUS_MIN, RADIUS_MAX);

            // Cache pawn roles (somewhat inefficient memory use, I know...)
            foreach (CarnivalRole role in Enum.GetValues(typeof(CarnivalRole)))
            {
                List<Pawn> pawns = (from p in this.lord.ownedPawns
                                    where p.Is(role)
                                    select p).ToList();
                data.pawnsWithRoles.Add(role, pawns);
            }


            int numCarnies = this.lord.ownedPawns.Count - data.pawnsWithRoles[CarnivalRole.Carrier].Count;
            
            // Give workers tents (currently 8 carnies per tent)
            int numBedTents = numCarnies > 8 ? Mathf.CeilToInt(numCarnies / 8f) : 1;

            Thing tentCrate;
            for (int i = 0; i < numBedTents; i++)
            {
                tentCrate = ThingMaker.MakeThing(_DefOf.Carn_Crate_TentLodge, GenStuff.RandomStuffFor(_DefOf.Carn_Crate_TentLodge));
                // Makes them carry them instead of just having them in inventory:
                data.TryHaveWorkerCarry(tentCrate);
            }

            if (data.pawnsWithRoles[CarnivalRole.Manager].Count > 0)
            {
                tentCrate = ThingMaker.MakeThing(_DefOf.Carn_Crate_TentMan, _DefOf.DevilstrandCloth);
                if (!data.TryHaveWorkerCarry(tentCrate))
                {
                    // If no workers to carry it, force the manager to carry it
                    data.pawnsWithRoles[CarnivalRole.Manager].First().carryTracker.TryStartCarry(tentCrate);
                }
            }

            // Place blueprints
            data.blueprints = BlueprintPlacer.PlaceCarnivalBlueprints(data.setupSpot, base.Map, this.lord.faction, data.availableCrates).ToList();

        }



        public override void UpdateAllDuties()
        {
            LordToilData_Carnival data = this.Data; // Single cast
            
            foreach (Pawn pawn in this.lord.ownedPawns)
            {
                if (pawn.Is(CarnivalRole.Worker) && pawn.carryTracker.CarriedThing != null)
                {
                    SetAsBuilder(pawn);
                    continue;
                }

                SetAsIdler(pawn);
            }
        }



        public override void LordToilTick()
        {
            base.LordToilTick();

            LordToilData_Carnival data = this.Data;

            // Check if everything is setup
            if (this.lord.ticksInToil % 400 == 0)
            {
                if (!(from frame in this.Frames
                      where !frame.Destroyed
                      select frame).Any())
                {
                    if (!(from blue in data.blueprints
                          where !blue.Destroyed
                          select blue).Any())
                    {
                        if (!base.Map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial).Any(b => b.Faction == this.lord.faction))
                        {
                            // No frames, blueprints, OR buildings
                            if ((from crate in data.availableCrates
                                 where !crate.Destroyed
                                 select crate).Any())
                            {
                                // Some more available crates, place new blueprints
                                data.blueprints = BlueprintPlacer.PlaceCarnivalBlueprints(data.setupSpot, base.Map, this.lord.faction, data.availableCrates).ToList();
                                this.UpdateAllDuties();
                                return;
                            }
                            else
                            {
                                // Nothing is buildable. Was the carnival attacked?
                                this.lord.ReceiveMemo("NothingBuildable");
                                return;
                            }
                        }
                        else
                        {
                            // Buildings are there. Next toil.
                            this.lord.ReceiveMemo("SetupDone");
                            return;
                        }
                    }
                }
            }
            // End check

        }

        public override void Notify_PawnLost(Pawn victim, PawnLostCondition cond)
        {
            if (cond == PawnLostCondition.IncappedOrKilled
                || cond == PawnLostCondition.MadePrisoner)
            {
                // Hostile actions
                this.lord.ReceiveMemo("HostileConditions");
                return;
            }

            // Non-hostile cause of PawnLost
            this.UpdateAllDuties();
        }


        public override void Notify_ConstructionFailed(Pawn pawn, Frame frame, Blueprint_Build newBlueprint)
        {
            if (frame.Faction == this.lord.faction && newBlueprint != null)
            {
                this.Data.blueprints.Add(newBlueprint);
            }
        }


        public override void Cleanup()
        {
            // Do more cleanup here?
            Data.blueprints.RemoveAll(blue => blue.Destroyed);
            foreach (Blueprint b in Data.blueprints)
            {
                b.Destroy(DestroyMode.Cancel);
            }
            foreach (Frame frame in this.Frames)
            {
                frame.Destroy(DestroyMode.Cancel);
            }
        }



        private void SetAsBuilder(Pawn p)
        {
            if (p != null)
            {
                p.mindState.duty = new PawnDuty(_DefOf.BuildCarnival, Data.setupSpot, Data.baseRadius);

                p.workSettings.EnableAndInitialize();
                p.workSettings.SetPriority(WorkTypeDefOf.Construction, 1);
            }
        }

        private void SetAsIdler(Pawn p)
        {
            Pawn random;
            
            if (!this.lord.ownedPawns.TryRandomElement(out random)
                && p != null)
            {
                p.mindState.duty = new PawnDuty(DutyDefOf.Escort, random, 6f);
            }
        }

    }
}
