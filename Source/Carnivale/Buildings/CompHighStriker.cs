using UnityEngine;
using Verse;

namespace Carnivale
{
    public class CompHighStriker : ThingComp
    {
        private const int JumpTickDuration = 200;

        private const float MaxJumpHeight = 5.5f;

        private static readonly Material StrikerMat = MaterialPool.MatFrom("Carnivale/Building/StrikerMat");

        private bool jumpNow = false;

        private float jumpPos = 0f;

        public override void CompTick()
        {
            base.CompTick();

            
        }

        public override void PostDraw()
        {
            base.PostDraw();

            if (jumpNow)
            {

            }
        }

        public void TriggerStrikerJump(float heightPercent)
        {

        }

    }
}
