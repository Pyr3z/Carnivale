//using RimWorld;
//using UnityEngine;
//using Verse;
//using System.Collections.Generic;
//using System.Diagnostics;

//namespace Carnivale
//{
//    public class Zone_NonPlayerStockpile : Zone, IStoreSettingsParent//, ISlotGroupParent
//    {
//        public CarnivalInfo info;

//        //public SlotGroup slotGroup;

//        public StorageSettings settings;



//        public bool IgnoreStoredThingsBeauty
//        { get { return false; } }

//        public bool StorageTabVisible
//        { get { return false; } }

//        protected override Color NextZoneColor
//        { get { return ZoneColorUtility.NextStorageZoneColor(); } }



//        public Zone_NonPlayerStockpile() { }

//        public Zone_NonPlayerStockpile(string name, CarnivalInfo info) : base(name, info.nonplayerZoneManager)
//        {
//            this.info = info;
//            //this.slotGroup = new SlotGroup(this);
//            this.settings = new StorageSettings(this); // no filters yet
//        }



//        public override void ExposeData()
//        {
//            base.ExposeData();

//            Scribe_References.Look(ref this.info, "info");

//            Scribe_Deep.Look<StorageSettings>(ref this.settings, "settings", new object[]
//            {
//                this
//            });

//            //if (Scribe.mode == LoadSaveMode.PostLoadInit)
//            //{
//            //    this.slotGroup = new SlotGroup(this);
//            //}
//        }



//        [DebuggerHidden]
//        public IEnumerable<IntVec3> AllSlotCells()
//        {
//            for (int i = 0; i < this.cells.Count; i++)
//            {
//                yield return this.cells[i];
//            }
//        }

//        public List<IntVec3> AllSlotCellsList()
//        {
//            return this.cells;
//        }

//        public StorageSettings GetParentStoreSettings()
//        {
//            return null;
//        }

//        //public SlotGroup GetSlotGroup()
//        //{
//        //    return this.slotGroup;
//        //}

//        public StorageSettings GetStoreSettings()
//        {
//            return settings;
//        }

//        public override IEnumerable<Gizmo> GetGizmos()
//        {
//            yield return new Command_Toggle
//            {
//                icon = ContentFinder<Texture2D>.Get("UI/Commands/HideZone", true),
//                defaultLabel = ((!this.hidden) ? "CommandHideZoneLabel".Translate() : "CommandUnhideZoneLabel".Translate()),
//                defaultDesc = "CommandHideZoneDesc".Translate(),
//                isActive = (() => this.hidden),
//                toggleAction = delegate
//                {
//                    this.hidden = !this.hidden;
//                    foreach (var cell in this.Cells)
//                    {
//                        this.Map.mapDrawer.MapMeshDirty(cell, MapMeshFlag.Zone);
//                    }
//                },
//                hotKey = KeyBindingDefOf.Misc2
//            };
//        }
//    }
//}
