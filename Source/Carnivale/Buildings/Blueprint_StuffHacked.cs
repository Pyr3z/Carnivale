using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Carnivale
{
    public class Blueprint_StuffHacked : Blueprint_Build
    {

        protected override Thing MakeSolidThing()
        {
            var frameDef = this.def.entityDefToBuild.frameDef;
            var hackedFrame = (Frame_StuffHacked)Activator.CreateInstance(typeof(Frame_StuffHacked));
            hackedFrame.def = frameDef;
            hackedFrame.PostMake();

            return hackedFrame;
        }


        public override List<ThingCountClass> MaterialsNeeded()
        {
            return this.def.entityDefToBuild.CostListAdjusted(null, false);
        }

    }
}
