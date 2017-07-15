using UnityEngine;
using Verse;

namespace Carnivale
{
    [StaticConstructorOnStartup]
    public class CompHighStriker : CompCarnBuilding
    {
        private const int MinJumpTickDuration = 50;

        private const int MaxJumpTickDuration = 100;

        private const float MinZOffset = -2.171875f;

        private const float MaxZOffset = 2.109375f; //2.3125f;

        private const float MaxJumpHeight = -MinZOffset + MaxZOffset;

        private static readonly Vector3 Vector111 = new Vector3(1f, 1f, 1f);

        private static readonly Material StrikerMat = MaterialPool.MatFrom("Carnivale/Building/StrikerMat");

        private bool jumpingNow = false;

        private int curTick = 0;

        private int curTickDuration = 0;

        private float curHeightPercent = 0f;

        private float curMaxHeightPercent = 0f;

        private float curPosZ = -1f;

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
                return parent.TrueCenter().z + MinZOffset;
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
                
                if (curTick < curTickDuration)
                {
                    curHeightPercent = Mathf.Sin(Mathf.PI * (++curTick / (float)curTickDuration));
                }
                else
                {
                    curTick = 0;
                    curTickDuration = 0;
                    curHeightPercent = 0f;
                    curMaxHeightPercent = 0f;
                    jumpingNow = false;
                }
            }

        }

        public override void PostDraw()
        {
            base.PostDraw();

            var pos = this.parent.TrueCenter();
            pos.y += 0.046875f; // 3 'pixels' toward camera
            pos.z = CurPosZ;

            var trsMatrix = default(Matrix4x4);
            trsMatrix.SetTRS(pos, Quaternion.identity, Vector111);

            Graphics.DrawMesh(MeshPool.plane10, trsMatrix, StrikerMat, 0);
        }

        public void TriggerStrikerJump(float maxHeightPercent)
        {
            curMaxHeightPercent = maxHeightPercent;

            curTickDuration = (int)Mathf.Lerp(MinJumpTickDuration, MaxJumpTickDuration, curMaxHeightPercent);

            jumpingNow = true;

            //Log.Warning("Reached striker jump trigger. jumpingNow=" + jumpingNow + ", curMaxHeightPercent=" + curMaxHeightPercent);
        }


        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref this.jumpingNow, "jumpingNow", false);
            Scribe_Values.Look(ref this.curTick, "tick", 0);
            Scribe_Values.Look(ref this.curTickDuration, "tickDuration", 0);
            Scribe_Values.Look(ref this.curHeightPercent, "heighPercent", 0f);
            Scribe_Values.Look(ref this.curMaxHeightPercent, "maxHeightPercent", 0f);
            Scribe_Values.Look(ref this.curPosZ, "strikerPosZ", -1f, true);
        }
    }
}
