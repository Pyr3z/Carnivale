using System;
using System.Collections;
using System.Collections.Generic;
using Verse;

namespace Carnivale
{
    /// <summary>
    /// The use case for an inheriting type of this class is for exposing
    /// Dictionaries whose values are Lists of IReferenceables.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class DeepReferenceableList<T> : IEnumerable<T>, IExposable where T:ILoadReferenceable
    {
        protected List<T> referenceableList;

        public int Count { get { return referenceableList.Count; } }



        public DeepReferenceableList()
        {
            this.referenceableList = new List<T>();
        }



        public virtual void ExposeData()
        {
            Scribe_Collections.Look(ref referenceableList, "loadReferenceIDs", LookMode.Reference);
        }

        public IEnumerable<T> Concat(IEnumerable<T> other)
        {
            foreach (var t in this)
                yield return t;
            foreach (var o in other)
                yield return o;
        }

        public void Add(T t)
        {
            referenceableList.Add(t);
        }

        public bool Remove(T t)
        {
            return referenceableList.Remove(t);
        }

        public void RemoveAt(int index)
        {
            referenceableList.RemoveAt(index);
        }

        public T RandomElementOrNull()
        {
            return referenceableList.RandomElementWithFallback();
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

    }



    public sealed class DeepPawnList : DeepReferenceableList<Pawn>
    {
        public DeepPawnList()
        {
            this.referenceableList = new List<Pawn>();
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref this.referenceableList, "pawns", LookMode.Reference);
        }

        public static implicit operator DeepPawnList(List<Pawn> list)
        {
            return new DeepPawnList()
            {
                referenceableList = list
            };
        }
    }



}
