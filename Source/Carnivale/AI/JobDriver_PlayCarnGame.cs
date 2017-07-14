using System.Collections.Generic;
using RimWorld;
using Verse.AI;
using Xnope;
using Verse;

namespace Carnivale
{
    public abstract class JobDriver_PlayCarnGame : JobDriver
    {
        protected bool victory = false;

        protected Building GameBuilding
        {
            get
            {
                return TargetThingA as Building;
            }
        }

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
                if (WatchTickAction())
                {
                    this.victory = true;
                    this.ReadyForNextToil();
                }
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
                if (this.victory)
                {
                    this.GetPrizeInitAction();
                }

                EndJobWith(JobCondition.Succeeded);
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;

            return toil;
        }


        protected virtual bool WatchTickAction()
        {
            var extraJoyGainFactor = TargetThingA.GetStatValue(StatDefOf.EntertainmentStrengthFactor);
            JoyUtility.JoyTickCheckEnd(this.pawn, JoyTickFullJoyAction.None, extraJoyGainFactor);

            return false;
        }

        protected abstract void GetPrizeInitAction();

    }
}
