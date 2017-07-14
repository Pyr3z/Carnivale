using RimWorld;
using UnityEngine;
using Verse;

namespace Carnivale
{
    public class JobDriver_PlayHighStriker : JobDriver_PlayCarnGame
    {
        private static Effecter strikeEffect = EffecterDefOf.Mine.Spawn();

        private const int RingerJumpInterval = 350;

        private CompHighStriker compInt = null;

        private CompHighStriker Comp
        {
            get
            {
                if (compInt == null)
                {
                    compInt = GameBuilding.GetComp<CompHighStriker>();
                    if (compInt == null)
                    {
                        Log.Error("Could not get CompHighStriker from " + GameBuilding + ". Expect more errors.");
                    }
                }

                return compInt;
            }
        }

        protected override void WatchTickAction()
        {
            if (this.pawn.IsHashIntervalTick(RingerJumpInterval))
            {
                // Do strike effect
                strikeEffect.Trigger(pawn, new TargetInfo(pawn.Position + IntVec3.East, Map));

                // Make striker jump
                var meleeSkillOffset = pawn.skills.GetSkill(SkillDefOf.Melee).Level / 100;
                var luckiness = pawn.GetStatValue(_DefOf.Stat_Luckiness);
                var heightPercent = Rand.Range(0.0f, 1.0f) + luckiness + meleeSkillOffset;
                heightPercent = Mathf.Clamp(heightPercent, 0.10f, 1.0f);

                Comp.TriggerStrikerJump(heightPercent);
            }

            base.WatchTickAction();
        }

        protected override void GetPrizeInitAction()
        {
            
        }

    }
}
