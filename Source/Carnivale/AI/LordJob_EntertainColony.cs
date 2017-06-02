using Verse.AI.Group;
using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;

namespace Carnivale.AI
{
    public class LordJob_EntertainColony : LordJob
    {
        private Faction faction;

        private IntVec3 setupSpot;

        private int durationTicks;



        private LordJob_EntertainColony() { }

        public LordJob_EntertainColony(Faction faction, IntVec3 setupSpot, int durationDays)
        {
            this.faction = faction;
            this.setupSpot = setupSpot;
            this.durationTicks = durationDays * GenDate.TicksPerDay;

        }



        public override StateGraph CreateGraph()
        {
            StateGraph mainGraph = new StateGraph();

            LordToil toil_MoveToSetup = mainGraph.AttachSubgraph(new LordJob_Travel(this.setupSpot).CreateGraph()).StartingToil;
            mainGraph.StartingToil = toil_MoveToSetup;



            return mainGraph;
        }


        private StateGraph VisitColonyClone()
        {
            // Deprecated CreateGraph() method. Here for reference.
            // Behaviour is very close to visitors.

            StateGraph mainGraph = new StateGraph();

            LordToil lordToil_MoveToSetup = mainGraph.AttachSubgraph(new LordJob_Travel(this.setupSpot).CreateGraph()).StartingToil;
            mainGraph.StartingToil = lordToil_MoveToSetup;

            LordToil_DefendCarnival lordToil_Defend = new LordToil_DefendCarnival(this.setupSpot, 30f);
            mainGraph.AddToil(lordToil_Defend);

            LordToil_TakeWoundedGuest lordToil_TakeWounded = new LordToil_TakeWoundedGuest();
            mainGraph.AddToil(lordToil_TakeWounded);

            StateGraph exitGraph = new LordJob_TravelAndExit(IntVec3.Invalid).CreateGraph();
            LordToil lordToil_MoveToExit = mainGraph.AttachSubgraph(exitGraph).StartingToil;
            LordToil exitMapTarget = exitGraph.lordToils[1];
            LordToil_ExitMap lordToil_Exit = new LordToil_ExitMap(LocomotionUrgency.Walk, true);
            mainGraph.AddToil(lordToil_Exit);


            // Exit due to bad temperature
            Transition trans_ExitBadTemp = new Transition(lordToil_MoveToSetup, lordToil_MoveToExit);
            trans_ExitBadTemp.AddSource(lordToil_Defend);
            trans_ExitBadTemp.AddTrigger(new Trigger_PawnExperiencingDangerousTemperatures());
            trans_ExitBadTemp.AddPreAction(new TransitionAction_Message("MessageVisitorsDangerousTemperature".Translate(new object[] {
                faction.def.pawnsPlural.CapitalizeFirst(),
                faction.Name
            })));
            trans_ExitBadTemp.AddPreAction(new TransitionAction_EnsureHaveExitDestination());
            trans_ExitBadTemp.AddPostAction(new TransitionAction_WakeAll());
            mainGraph.AddTransition(trans_ExitBadTemp);

            // Exit due to being trapped
            Transition trans_ExitTrapped = new Transition(lordToil_MoveToSetup, lordToil_Exit);
            trans_ExitTrapped.AddSources(new LordToil[] {
                lordToil_Defend,
                lordToil_TakeWounded
            });
            trans_ExitTrapped.AddSources(exitGraph.lordToils);
            trans_ExitTrapped.AddTrigger(new Trigger_PawnCannotReachMapEdge());
            trans_ExitTrapped.AddPreAction(new TransitionAction_Message("MessageVisitorsTrappedLeaving".Translate(new object[] {
                faction.def.pawnsPlural.CapitalizeFirst(),
                faction.Name
            })));
            mainGraph.AddTransition(trans_ExitTrapped);

            // ???
            Transition trans_ = new Transition(lordToil_Exit, lordToil_MoveToExit);
            trans_.AddTrigger(new Trigger_PawnCanReachMapEdge());
            trans_.AddPreAction(new TransitionAction_EnsureHaveExitDestination());
            trans_.AddPostAction(new TransitionAction_EndAllJobs());
            mainGraph.AddTransition(trans_);

            // Arrival
            Transition trans_Arrival = new Transition(lordToil_MoveToSetup, lordToil_Defend);
            trans_Arrival.AddTrigger(new Trigger_Memo("TravelArrived"));
            mainGraph.AddTransition(trans_Arrival);

            // Gathering wounded
            Transition trans_TakeWounded = new Transition(lordToil_Defend, lordToil_TakeWounded);
            trans_TakeWounded.AddTrigger(new Trigger_WoundedGuestPresent());
            trans_TakeWounded.AddPreAction(new TransitionAction_Message("MessageVisitorsTakingWounded".Translate(new object[] {
                faction.def.pawnsPlural.CapitalizeFirst(),
                faction.Name
            })));

            // Exit due to becoming enemy
            Transition trans_BecomeEnemy = new Transition(lordToil_Defend, exitMapTarget);
            trans_BecomeEnemy.AddSources(new LordToil[] {
                lordToil_TakeWounded,
                lordToil_MoveToSetup
            });
            trans_BecomeEnemy.AddTrigger(new Trigger_BecameColonyEnemy());
            trans_BecomeEnemy.AddPreAction(new TransitionAction_SetDefendLocalGroup());
            trans_BecomeEnemy.AddPostAction(new TransitionAction_WakeAll());
            trans_BecomeEnemy.AddPostAction(new TransitionAction_EndAllJobs());
            mainGraph.AddTransition(trans_BecomeEnemy);

            // Exit normally, from elapsed time
            Transition trans_ExitNormally = new Transition(lordToil_Defend, lordToil_MoveToExit);
            trans_ExitNormally.AddTrigger(new Trigger_TicksPassed(durationTicks + Rand.Range(-30000, 10000)));
            trans_ExitNormally.AddPreAction(new TransitionAction_Message("VisitorsLeaving".Translate(faction.Name)));
            mainGraph.AddTransition(trans_ExitNormally);



            return mainGraph;
        }



        public override void ExposeData()
        {
            Scribe_References.Look(ref this.faction, "faction", false);

            Scribe_Values.Look(ref this.setupSpot, "setupSpot", default(IntVec3), false);

            Scribe_Values.Look(ref this.durationTicks, "durationTicks", default(int), false);

            //Scribe_Collections.Look(ref this.workersWithCrates, false, "workersWithCrates", LookMode.Reference);

            //Scribe_Collections.Look(ref this.availableCrates, false, "availableCrates", LookMode.Reference);
        }
    }
}
