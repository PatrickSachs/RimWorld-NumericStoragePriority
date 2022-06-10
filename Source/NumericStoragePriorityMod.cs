using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace Lilith.RimWorld.NumericStoragePriority {
    /// <summary>
    /// Main mod class.
    /// </summary>
    [UsedImplicitly]
    public class NumericStoragePriorityMod : Mod {
        /// <summary>
        /// Harmony instance.
        /// </summary>
        public static Harmony Harm;
        /// <summary>
        /// The mod settings.
        /// </summary>
        public static NumericStoragePrioritySettings Settings;
        
        public NumericStoragePriorityMod(ModContentPack content) : base(content) {
            Harm = new Harmony("lilith.numericstoragepriority");
            #if DEBUG
            Harmony.DEBUG = true;
            #endif
            Harm.PatchAll();
            Settings = GetSettings<NumericStoragePrioritySettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect) {
            Settings.DoSettingsWindowContents(inRect);
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory() {
            return Settings.SettingsCategory();
        }
    }
}