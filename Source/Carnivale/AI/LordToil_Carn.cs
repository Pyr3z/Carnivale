using Verse;
using Verse.AI.Group;

namespace Carnivale
{
    public abstract class LordToil_Carn : LordToil
    {
        // FIELDS + PROPERTIES //

        protected CarnivalInfo Info
        {
            get
            {
                return CarnivalUtils.Info;
            }
        }

        public override IntVec3 FlagLoc { get { return Info.setupCentre; } }


        // CONSTRUCTORS //

        public LordToil_Carn() { }


        // PROTECTED METHODS

        protected Pawn GetClosestCarrier(Pawn closestTo)
        {
            Pawn carrier = null;
            float minDist = float.MaxValue;
            foreach (var car in Info.pawnsWithRole[CarnivalRole.Carrier])
            {
                float tempDistSqrd = car.Position.DistanceToSquared(closestTo.Position);
                if (tempDistSqrd < minDist)
                {
                    minDist = tempDistSqrd;
                    carrier = car;
                }
            }

            return carrier;
        }



    }
}
