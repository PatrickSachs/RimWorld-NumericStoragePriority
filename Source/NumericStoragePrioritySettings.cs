using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Lilith.RimWorld.NumericStoragePriority {
    /// <summary>
    /// Settings for the mod.
    /// </summary>
    public class NumericStoragePrioritySettings : ModSettings {
        public enum SortDirection {
            Original,
            NumericDesc,
            NumericAsc,
            AlphaDesc,
            AlphaAsc,
        }
        
        /// <summary>
        /// When this is enabled storage priorities will always be displayed as numbers, never names.
        /// </summary>
        public bool DisableNames = false;
        /// <summary>
        /// Disabled the number input and forces you to use custom presets.
        /// </summary>
        public bool DisableInput = false;
        /// <summary>
        /// Adds custom presets to the names.
        /// </summary>
        public Dictionary<int, string> CustomNames = new Dictionary<int, string>();
        /// <summary>
        /// How the priorities are sorted.
        /// </summary>
        public SortDirection Sort;
        
        private Vector2 _scrollPosition;
        private string _search;
        private byte? _lastEditedPriority = null;
        private byte _newPriority = 0;
        private string _newName = string.Empty;

        public string SettingsCategory() {
            return "Lilith_NumericStoragePriority_SettingsCategory".Translate();
        }

        public override void ExposeData() {
            Scribe_Values.Look(ref DisableNames, nameof(DisableNames));
            Scribe_Values.Look(ref Sort, nameof(Sort));
            Scribe_Collections.Look(ref CustomNames, nameof(CustomNames));
        }

        /// <summary>
        /// The (optional) GUI part to set your settings.
        /// </summary>
        /// <param name="inRect">A Unity Rect with the size of the settings window.</param>
        public void DoSettingsWindowContents(Rect inRect)
        {
            if (CustomNames.Count == 0) {
                foreach (var obj in Enum.GetValues(typeof(StoragePriority))) {
                    var storagePriority = (StoragePriority) obj;
                    CustomNames.Add((byte) storagePriority, storagePriority.Label().CapitalizeFirst());
                }
            }
            
            var outRect = inRect.TopPart(0.9f);
            var rect = new Rect(0f, 0f, outRect.width - 18f, 1500f);
            Widgets.BeginScrollView(outRect, ref this._scrollPosition, rect, true);
            var listingStandard = new Listing_Standard();
            listingStandard.Begin(rect);
            listingStandard.CheckboxLabeled("Lilith_NumericStoragePriority_DisableNames".Translate(), 
                                            ref DisableNames, 
                                            "Lilith_NumericStoragePriority_DisableNames_Tooltip".Translate());
            listingStandard.CheckboxLabeled("Lilith_NumericStoragePriority_DisableInput".Translate(), 
                                            ref DisableInput, 
                                            "Lilith_NumericStoragePriority_DisableInput_Tooltip".Translate());
            if (listingStandard.ButtonText("Lilith_NumericStoragePriority_Sort".Translate() + ": " + Sort)) {
                var opts = Enum.GetValues(typeof(SortDirection)).Cast<SortDirection>().Select(e => new FloatMenuOption(e.ToString(), () => Sort = e)).ToList();
                Find.WindowStack.Add(new FloatMenu(opts));
            }
            
            _search = listingStandard.TextEntryLabeled("Lilith_NumericStoragePriority_CustomNames_Search".Translate() + ": ", _search).ToLowerInvariant();
            
            var free = FindFreePresetNumber();
            if (free.HasValue) {
                if (listingStandard.ButtonText("Lilith_NumericStoragePriority_CustomNames_Add".Translate())) {
                    CustomNames.Add(free.Value, "Lilith_NumericStoragePriority_CustomNames_DefaultName".Translate());
                }
            }
            listingStandard.Gap();
            
            var performUpdate = false;
            byte? deleteName = null;
            foreach (var customName in CustomNames) {
                if (!customName.Value.ToLower(CultureInfo.CurrentUICulture).Contains(_search)
                    && !customName.Key.ToString(CultureInfo.CurrentUICulture).Contains(_search)) {
                    continue;
                }

                var newPriority = _lastEditedPriority == customName.Key ? _newPriority.ToString() : customName.Key.ToString();
                var newName = _lastEditedPriority == customName.Key ? _newName : customName.Value;
                
                newPriority = listingStandard.TextEntryLabeled("Lilith_NumericStoragePriority_CustomNames_Priority".Translate() + ": ", newPriority);
                newName = listingStandard.TextEntryLabeled("Lilith_NumericStoragePriority_CustomNames_Name".Translate() + ": ", newName);

                if (!byte.TryParse(newPriority, out var newPriorityByte)) {
                    newPriorityByte = (byte) customName.Key;
                }
                
                if (_lastEditedPriority == customName.Key 
                    || newPriorityByte != customName.Key 
                    || newName != customName.Value) {
                    _lastEditedPriority = (byte) customName.Key;
                    _newPriority = newPriorityByte;
                    _newName = newName;
                }

                if (_lastEditedPriority == customName.Key) {
                    if (listingStandard.ButtonText("Lilith_NumericStoragePriority_CustomNames_Confirm".Translate())) {
                        performUpdate = true;
                    }
                }

                if (listingStandard.ButtonText("Lilith_NumericStoragePriority_CustomNames_Delete".Translate())) {
                    deleteName = (byte) customName.Key;
                }

                listingStandard.Gap();
            }
            listingStandard.End();
            Widgets.EndScrollView();

            if (performUpdate) {
                CustomNames.Remove(_lastEditedPriority.Value);
                CustomNames[_newPriority] = _newName;
                _lastEditedPriority = null;
                _newPriority = 0;
                _newName = string.Empty;
            }

            if (deleteName.HasValue) {
                CustomNames.Remove(deleteName.Value);
            }
        }

        private byte? FindFreePresetNumber() {
            for (byte value = 0; value < byte.MaxValue; value++) {
                if (!CustomNames.ContainsKey(value)) {
                    return value;
                }
            }

            return null;
        }
    }
}