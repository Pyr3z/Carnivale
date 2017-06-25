using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Carnivale
{
    public class JobDriver_StrikeBuilding : JobDriver
    {
        private float workLeft;

        private float initialNeededWork;

        [Unsaved]
        private CarnivalInfo infoInt = null;

        private Thing Target
        {
            get
            {
                return base.CurJob.targetA.Thing;
            }
        }

        private Building Building
        {
            get
            {
                return (Building)this.Target.GetInnerIfMinified();
            }
        }

        private float InitialNeededWork
        {
            get
            {
                // half as much work than to build
                float workToBuild = Building.GetStatValue(StatDefOf.WorkToBuild);
                return workToBuild / 2f;
            }
        }

        private CarnivalInfo Info
        {
            get
            {
                if (infoInt == null)
                {
                    infoInt = Map.GetComponent<CarnivalInfo>();
                }

                return infoInt;
            }
        }


        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref this.workLeft, "workLeft");

            Scribe_Values.Look(ref this.initialNeededWork, "initialNeededWork");
        }



        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Reserve.Reserve(TargetIndex.A);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            yield return DoWork();

            yield return FinishRemoving();
        }


        private Toil DoWork()
        {
            Toil doWork = new Toil()
                .FailOnDespawnedOrNull(TargetIndex.A)
                .FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);

            doWork.initAction = delegate
            {
                workLeft = InitialNeededWork;
                initialNeededWork = workLeft;
            };

            doWork.tickAction = delegate
            {
                workLeft -= this.pawn.GetStatValue(StatDefOf.ConstructionSpeed);
                // do pawn skill increment, or fuck it doesn't matter?
                if (workLeft <= 0f)
                {
                    doWork.actor.jobs.curDriver.ReadyForNextToil();
                }
            };

            doWork.defaultCompleteMode = ToilCompleteMode.Never;

            return doWork.WithProgressBar(TargetIndex.A, () => 1f - workLeft / initialNeededWork);
        }
        

        private Toil FinishRemoving()
        {
            Toil toil = new Toil()
            {
                initAction = delegate
                {
                    Building.Destroy(DestroyMode.Deconstruct);
                    this.pawn.records.Increment(RecordDefOf.ThingsDeconstructed);

                    Info.carnivalBuildings.Remove(Building);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            return toil;
        }

    }
}
