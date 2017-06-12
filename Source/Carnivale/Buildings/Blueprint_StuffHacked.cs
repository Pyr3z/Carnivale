using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Carnivale
{
    public class Blueprint_StuffHacked : Blueprint_Build
    {
        // Should only be used for stuffed tents.

        protected override Thing MakeSolidThing()
        {
            // A copy of ThingMaker.MakeThing() without validation checks,
            // to allow for hacky frame stuffing.
            ThingDef frameDef = this.def.entityDefToBuild.frameDef;
            Thing thing = (Thing)Activator.CreateInstance(frameDef.thingClass);
            thing.def = frameDef;
            //thing.SetStuffDirect(this.stuffToUse);
            thing.PostMake();
            return thing;
        }


        public override List<ThingCountClass> MaterialsNeeded()
        {
            return this.def.entityDefToBuild.CostListAdjusted(null, false);
        }

    }
}
