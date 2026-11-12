using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace MikhailKillsZombies.FatePainFx
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency("zombieghost.modmenu", BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class FatePainFxPlugin : BaseUnityPlugin
    {
        public const string Guid = "mikhailkillszombies.fatepainfx";
        public const string Name = "FATE Pain FX";
        public const string Version = "0.1.0";

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<int> Design;
        internal static ConfigEntry<bool> Evolution;
        internal static ConfigEntry<float> Opacity;
        internal static ConfigEntry<float> FadeDuration;
        private Harmony harmony;

        private void Awake()
        {
            Enabled = Config.Bind("Display", "Enabled", true, "Show a screen effect when the local player takes damage.");
            Design = Config.Bind("Display", "Design", 0, "Selected overlay family. Change this in FATE Mod Menu.");
            Evolution = Config.Bind("Display", "Evolution", true, "Use the related stronger variant when health is low.");
            Opacity = Config.Bind("Display", "Opacity", 0.72f, "Maximum overlay opacity, from 0 to 1.");
            FadeDuration = Config.Bind("Display", "FadeDuration", 0.8f, "Seconds for the overlay to fade away.");

            PainFxRuntime.PluginFolder = Path.GetDirectoryName(Info.Location);
            var runner = new GameObject("FATE Pain FX Runtime");
            DontDestroyOnLoad(runner);
            runner.AddComponent<PainFxRuntime>();

            harmony = new Harmony(Guid);
            var target = AccessTools.Method(typeof(Tableflip.PlayerUI), "Vitals_OnDamaged");
            if (target != null)
                harmony.Patch(target, postfix: new HarmonyMethod(typeof(FatePainFxPlugin), nameof(AfterPlayerDamaged)));
            else
                Logger.LogWarning("PlayerUI.Vitals_OnDamaged was not found; damage overlays are unavailable.");
            Logger.LogInfo("FATE Pain FX loaded. Configure the design in FATE Mod Menu.");
        }

        private static void AfterPlayerDamaged(object __instance) => PainFxRuntime.Pulse(__instance);

    }
}
