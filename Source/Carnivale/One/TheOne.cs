using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Carnivale
{
    public static class TheOne
    {
        public static TraitDef Def;

        public static void Instantiate()
        {
            Def = new TraitDef()
            {
                defName = "Carn_TheOne",
                degreeDatas = new List<TraitDegreeData>()
                {
                    new TraitDegreeData()
                    {
                        label = "The One",
                        description = "NAME's own mother was always afraid of HIM, even on her death bed. She used to say that NAME was a monster because of the things he could do...",

                    }
                },
            };

            //DefDatabase<TraitDef>.Add(Def);
        }

        public static void Bless(this Pawn p)
        {
            p.story.traits.GainTrait(new Trait(Def, 0, true));
        }
    }
}
