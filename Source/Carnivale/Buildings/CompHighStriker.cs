using UnityEngine;
using Verse;

namespace Carnivale
{
    [StaticConstructorOnStartup]
    public class CompHighStriker : CompCarnBuilding
    {
        private const int MinJumpTickDuration = 20;

        private const int MaxJumpTickDuration = 100;

        private const float MinOffset = -2.171875f;

        private const float MaxOffset = 2.3125f;

        private const float MaxJumpHeight = -MinOffset + MaxOffset;

        private static readonly Vector3 Vector111 = new Vector3(1f, 1f, 1f);

        private static readonly Material StrikerMat = MaterialPool.MatFrom("Carnivale/Building/StrikerMat");

        private bool jumpingNow = false;

        private int curTick = 0;

        private int curTickDuration = 20;

        private float curPosZ = -1f;

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
                return parent.TrueCenter().z + MinOffset;
            }
        }

        private float MaxPosZ
        {
            get
            {
                return MinPosZ + MaxJumpHeight * curMaxHeightPercent;
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            if (jumpingNow)
            {
                CurPosZ = Mathf.Lerp(MinPosZ, MaxPosZ, curHeightPercent);
                
                if (curTick < curTickDuration / 2)
                {
                    curHeightPercent = (++curTick / (curTickDuration / 2f));
                }
                else
                {
                    jumpingNow = false;
                }
            }
            else if (CurPosZ > MinPosZ)
            {
                CurPosZ = Mathf.Lerp(MinPosZ, MaxPosZ, curHeightPercent);

                if (curTick > 0)
                {
                    curHeightPercent = (--curTick / (curTickDuration / 2f));
                }
                else
                {
                    CurPosZ = MinPosZ;
                }
            }

        }

        public override void PostDraw()
        {
            base.PostDraw();

            var pos = this.parent.TrueCenter();
            pos.y += 0.046875f;
            pos.z = CurPosZ;

            var trsMatrix = default(Matrix4x4);
            trsMatrix.SetTRS(pos, Quaternion.identity, Vector111);

            Graphics.DrawMesh(MeshPool.plane025, trsMatrix, StrikerMat, 0);
        }

        public void TriggerStrikerJump(float maxHeightPercent)
        {
            curHeightPercent = 0f;
            curMaxHeightPercent = maxHeightPercent;
            curTickDuration = (int)Mathf.Lerp(MinJumpTickDuration, MaxJumpTickDuration, curMaxHeightPercent);
            jumpingNow = true;
            Log.Warning("Reached striker jump trigger. jumpingNow=" + jumpingNow + ", curMaxHeightPercent=" + curMaxHeightPercent);
        }

    }
}
