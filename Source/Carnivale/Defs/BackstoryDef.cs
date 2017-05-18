using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Carnivale
{
    public class BackstoryDef : Def    // from CCL
    {
        #region XML Data
        public string baseDescription;
        public BodyType bodyTypeGlobal = BodyType.Undefined;
        public BodyType bodyTypeMale = BodyType.Male;
        public BodyType bodyTypeFemale = BodyType.Female;
        public string title;
        public string titleShort;
        public BackstorySlot slot = BackstorySlot.Adulthood;
        public bool shuffleable = true;
        public bool addToDatabase = true;
        public List<WorkTags> workAllows = new List<WorkTags>();
        public List<WorkTags> workDisables = new List<WorkTags>();
        public List<WorkTags> requiredWorkTags = new List<WorkTags>();
        public List<BackstoryDefListItem> skillGains = new List<BackstoryDefListItem>();
        public List<string> spawnCategories = new List<string>();
        public List<BackstoryDefListItem> forcedTraits = new List<BackstoryDefListItem>();
        public List<BackstoryDefListItem> disallowedTraits = new List<BackstoryDefListItem>();
        #endregion

        public static BackstoryDef Named(string defName)
        {
            return DefDatabase<BackstoryDef>.GetNamed(defName);
        }

        public override void ResolveReferences()
        {
            base.ResolveReferences();

            if (!this.addToDatabase) return;
            if (BackstoryDatabase.allBackstories.ContainsKey(this.UniqueSaveKey())) return;

            Backstory b = new Backstory();
            if (!this.title.NullOrEmpty())
                b.SetTitle(this.title);
            else
            {
                return;
            }
            if (!titleShort.NullOrEmpty())
                b.SetTitleShort(titleShort);
            else
                b.SetTitleShort(b.Title);

            if (!baseDescription.NullOrEmpty())
                b.baseDesc = baseDescription;
            else
            {
                b.baseDesc = "Empty.";
            }

            b.bodyTypeGlobal = bodyTypeGlobal;
            b.bodyTypeMale = bodyTypeMale;
            b.bodyTypeFemale = bodyTypeFemale;

            b.slot = slot;

            b.shuffleable = shuffleable;
            if (spawnCategories.NullOrEmpty())
            {
                return;
            }
            else
                b.spawnCategories = spawnCategories;

            if (workAllows.Count > 0)
            {
                foreach (WorkTags current in Enum.GetValues(typeof(WorkTags)))
                {
                    if (!workAllows.Contains(current))
                    {
                        b.workDisables |= current;
                    }
                }
            }
            else if (workDisables.Count > 0)
            {
                foreach (var tag in workDisables)
                {
                    b.workDisables |= tag;
                }
            }
            else
            {
                b.workDisables = WorkTags.None;
            }

            if (requiredWorkTags.Count > 0)
            {
                foreach (var tag in requiredWorkTags)
                {
                    b.requiredWorkTags |= tag;
                }
            }
            else
            {
                b.requiredWorkTags = WorkTags.None;
            }

            b.skillGains = skillGains.ToDictionary(i => i.defName, i => i.degree);

            if (forcedTraits.Count > 0)
            {
                b.forcedTraits = new List<TraitEntry>();

                foreach (var trait in forcedTraits)
                {
                    b.forcedTraits.Add(new TraitEntry(TraitDef.Named(trait.defName), trait.degree));
                }

            }

            if (disallowedTraits.Count > 0)
            {
                // This approach is better than Rainbeau Flambe's
                // Editable Backstories mod, as it allows multiple entries of
                // a spectrum trait to be disallowed.

                b.disallowedTraits = new List<TraitEntry>();

                foreach (var trait in disallowedTraits)
                {
                    b.disallowedTraits.Add(new TraitEntry(TraitDef.Named(trait.defName), trait.degree));
                }
            }

            b.ResolveReferences();
            b.PostLoad();
            b.identifier = this.UniqueSaveKey();

            bool flag = false;
            foreach (var s in b.ConfigErrors(false))
            {
                if (!flag)
                {
                    flag = true;
                }
            }
            if (!flag)
            {
                BackstoryDatabase.AddBackstory(b);
                Log.Message("Added " + this.UniqueSaveKey() + " backstory");
            }
        }
    }

    public static class BackstoryDefExt
    {
        public static string UniqueSaveKey(this BackstoryDef def)
        {
            // TODO: externalise this when core mod is extracted
            return "XnopeBS_" + def.defName;
        }
    }

    public struct BackstoryDefListItem
    {
        public string defName;
        public int degree;
    }

}
