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
    //[HarmonyPatch(typeof(Scribe_Values))]
    //[HarmonyPatch(nameof(Scribe_Values.Look))]
    [HarmonyPatch]
    [UsedImplicitly]
    public class Scribe_Values_Look {
        [UsedImplicitly]
        public static MethodInfo TargetMethod() {
            return typeof(Scribe_Values).GetMethod(nameof(Scribe_Values.Look))?.MakeGenericMethod(typeof(StoragePriority));
        }
        
        [UsedImplicitly]
        public static bool Prefix(ref StoragePriority value, string label, StoragePriority defaultValue, bool forceSave) {
            // Ensure that the enum is correct
            /*if (typeof(T) != typeof(StoragePriority)) {
                return true;
            }*/

            var storagePriority = value as StoragePriority?;
            var defaultPriority = defaultValue as StoragePriority?;
            var numericLabel = label + "Numeric";

            // Only then save the values. Also save in the old format to allow users to uninstall this mod at any time
            switch (Scribe.mode) {
                case LoadSaveMode.Saving: {
                    if (!forceSave && storagePriority.Equals(defaultPriority)) {
                        return false;
                    }

                    if (!storagePriority.HasValue) {
                        if (!Scribe.EnterNode(label)) {
                            return false;
                        }
                        try {
                            Scribe.saver.WriteAttribute("IsNull", "True");
                            return false;
                        } finally {
                            Scribe.ExitNode();
                        }
                    }
                    
                    // Here we differ from the original implementation, as we check if we are still a named priority or not.
                    var compatStoragePriority = storagePriority.Value;
                    if (!Enum.IsDefined(typeof(StoragePriority), compatStoragePriority)) {
                        compatStoragePriority = StoragePriority.Critical;
                    }
                    Scribe.saver.WriteElement(label, compatStoragePriority.ToString());
                    // Save numeric value, this is what we will load with this mod later on if it exists
                    Scribe.saver.WriteElement(numericLabel, ((byte) storagePriority.Value).ToString());
                    return false;
                }
                case LoadSaveMode.LoadingVars: {
                    var numericNode = Scribe.loader.curXmlParent[numericLabel];
                    // The save was saved using this mod, load the numeric value
                    if (numericNode != null) {
                        // RimWorld does not have a ParseHelper for unsigned bytes, so load it as an int instead
                        value = (StoragePriority) ScribeExtractor.ValueFromNode(numericNode, defaultPriority.HasValue ? (int) defaultPriority.Value : 0);
                    } else {
                        value = ScribeExtractor.ValueFromNode<StoragePriority>(Scribe.loader.curXmlParent[label], defaultValue);
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