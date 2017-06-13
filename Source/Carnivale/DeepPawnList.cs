using System;
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

        public DeepPawnList Concat(DeepPawnList other)
        {
            List<Pawn> newList = new List<Pawn>();

            foreach (Pawn p in this)
            {
                newList.Add(p);
            }
            foreach (Pawn p in other)
            {
                newList.Add(p);
            }

            return new DeepPawnList()
            {
                pawnsList = newList
            };
        }

        public bool Remove(Pawn pawn)
        {
            return pawnsList.Remove(pawn);
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

        public Pawn this[int index]
        {
            get
            {
                return this.pawnsList[index];
            }
            set
            {
                this.pawnsList[index] = value;
            }
        }

        public static implicit operator List<Pawn>(DeepPawnList dpl)
        {
            return dpl.pawnsList;
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
