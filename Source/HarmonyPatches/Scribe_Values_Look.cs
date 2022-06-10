using System;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace Lilith.RimWorld.NumericStoragePriority.HarmonyPatches {
    /// <summary>
    /// Super low level patch overriding how a very specific enum is saved.
    /// </summary>
    [HarmonyPatch]
    [UsedImplicitly]
    public class Scribe_Values_Look {
        [UsedImplicitly]
        public static MethodInfo TargetMethod() {
            return typeof(Scribe_Values).GetMethod(nameof(Scribe_Values.Look))?.MakeGenericMethod(typeof(StoragePriority));
        }
        
        [UsedImplicitly]
        public static bool Prefix(ref StoragePriority value, string label, StoragePriority defaultValue, bool forceSave) {
            var numericLabel = label + "Numeric";

            // Only then save the values. Also save in the old format to allow users to uninstall this mod at any time
            switch (Scribe.mode) {
                case LoadSaveMode.Saving: {
                    if (!forceSave && value == defaultValue) {
                        return false;
                    }
                    
                    // Here we differ from the original implementation, as we check if we are still a named priority or not.
                    var compatStoragePriority = value;
                    if (!Enum.IsDefined(typeof(StoragePriority), compatStoragePriority)) {
                        compatStoragePriority = StoragePriority.Critical;
                    }
                    Scribe.saver.WriteElement(label, compatStoragePriority.ToString());
                    // Save numeric value, this is what we will load with this mod later on if it exists
                    Scribe.saver.WriteElement(numericLabel, ((byte) value).ToString());
                    return false;
                }
                case LoadSaveMode.LoadingVars: {
                    var numericNode = Scribe.loader.curXmlParent[numericLabel];
                    // The save was saved using this mod, load the numeric value
                    if (numericNode != null) {
                        // RimWorld does not have a ParseHelper for unsigned bytes, so load it as an int instead
                        value = (StoragePriority) ScribeExtractor.ValueFromNode(numericNode, (int) defaultValue);
                    } else {
                        value = ScribeExtractor.ValueFromNode(Scribe.loader.curXmlParent[label], defaultValue);
                    }
                    return false;
                }
                default:
                    // RimWorld does nothing in this case, but just in case...
                    return true;
            }
        }
    }
}