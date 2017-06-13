using RimWorld;
using System.Collections.Generic;
using System.Xml;
using Verse;
using Verse.AI.Group;
using System;

namespace Carnivale
{
    public class LordToilData_Carnival : LordToilData
    {
        public CarnivalInfo info;

        public LordToilData_Carnival(CarnivalInfo carnivalInfo)
        {
            this.info = carnivalInfo;
        }


        public override void ExposeData()
        {
            Scribe_References.Look(ref info, "info");
        }

    }
}
