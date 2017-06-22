using System;
using Verse.AI.Group;

namespace Carnivale.AI
{
    public class LordToil_Leave : LordToil
    {
        public override bool AllowRestingInBed { get { return false; } }

        public override bool AllowSatisfyLongNeeds { get { return false; } }



        public override void UpdateAllDuties()
        {
            throw new NotImplementedException();
        }
    }
}
