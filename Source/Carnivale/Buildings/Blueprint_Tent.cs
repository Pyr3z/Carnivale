using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using System.Diagnostics;
using System.Text;

namespace Carnivale
{
    public class Blueprint_Tent : Blueprint_Build
    {

        protected override Thing MakeSolidThing()
        {
            // A copy of ThingMaker.MakeThing() without validation checks
            ThingDef frameDef = this.def.entityDefToBuild.frameDef;
            Thing thing = (Thing)Activator.CreateInstance(frameDef.thingClass);
            thing.def = frameDef;
            thing.SetStuffDirect(this.stuffToUse);
            thing.PostMake();
            return thing;
        }

    }
}
