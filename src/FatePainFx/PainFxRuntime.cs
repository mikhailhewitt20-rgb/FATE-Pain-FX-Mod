using System;
using System.IO;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace MikhailKillsZombies.FatePainFx
{
    internal sealed class PainFxRuntime : MonoBehaviour
    {
        private static PainFxRuntime instance;
        internal static string PluginFolder;
        private Canvas canvas;
        private Image overlay;
        private Sprite sprite;
        private Texture2D texture;
        private float fadeStarted = -1f;
        private float nextMenuAttempt;
        private object menuModule;
        private ManualLogSource log;
        private int loadedDesign = -1;

        private static readonly string[] FileNames = {
            "Effect_1.png", "Effect_2.png", "Effect_3.png", "Effect_4.png", "Effect_5.png",
            "Effect_6.png", "Effect_7.png", "Effect_8.png", "Effect_9.png", "Effect_10.png",
            "Effect_11.png", "Blood_Blur_1.png", "Blood_Blur_2.png", "Blood_Drops.png",
            "Drip_1.png", "Drip_2.png", "Solid_Drip_1.png", "Solid_Drip_2.png", "Veins_1.png"
        };

        private static readonly int[] FirstStage = { 0, 2, 3, 4, 5, 6, 7, 8, 9, 11, 13, 14, 16, 18 };
        private static readonly int[] EvolvedStage = { 1, -1, -1, -1, -1, -1, -1, -1, 10, 12, -1, 15, 17, -1 };
        internal static readonly string[] DesignNames = {
            "Top Drips (Effect 1 → 2)", "Soft Fog", "Fine Splatter", "Dense Splatter",
            "Scattered Splatter", "Heavy Speckle", "Thick Splatter", "Soft Border",
            "Slash Burst (Effect 10 → 11)",
            "Blood Blur (1 → 2)", "Blood Drops", "Drips (1 → 2)", "Solid Drips (1 → 2)", "Veins"
        };

        private void Awake()
        {
            instance = this;
            log = BepInEx.Logging.Logger.CreateLogSource("FATE Pain FX");
            CreateOverlay();
        }

        private void Update()
        {
            UpdateFade();
            if (menuModule == null && Time.unscaledTime >= nextMenuAttempt)
            {
                nextMenuAttempt = Time.unscaledTime + 2f;
                RegisterMenuSettings();
            }
        }

        internal static void Pulse(object playerUi)
        {
            if (instance == null || !FatePainFxPlugin.Enabled.Value) return;
            var health = ReadHealthPercent(playerUi);
            instance.EnsureDesign(instance.ResolveDesign(health));
            instance.fadeStarted = Time.unscaledTime;
            if (instance.overlay != null)
                instance.overlay.color = new Color(1f, 1f, 1f, Mathf.Clamp01(FatePainFxPlugin.Opacity.Value));
        }

        private void CreateOverlay()
        {
            var canvasObject = new GameObject("Pain FX Canvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = short.MaxValue;

            var imageObject = new GameObject("Pain FX Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(canvasObject.transform, false);
            var rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            overlay = imageObject.GetComponent<Image>();
            overlay.raycastTarget = false;
            overlay.color = new Color(1f, 1f, 1f, 0f);
            EnsureDesign(ResolveDesign(1f));
        }

        private void UpdateFade()
        {
            if (fadeStarted < 0f || overlay == null) return;
            var duration = Mathf.Max(0.05f, FatePainFxPlugin.FadeDuration.Value);
            var remaining = 1f - ((Time.unscaledTime - fadeStarted) / duration);
            if (remaining <= 0f)
            {
                overlay.color = new Color(1f, 1f, 1f, 0f);
                fadeStarted = -1f;
                return;
            }
            overlay.color = new Color(1f, 1f, 1f, Mathf.Clamp01(FatePainFxPlugin.Opacity.Value * remaining));
        }

        private static float ReadHealthPercent(object playerUi)
        {
            try
            {
                var agentField = AccessTools.Field(playerUi.GetType(), "currentAgent");
                var agent = agentField?.GetValue(playerUi);
                var vitalsProperty = agent == null ? null : AccessTools.Property(agent.GetType(), "Vitals");
                var vitals = vitalsProperty?.GetValue(agent, null);
                var method = vitals == null ? null : AccessTools.Method(vitals.GetType(), "GetHealthPct");
                return method == null ? 1f : Mathf.Clamp01(Convert.ToSingle(method.Invoke(vitals, null)));
            }
            catch { return 1f; }
        }

        private int ResolveDesign(float health)
        {
            var group = Mathf.Clamp(FatePainFxPlugin.Design.Value, 0, FirstStage.Length - 1);
            if (FatePainFxPlugin.Design.Value != group) FatePainFxPlugin.Design.Value = group;
            var evolved = EvolvedStage[group];
            return FatePainFxPlugin.Evolution.Value && health <= 0.35f && evolved >= 0
                ? evolved : FirstStage[group];
        }

        private void EnsureDesign(int index)
        {
            if (overlay == null) return;
            index = Mathf.Clamp(index, 0, FileNames.Length - 1);
            if (loadedDesign == index && sprite != null) return;

            Destroy(sprite);
            Destroy(texture);
            sprite = null;
            texture = null;
            try
            {
                var path = Path.Combine(PluginFolder, "assets", FileNames[index]);
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false))
                    throw new InvalidDataException("Unity could not decode the PNG.");
                texture.name = FileNames[index];
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                sprite.name = FileNames[index];
                overlay.sprite = sprite;
                overlay.enabled = true;
                loadedDesign = index;
            }
            catch (Exception exception)
            {
                log?.LogWarning("Could not load " + FileNames[index] + ": " + exception.Message);
                overlay.sprite = null;
                loadedDesign = -1;
            }
        }

        private Texture2D SelectedTexture()
        {
            EnsureDesign(ResolveDesign(1f));
            return texture;
        }

        private void RegisterMenuSettings()
        {
            try
            {
                var api = AccessTools.TypeByName("ZombieGhost.ModMenu.ModMenuApi");
                var register = api?.GetMethod("RegisterModule", new[] { typeof(string), typeof(string) });
                if (register == null) return;

                menuModule = register.Invoke(null, new object[] { FatePainFxPlugin.Guid, "FATE Pain FX" });
                if (menuModule == null) return;
                var type = menuModule.GetType();
                type.GetMethod("RegisterToggle")?.Invoke(menuModule, new object[] {
                    "display.enabled", "Show damage overlay", new Func<bool>(() => FatePainFxPlugin.Enabled.Value),
                    new Action<bool>(value => FatePainFxPlugin.Enabled.Value = value), "Display" });
                type.GetMethod("RegisterToggle")?.Invoke(menuModule, new object[] {
                    "display.evolution", "Evolve paired design at low health", new Func<bool>(() => FatePainFxPlugin.Evolution.Value),
                    new Action<bool>(value => FatePainFxPlugin.Evolution.Value = value), "Display" });
                type.GetMethod("RegisterChoice")?.Invoke(menuModule, new object[] {
                    "display.design", "Design", new Func<int>(() => FatePainFxPlugin.Design.Value),
                    new Action<int>(value => { FatePainFxPlugin.Design.Value = value; EnsureDesign(ResolveDesign(1f)); }), DesignNames, "Display" });
                type.GetMethod("RegisterSlider")?.Invoke(menuModule, new object[] {
                    "display.opacity", "Opacity", new Func<float>(() => FatePainFxPlugin.Opacity.Value),
                    new Action<float>(value => FatePainFxPlugin.Opacity.Value = value), 0.1f, 1f, 0.05f, "Display", "0%" });
                type.GetMethod("RegisterSlider")?.Invoke(menuModule, new object[] {
                    "display.fade", "Fade time (seconds)", new Func<float>(() => FatePainFxPlugin.FadeDuration.Value),
                    new Action<float>(value => FatePainFxPlugin.FadeDuration.Value = value), 0.1f, 3f, 0.1f, "Display", "0.0" });
                type.GetMethod("RegisterPreview")?.Invoke(menuModule, new object[] {
                    "display.preview", "Selected design preview", new Func<Texture2D>(SelectedTexture), "Display", 180f });
                type.GetMethod("RegisterNote")?.Invoke(menuModule, new object[] {
                    "display.note", "Related artwork evolves to its paired variant when health falls to 35%. Other designs stay fixed. The preview shows the first stage.", "Display" });
            }
            catch (Exception exception)
            {
                log?.LogWarning("FATE Mod Menu settings registration failed: " + exception.Message);
                (menuModule as IDisposable)?.Dispose();
                menuModule = null;
                nextMenuAttempt = Time.unscaledTime + 30f;
            }
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
            (menuModule as IDisposable)?.Dispose();
            Destroy(sprite);
            Destroy(texture);
        }
    }
}
