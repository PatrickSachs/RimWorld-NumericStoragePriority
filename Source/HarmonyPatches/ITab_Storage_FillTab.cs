using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace Lilith.RimWorld.NumericStoragePriority.HarmonyPatches {
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [HarmonyPatch]
    public class ITab_Storage_FillTab {
        private static readonly PropertyInfo SelStoreSettingsParent =
            typeof(ITab_Storage).GetProperty(nameof(SelStoreSettingsParent), BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly PropertyInfo TopAreaHeight = 
            typeof(ITab_Storage).GetProperty(nameof(TopAreaHeight), BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo WinSize = 
            typeof(ITab_Storage).GetField(nameof(WinSize), BindingFlags.Static | BindingFlags.NonPublic);
        
        [UsedImplicitly]
        public static MethodInfo TargetMethod() {
            return typeof(ITab_Storage).GetMethod("FillTab", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [UsedImplicitly]
        public static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> instructions) {
            var instructionsList = instructions.AsList();
            // if (IsPrioritySettingVisible)
            var getIsPrioritySettingVisible = typeof(ITab_Storage).GetProperty("IsPrioritySettingVisible", BindingFlags.NonPublic | BindingFlags.Instance)?.GetMethod;
            var startIdx = instructionsList.FindSequenceIndex(
                                                         instr => instr.opcode == OpCodes.Callvirt && ReferenceEquals(instr.operand, getIsPrioritySettingVisible),
                                                         instr => instr.opcode == OpCodes.Brfalse) + 2;
            #if DEBUG
            Log.Message("[LILITH STORAGE PRIORITY] startIdx " + startIdx + " getIsPrioritySettingVisible " + getIsPrioritySettingVisible);
            #endif
            
            // UIHighlighter.HighlightOpportunity(?, "StoragePriority")
            var highlightOpportunity = typeof(UIHighlighter).GetMethod(nameof(UIHighlighter.HighlightOpportunity), BindingFlags.Public | BindingFlags.Static);
            var endIdx = instructionsList.FindSequenceIndex(
                                                            instr => instr.opcode == OpCodes.Ldstr && instr.operand is "StoragePriority",
                                                            instr => instr.opcode == OpCodes.Call && ReferenceEquals(instr.operand, highlightOpportunity)) + 2;
            #if DEBUG
            Log.Message("[LILITH STORAGE PRIORITY] endIdx " + endIdx + " highlightOpportunity " + highlightOpportunity);
            #endif
            
            // ITab_Storage_FillTab.DrawGUI(this)
            var drawGui = typeof(ITab_Storage_FillTab).GetMethod(nameof(DrawGUI), BindingFlags.Public | BindingFlags.Static);
            #if DEBUG
            Log.Message("[LILITH STORAGE PRIORITY] drawGui " + drawGui);
            #endif
            instructionsList.SafeReplaceRange(startIdx, endIdx - startIdx, new [] {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, drawGui),
            });

            return instructionsList;
        }
        
        public static void DrawGUI(ITab_Storage tabStorage) {
            var CONTRACTED = 10;
            var SPACE = 5;
            Text.Font = GameFont.Small;
            var storeSettingsParent = (IStoreSettingsParent) SelStoreSettingsParent.GetValue(tabStorage);
            var settings = storeSettingsParent.GetStoreSettings();
            var topAreaHeight = (float) TopAreaHeight.GetValue(tabStorage);
            var winSize = ((Vector2) WinSize.GetValue(null)).x - CONTRACTED * 2 - SPACE;
            var rect2 = new Rect(0f, 0f, winSize / 2, topAreaHeight - 6f);
            var priorityByte = (byte) settings.Priority;
            if (Widgets.ButtonText(rect2, "Priority".Translate() + ": " + StoragePriorityName(priorityByte))) {
                var list = BuildOptions(settings);
                Find.WindowStack.Add(new FloatMenu(list));
            }

            if (!NumericStoragePriorityMod.Settings.DisableInput) {
                var rect3 = new Rect(winSize / 2 + SPACE, 0f, winSize / 2, topAreaHeight - 6f);
                var buffer = priorityByte.ToString();
                var value = (int) priorityByte;
                Widgets.TextFieldNumeric(rect3, ref value, ref buffer, byte.MinValue, byte.MaxValue);
                var newPriority = (StoragePriority) (byte) value;
                if (newPriority != settings.Priority) {
                    settings.Priority = newPriority;
                }
            }

            UIHighlighter.HighlightOpportunity(rect2, "StoragePriority");
        }

        private static List<FloatMenuOption> BuildOptions(StorageSettings settings) {
            var modSettings = NumericStoragePriorityMod.Settings;
            var enumerable = modSettings.CustomNames.AsEnumerable();
            switch (modSettings.Sort) {
                case NumericStoragePrioritySettings.SortDirection.Original:
                    break;
                case NumericStoragePrioritySettings.SortDirection.NumericDesc:
                    enumerable = enumerable.OrderByDescending(e => e.Key);
                    break;
                case NumericStoragePrioritySettings.SortDirection.NumericAsc:
                    enumerable = enumerable.OrderBy(e => e.Key);
                    break;
                case NumericStoragePrioritySettings.SortDirection.AlphaDesc:
                    enumerable = enumerable.OrderByDescending(e => e.Value);
                    break;
                case NumericStoragePrioritySettings.SortDirection.AlphaAsc:
                    enumerable = enumerable.OrderBy(e => e.Value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return enumerable
                   .Select(name => BuildOption((byte) name.Key, settings))
                   .ToList();
        }

        private static FloatMenuOption BuildOption(byte priority, StorageSettings settings) {
            return new FloatMenuOption(StoragePriorityName(priority), () => settings.Priority = (StoragePriority) (byte) priority);
        }

        private static string StoragePriorityName(byte priority) {
            var settings = NumericStoragePriorityMod.Settings;
            if (!settings.DisableNames && settings.CustomNames.TryGetValue(priority, out var customName)) {
                return customName;
            }

            return priority.ToString();
        }
    }
}