using System.Collections;
using System.Collections.Generic;
using Verse;

namespace Carnivale
{
    public class DeepPawnList : IEnumerable<Pawn>, IExposable
    {
        private List<Pawn> pawnsList;

        public int Count
        {
            get
            {
                return pawnsList.Count;
            }
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref pawnsList, "pawnsList", LookMode.Reference, new object[0]);
        }

        public IEnumerator<Pawn> GetEnumerator()
        {
            return ((IEnumerable<Pawn>)pawnsList).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<Pawn>)pawnsList).GetEnumerator();
        }

        public static implicit operator DeepPawnList(List<Pawn> list)
        {
            return new DeepPawnList()
            {
                pawnsList = list
            };
        }
    }
}
