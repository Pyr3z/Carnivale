using UnityEngine;
using Verse;

namespace Carnivale
{
    public class Building_Tent : Building
    {
        
        // Draw flag colour
        public override Color DrawColorTwo
        { get { return this.factionInt.Color; } }


    }
}
