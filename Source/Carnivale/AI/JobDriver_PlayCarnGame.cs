using System.Collections.Generic;
using RimWorld;
using Verse.AI;
using Xnope;

namespace Carnivale
{
    public abstract class JobDriver_PlayCarnGame : JobDriver
    {
        public override string GetReport()
        {
            return "playing " + TargetThingA.LabelShort + ".";
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.EndOnDespawnedOrNull(TargetIndex.A, JobCondition.Incompletable);

            // Reserve the building
            yield return Toils_Reserve.Reserve(TargetIndex.A);

            // Goto interaction cell
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.InteractionCell);

            // Pay
            // todo

            // "Watch" the building
            yield return WatchBuilding();

            // Get prize
            yield return GetPrize();
        }


        private Toil WatchBuilding()
        {
            var toil = new Toil();

            toil.initAction = delegate
            {
                pawn.Rotation = pawn.Position.RotationFacing(TargetLocA);
            };

            toil.AddPreTickAction(delegate
            {
                this.WatchTickAction();
            });
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = base.CurJob.def.joyDuration;

            return toil;
        }

        private Toil GetPrize()
        {
            var toil = new Toil();

            toil.initAction = delegate
            {
                this.GetPrizeInitAction();
                EndJobWith(JobCondition.Succeeded);
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;

            return toil;
        }


        protected virtual void WatchTickAction()
        {
            //this.pawn.GainComfortFromCellIfPossible();

            var extraJoyGainFactor = TargetThingA.GetStatValue(StatDefOf.EntertainmentStrengthFactor);
            JoyUtility.JoyTickCheckEnd(this.pawn, JoyTickFullJoyAction.GoToNextToil, extraJoyGainFactor);
        }

        protected abstract void GetPrizeInitAction();

    }
}
