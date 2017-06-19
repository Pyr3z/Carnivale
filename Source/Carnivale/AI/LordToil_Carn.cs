using Verse;
using Verse.AI.Group;
using System.Linq;
using RimWorld;

namespace Carnivale
{
    public abstract class LordToil_Carn : LordToil
    {
        // FIELDS + PROPERTIES //

        [Unsaved]
        private LordToilData_Carnival dataInt;

        public LordToilData_Carnival Data
        {
            get
            {
                if (dataInt == null)
                {
                    dataInt = (LordToilData_Carnival)this.data;
                }
                return dataInt;
            }
        }

        protected CarnivalInfo Info
        {
            get
            {
                return Data.info;
            }
        }

        public override IntVec3 FlagLoc
        {
            get
            {
                return Data.info.setupCentre;
            }
        }


        // CONSTRUCTORS //

        public LordToil_Carn() { }


        // OVERRIDE METHODS //

        // Do this in CarnivalInfo tick instead.
        //public override void LordToilTick()
        //{
        //    // Check if there are any things needing to be hauled to carriers or trash
        //    if (this.lord.ticksInToil % 1009 == 0)
        //    {
        //        foreach (Thing thing in from t in GenRadial.RadialDistinctThingsAround(Info.setupCentre, this.Map, Info.baseRadius, true)
        //                                where (t.def.IsWithinCategory(ThingCategoryDefOf.Root))
        //                                  && !t.def.IsWithinCategory(ThingCategoryDefOf.Chunks)
        //                                  && (!(this.data is LordToilData_Setup) || ((LordToilData_Setup)this.data).availableCrates.Contains(t))
        //                                  && !Info.thingsToHaul.Contains(t)
        //                                select t)
        //        {
        //            if (Prefs.DevMode)
        //                Log.Warning("[Debug] Adding " + thing + " to CarnivalInfo.thingsToHaul.");
        //            Info.thingsToHaul.Add(thing);
        //        }
        //    }
        //}


    }
}
