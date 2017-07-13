using UnityEngine;
using Verse;

namespace Carnivale
{
    public class CompHighStriker : ThingComp
    {
        private const int JumpTickDuration = 100;

        private const float MinOffset = -2.171875f;

        private const float MaxOffset = 2.3125f;

        private const float MaxJumpHeight = -MinOffset + MaxOffset;

        private static readonly Vector3 Vector111 = new Vector3(1f, 1f, 1f);

        private static readonly Material StrikerMat = MaterialPool.MatFrom("Carnivale/Building/StrikerMat");

        private bool jumpingNow = false;

        private float curPosZ = -1f;

        private float minPosZ = -1f;

        private float curHeightPercent = 0f;

        private float curMaxHeightPercent = 0f;

        private float CurPosZ
        {
            get
            {
                if (curPosZ == -1f)
                {
                    curPosZ = MinPosZ;
                }

                return curPosZ;
            }
            set
            {
                curPosZ = value;
            }
        }

        private float MinPosZ
        {
            get
            {
                if (minPosZ == -1f)
                {
                    minPosZ = parent.TrueCenter().z + MinOffset;
                }

                return minPosZ;
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            if (jumpingNow)
            {
                // Lerp up
                CurPosZ = Mathf.Lerp(MinPosZ, MinPosZ + MaxJumpHeight * curHeightPercent, curHeightPercent);
                
                if (curHeightPercent < curMaxHeightPercent)
                {
                    curHeightPercent += MaxJumpHeight * curHeightPercent / JumpTickDuration;
                }
                else
                {
                    curHeightPercent = MaxJumpHeight;
                    jumpingNow = false;
                }
            }
            else if (CurPosZ > 0f)
            {
                // Lerp down
                CurPosZ = Mathf.Lerp(MinPosZ + MaxJumpHeight * curHeightPercent, MinPosZ, curHeightPercent);

                if (curHeightPercent > 0f)
                {
                    curHeightPercent -= MaxJumpHeight * curHeightPercent / JumpTickDuration;
                }
                else
                {
                    curHeightPercent = 0f;
                }
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();

            var jumpPos = new Vector3(this.parent.TrueCenter().x, 1f, CurPosZ);

            var trsMatrix = default(Matrix4x4);
            trsMatrix.SetTRS(jumpPos, default(Rot4).AsQuat, Vector111);

            Graphics.DrawMesh(MeshPool.plane025, trsMatrix, StrikerMat, 0);
        }

        public void TriggerStrikerJump(float maxHeightPercent)
        {
            curMaxHeightPercent = maxHeightPercent;
            jumpingNow = true;
        }

    }
}
