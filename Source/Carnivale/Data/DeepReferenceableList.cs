using System;
using System.Collections;
using System.Collections.Generic;
using Verse;

namespace Carnivale
{
    public sealed class DeepReferenceableList<T> : IEnumerable<T>, IExposable where T:ILoadReferenceable
    {
        private List<T> referenceableList;

        public int Count
        {
            get
            {
                return referenceableList.Count;
            }
        }

        public DeepReferenceableList<T> Concat(DeepReferenceableList<T> other)
        {
            List<T> newList = new List<T>();

            foreach (T p in this)
            {
                newList.Add(p);
            }
            foreach (T p in other)
            {
                newList.Add(p);
            }

            return new DeepReferenceableList<T>()
            {
                referenceableList = newList
            };
        }

        public void Add(T pawn)
        {
            referenceableList.Add(pawn);
        }

        public bool Remove(T pawn)
        {
            return referenceableList.Remove(pawn);
        }

        public void RemoveAt(int index)
        {
            referenceableList.RemoveAt(index);
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref referenceableList, "loadReferenceIDs", LookMode.Reference);
        }




        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)referenceableList).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)referenceableList).GetEnumerator();
        }

        public T this[int index]
        {
            get
            {
                return this.referenceableList[index];
            }
            set
            {
                this.referenceableList[index] = value;
            }
        }

        public static implicit operator List<T>(DeepReferenceableList<T> dpl)
        {
            return dpl.referenceableList;
        }

        public static implicit operator DeepReferenceableList<T>(List<T> list)
        {
            return new DeepReferenceableList<T>()
            {
                referenceableList = list
            };
        }
    }
}
