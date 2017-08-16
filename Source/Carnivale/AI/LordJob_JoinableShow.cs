using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Carnivale
{
    public class LordJob_JoinableShow : LordJob_VoluntarilyJoinable
    {
        private static IntRange durationRange = new IntRange(15000, 36000);

        public Pawn entertainer;

        private CarnivalInfo info;

        private IntVec3 spot;

        private CellRect audienceRect;

        private IntVec3 entertainerSpot;


        public override bool AllowStartNewGatherings { get { return false; } }


        public LordJob_JoinableShow() { }

        public LordJob_JoinableShow(Building_Carn venue, Pawn entertainer)
        {
            this.info = CarnUtils.Info;

            spot = venue.AudienceCentre;

            this.entertainer = entertainer;

            var rotation = venue.Rotation;

            audienceRect = CellRect.CenteredOn(spot, 2);

            switch (rotation.AsByte)
            {
                case 0:
                    audienceRect.maxZ--;
                    entertainerSpot = spot + IntVec3.North * 2;
                    break;
                case 1:
                    audienceRect.maxX--;
                    entertainerSpot = spot + IntVec3.East * 2;
                    break;
                case 2:
                    audienceRect.minZ++;
                    entertainerSpot = spot + IntVec3.South * 2;
                    break;
                case 3:
                    audienceRect.minX++;
                    entertainerSpot = spot + IntVec3.West * 2;
                    break;
            }
        }



        public override StateGraph CreateGraph()
        {
            var mainGraph = new StateGraph();

            var toil_Attend = new LordToil_AttendShow(audienceRect, entertainer, entertainerSpot);
            mainGraph.AddToil(toil_Attend);

            var toil_End = new LordToil_End();
            mainGraph.AddToil(toil_End);

            var trans_NormalEnd = new Transition(toil_Attend, toil_End);
            trans_NormalEnd.AddTrigger(new Trigger_Memo("StopEntertaining"));
            trans_NormalEnd.AddTrigger(new Trigger_TicksPassed(durationRange.RandomInRange));
            trans_NormalEnd.AddPreAction(new TransitionAction_Message("ShowEnded".Translate(entertainer.LabelShort)));
            mainGraph.AddTransition(trans_NormalEnd);

            var trans_BadEnd = new Transition(toil_Attend, toil_End);
            trans_BadEnd.AddTrigger(new Trigger_Memo("DangerPresent"));
            trans_BadEnd.AddTrigger(new Trigger_TickCondition(() => ShouldBeCalledOff()));
            trans_BadEnd.AddTrigger(new Trigger_PawnLostViolently());
            trans_BadEnd.AddPreAction(new TransitionAction_Message("ShowEndedBad".Translate(entertainer.LabelShort)));
            mainGraph.AddTransition(trans_BadEnd);

            return mainGraph;
        }



        public override float VoluntaryJoinPriorityFor(Pawn p)
        {
            if (p == entertainer)
            {
                return 100f;
            }

            if (info.allowedColonists.Contains(p))
            {
                return VoluntarilyJoinableLordJobJoinPriorities.PartyGuest;
            }

            return 0f;
        }

        public override void ExposeData()
        {
            Scribe_References.Look(ref this.info, "info");
            Scribe_References.Look(ref this.entertainer, "entertainer");

            Scribe_Values.Look(ref this.spot, "spot");
            Scribe_Values.Look(ref this.audienceRect, "audienceRect");
            Scribe_Values.Look(ref this.entertainerSpot, "entertainerSpot");
        }

        public override string GetReport()
        {
            return "Carnival";
        }



        private bool ShouldBeCalledOff()
        {
            return !PartyUtility.AcceptableGameConditionsToContinueParty(base.Map) || (!this.spot.Roofed(base.Map) && !JoyUtility.EnjoyableOutsideNow(base.Map, null));
        }
    }
}
