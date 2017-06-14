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

        public override void LordToilTick()
        {
            // Check if there are any things needing to be hauled to carriers
            if (this.lord.ticksInToil % 1000 == 0)
            {
                foreach (Thing thing in from t in GenRadial.RadialDistinctThingsAround(Info.setupCentre, this.Map, Info.baseRadius, true)
                                        where (t.def.IsWithinCategory(ThingCategoryDefOf.Foods)
                                          || t.def.IsWithinCategory(ThingCategoryDefOf.FoodMeals)
                                          || t.def.IsWithinCategory(ThingCategoryDefOf.Drugs)
                                          || t.def == ThingDefOf.Silver
                                          || t.def == ThingDefOf.Gold)
                                          && !Info.thingsToHaul.Contains(t)
                                        select t)
                {
                    Info.thingsToHaul.Push(thing);
                }
            }
        }
    }
}
