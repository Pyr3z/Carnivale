using Verse.AI.Group;
using RimWorld;
using Verse;
using System;

namespace Carnivale
{
    public class LordJob_EntertainColony : LordJob
    {
        private CarnivalInfo info;

        private IntVec3 setupCentre;

        private int durationTicks;


        public LordJob_EntertainColony()
        {
            
        }

        public LordJob_EntertainColony(IntVec3 setupCentre, int durationDays) : this()
        {
            this.setupCentre = setupCentre;
            this.durationTicks = durationDays * GenDate.TicksPerDay;
        }


        public override void ExposeData()
        {
            Scribe_References.Look(ref this.info, "info");

            Scribe_Values.Look(ref this.setupCentre, "setupSpot", default(IntVec3), false);

            Scribe_Values.Look(ref this.durationTicks, "durationTicks", default(int), false);
        }

        public override void Cleanup()
        {
            base.Cleanup();
            info.Cleanup();
            Utilities.cachedRoles.Clear();
        }

        public override StateGraph CreateGraph()
        {
            this.info = Map.GetComponent<CarnivalInfo>();

            var mainGraph = new StateGraph();

            // Use LordJob_Travel as starting toil for this graph:
            var toil_MoveToSetup = mainGraph.AttachSubgraph(new LordJob_Travel(this.setupCentre).CreateGraph()).StartingToil;

            // Next toil is to set up
            var toil_Setup = new LordToil_SetupCarnival(info);
            mainGraph.AddToil(toil_Setup);

            var trans_Setup = new Transition(toil_MoveToSetup, toil_Setup);
            trans_Setup.AddTrigger(new Trigger_Memo("TravelArrived"));
            trans_Setup.AddTrigger(new Trigger_TicksPassed(2500));
            mainGraph.AddTransition(trans_Setup);

            // Meat of the event: entertaining the colony
            var toil_Entertain = new LordToil_EntertainColony();
            mainGraph.AddToil(toil_Entertain);

            var trans_Entertain = new Transition(toil_Setup, toil_Entertain);
            trans_Entertain.AddTrigger(new Trigger_Memo("SetupDoneEntertain"));
            trans_Entertain.AddPostAction(new TransitionAction_Message("CarnEntertainNow".Translate(this.lord.faction)));
            trans_Entertain.AddPostAction(new TransitionAction_Custom(() => info.entertainingNow = true));
            mainGraph.AddTransition(trans_Entertain);

            // Rest the carnival between 22:00 and 10:00, or if anyone needs rest
            var toil_Rest = new LordToil_RestCarnival();
            mainGraph.AddToil(toil_Rest);

            var trans_ToRestFromSetup = new Transition(toil_Setup, toil_Rest);
            trans_ToRestFromSetup.AddTrigger(new Trigger_Memo("SetupDoneRest"));
            mainGraph.AddTransition(trans_ToRestFromSetup);

            var trans_ToRest = new Transition(toil_Entertain, toil_Rest);
            trans_ToRest.AddTrigger(new Trigger_TickCondition(() => info.AnyCarnyNeedsRest || !info.CanEntertainNow));
            trans_ToRest.AddPostAction(new TransitionAction_Message("CarnResting".Translate(this.lord.faction)));
            trans_ToRest.AddPostAction(new TransitionAction_Custom(delegate ()
            {
                info.entertainingNow = false;
                info.alreadyEntertainedToday = true;
            }));
            mainGraph.AddTransition(trans_ToRest);

            var trans_FromRest = new Transition(toil_Rest, toil_Entertain);
            trans_FromRest.AddTrigger(new Trigger_TickCondition(() => !info.AnyCarnyNeedsRest && info.CanEntertainNow));
            trans_FromRest.AddPostAction(new TransitionAction_WakeAll());
            trans_FromRest.AddPostAction(new TransitionAction_Message("CarnEntertainNow".Translate(this.lord.faction)));
            mainGraph.AddTransition(trans_FromRest);

            // Defend if attacked (needs work)
            var toil_Defend = new LordToil_DefendCarnival(this.setupCentre, 30f);
            mainGraph.AddToil(toil_Defend);

            var trans_Defend = new Transition(toil_Setup, toil_Defend);
            trans_Defend.AddSources(toil_Entertain);
            trans_Defend.AddTrigger(new Trigger_BecameColonyEnemy());
            trans_Defend.AddTrigger(new Trigger_Memo("HostileConditions")); // TODO: make carnies actively attack
            mainGraph.AddTransition(trans_Defend);

            // Strike buildings
            var toil_Strike = new LordToil_StrikeCarnival();
            mainGraph.AddToil(toil_Strike);

            var trans_Strike = new Transition(toil_Rest, toil_Strike);
            trans_Strike.AddSources(toil_Entertain);
            trans_Strike.AddTrigger(new Trigger_TicksPassed(this.durationTicks));
            trans_Strike.AddPostAction(new TransitionAction_Message("CarnPackingUp".Translate(this.lord.faction)));
            trans_Strike.AddPostAction(new TransitionAction_Custom(() => info.entertainingNow = false));
            mainGraph.AddTransition(trans_Strike);

            // Debug ender
            if (Prefs.DevMode)
            {
                var trans_StrikeDebug = new Transition(toil_Rest, toil_Strike);
                trans_StrikeDebug.AddSources(toil_Entertain);
                trans_StrikeDebug.AddTrigger(new Trigger_TickCondition(() => info.thingsToHaul.Any(t => t.def == ThingDefOf.AIPersonaCore)));
                trans_StrikeDebug.AddPostAction(new TransitionAction_Message("[Debug] " + "CarnPackingUp".Translate(this.lord.faction)));
                trans_StrikeDebug.AddPostAction(new TransitionAction_Custom(() => info.entertainingNow = false));
                mainGraph.AddTransition(trans_StrikeDebug);
            }

            // Normal exit map toil
            var toil_Exit = new LordToil_Leave();
            mainGraph.AddToil(toil_Exit);

            var trans_Exit = new Transition(toil_Strike, toil_Exit);
            trans_Exit.AddTrigger(new Trigger_Memo("StrikeDone"));
            trans_Exit.AddPostAction(new TransitionAction_EndAllJobs());
            trans_Exit.AddPostAction(new TransitionAction_WakeAll());
            mainGraph.AddTransition(trans_Exit);

            // TODO panic exit map

            return mainGraph;
        }


        //private StateGraph VisitColonyClone()
        //{
        //    // Deprecated CreateGraph() method. Here for reference.
        //    // Behaviour is very close to visitors.

        //    StateGraph mainGraph = new StateGraph();

        //    LordToil lordToil_MoveToSetup = mainGraph.AttachSubgraph(new LordJob_Travel(this.setupCentre).CreateGraph()).StartingToil;
        //    mainGraph.StartingToil = lordToil_MoveToSetup;

        //    LordToil_DefendCarnival lordToil_Defend = new LordToil_DefendCarnival(this.setupCentre, 30f);
        //    mainGraph.AddToil(lordToil_Defend);

        //    LordToil_TakeWoundedGuest lordToil_TakeWounded = new LordToil_TakeWoundedGuest();
        //    mainGraph.AddToil(lordToil_TakeWounded);

        //    StateGraph exitGraph = new LordJob_TravelAndExit(IntVec3.Invalid).CreateGraph();
        //    LordToil lordToil_MoveToExit = mainGraph.AttachSubgraph(exitGraph).StartingToil;
        //    LordToil exitMapTarget = exitGraph.lordToils[1];
        //    LordToil_ExitMap lordToil_Exit = new LordToil_ExitMap(LocomotionUrgency.Walk, true);
        //    mainGraph.AddToil(lordToil_Exit);


        //    // Exit due to bad temperature
        //    Transition trans_ExitBadTemp = new Transition(lordToil_MoveToSetup, lordToil_MoveToExit);
        //    trans_ExitBadTemp.AddSource(lordToil_Defend);
        //    trans_ExitBadTemp.AddTrigger(new Trigger_PawnExperiencingDangerousTemperatures());
        //    trans_ExitBadTemp.AddPreAction(new TransitionAction_Message("MessageVisitorsDangerousTemperature".Translate(new object[] {
        //        faction.def.pawnsPlural.CapitalizeFirst(),
        //        faction.Name
        //    })));
        //    trans_ExitBadTemp.AddPreAction(new TransitionAction_EnsureHaveExitDestination());
        //    trans_ExitBadTemp.AddPostAction(new TransitionAction_WakeAll());
        //    mainGraph.AddTransition(trans_ExitBadTemp);

        //    // Exit due to being trapped
        //    Transition trans_ExitTrapped = new Transition(lordToil_MoveToSetup, lordToil_Exit);
        //    trans_ExitTrapped.AddSources(new LordToil[] {
        //        lordToil_Defend,
        //        lordToil_TakeWounded
        //    });
        //    trans_ExitTrapped.AddSources(exitGraph.lordToils);
        //    trans_ExitTrapped.AddTrigger(new Trigger_PawnCannotReachMapEdge());
        //    trans_ExitTrapped.AddPreAction(new TransitionAction_Message("MessageVisitorsTrappedLeaving".Translate(new object[] {
        //        faction.def.pawnsPlural.CapitalizeFirst(),
        //        faction.Name
        //    })));
        //    mainGraph.AddTransition(trans_ExitTrapped);

        //    // ???
        //    Transition trans_ = new Transition(lordToil_Exit, lordToil_MoveToExit);
        //    trans_.AddTrigger(new Trigger_PawnCanReachMapEdge());
        //    trans_.AddPreAction(new TransitionAction_EnsureHaveExitDestination());
        //    trans_.AddPostAction(new TransitionAction_EndAllJobs());
        //    mainGraph.AddTransition(trans_);

        //    // Arrival
        //    Transition trans_Arrival = new Transition(lordToil_MoveToSetup, lordToil_Defend);
        //    trans_Arrival.AddTrigger(new Trigger_Memo("TravelArrived"));
        //    mainGraph.AddTransition(trans_Arrival);

        //    // Gathering wounded
        //    Transition trans_TakeWounded = new Transition(lordToil_Defend, lordToil_TakeWounded);
        //    trans_TakeWounded.AddTrigger(new Trigger_WoundedGuestPresent());
        //    trans_TakeWounded.AddPreAction(new TransitionAction_Message("MessageVisitorsTakingWounded".Translate(new object[] {
        //        faction.def.pawnsPlural.CapitalizeFirst(),
        //        faction.Name
        //    })));

        //    // Exit due to becoming enemy
        //    Transition trans_BecomeEnemy = new Transition(lordToil_Defend, exitMapTarget);
        //    trans_BecomeEnemy.AddSources(new LordToil[] {
        //        lordToil_TakeWounded,
        //        lordToil_MoveToSetup
        //    });
        //    trans_BecomeEnemy.AddTrigger(new Trigger_BecameColonyEnemy());
        //    trans_BecomeEnemy.AddPreAction(new TransitionAction_SetDefendLocalGroup());
        //    trans_BecomeEnemy.AddPostAction(new TransitionAction_WakeAll());
        //    trans_BecomeEnemy.AddPostAction(new TransitionAction_EndAllJobs());
        //    mainGraph.AddTransition(trans_BecomeEnemy);

        //    // Exit normally, from elapsed time
        //    Transition trans_ExitNormally = new Transition(lordToil_Defend, lordToil_MoveToExit);
        //    trans_ExitNormally.AddTrigger(new Trigger_TicksPassed(durationTicks + Rand.Range(-30000, 10000)));
        //    trans_ExitNormally.AddPreAction(new TransitionAction_Message("VisitorsLeaving".Translate(faction.Name)));
        //    mainGraph.AddTransition(trans_ExitNormally);



        //    return mainGraph;
        //}

    }
}
