using System;
using BepInEx;
using KingdomDeadEnd.Configuration;
using KingdomDeadEnd.Patches;
using KingdomDeadEnd.Rendering;
using KingdomDeadEnd.UI;
using KingdomFoundation.Api;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace KingdomDeadEnd.Core
{
    [BepInPlugin(PluginId, PluginName, PluginVersion)]
    [BepInDependency("urakka.kingdominc.foundation", "0.14.4")]
    public sealed class DeadEndPlugin : BaseUnityPlugin
    {
        public const string PluginId = "urakka.kingdominc.deadend";
        public const string PluginName = "Dead End";
        public const string PluginVersion = "0.1.1";

        private const float SweepIntervalSeconds = 3.0f;

        public static DeadEndPlugin Instance { get; private set; }
        public DeadEndSettings Settings { get; private set; }

        private DeadEndHooks hooks;
        private DeadEndFoundationUi foundationUi;
        private float nextSweepTime;
        private float lastToggleTime;
        private bool listeningForHotkey;

        private void Awake()
        {
            Instance = this;
            Settings = new DeadEndSettings(Config);

            hooks = new DeadEndHooks(this);
            hooks.Install();

            foundationUi = new DeadEndFoundationUi(this);
            foundationUi.Register();

            Logger.LogInfo($"{PluginName} v{PluginVersion} loaded.");
        }

        private void Start()
        {
            SweepAndAttachOverlays();
        }

        private void Update()
        {
            if (listeningForHotkey)
            {
                HandleHotkeyCapture();
                return;
            }

            HandleToggleHotkey();

            if (Time.unscaledTime >= nextSweepTime)
            {
                nextSweepTime = Time.unscaledTime + SweepIntervalSeconds;
                SweepAndAttachOverlays();
            }
        }

        private void OnDestroy()
        {
            hooks?.Dispose();
            hooks = null;

            foundationUi?.Dispose();
            foundationUi = null;

            DestroyAllOverlays();
            DeadEndSpriteFactory.Release();

            if (Instance == this)
                Instance = null;
        }

        public void ToggleEnabled(bool fromUser)
        {
            SetEnabled(!Settings.Enabled, fromUser);
        }

        public void SetEnabled(bool enabled, bool fromUser)
        {
            Settings.SetEnabled(enabled);
            foundationUi?.SyncModBar();
            RefreshAllOverlays(force: true);

            if (fromUser)
            {
                try
                {
                    FoundationApi.UiAccess.ShowStatusMessage(
                        enabled ? "Dead End: Unconnected Ports Highlighted" : "Dead End: Highlights Hidden");
                }
                catch
                {
                }
            }
        }

        public void StartListeningForHotkey()
        {
            listeningForHotkey = true;
            foundationUi?.UpdateHotkeyButtonText("[ Press Key... ]");
        }

        public void SweepAndAttachOverlays()
        {
            try
            {
                if (!FoundationApi.WorkspaceAccess.IsReady)
                    return;

                var objects = FoundationApi.WorkspaceAccess.AllObjects;
                for (int i = 0; i < objects.Count; i++)
                {
                    var item = objects[i];
                    if (item == null || item.Kind != WorkspaceObjectKind.Building || !item.World)
                        continue;

                    var inputs = item.Inputs;
                    for (int p = 0; p < inputs.Count; p++)
                    {
                        var port = inputs[p];
                        if (port != null && port.World)
                            DeadEndPortOverlay.Ensure(port.World, port);
                    }

                    var outputs = item.Outputs;
                    for (int p = 0; p < outputs.Count; p++)
                    {
                        var port = outputs[p];
                        if (port != null && port.World)
                            DeadEndPortOverlay.Ensure(port.World, port);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug($"SweepAndAttachOverlays error: {ex.Message}");
            }
        }

        public void RefreshAllOverlays(bool force)
        {
            try
            {
                var overlays = FindObjectsByType<DeadEndPortOverlay>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < overlays.Length; i++)
                {
                    if (overlays[i])
                        overlays[i].RefreshVisuals(force);
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug($"RefreshAllOverlays error: {ex.Message}");
            }
        }

        private void HandleHotkeyCapture()
        {
            if (Keyboard.current == null)
                return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                FinishHotkeyCapture();
                return;
            }

            if (Keyboard.current.backspaceKey.wasPressedThisFrame || Keyboard.current.deleteKey.wasPressedThisFrame)
            {
                Settings.SetToggleKey(Key.None);
                FinishHotkeyCapture();
                return;
            }

            foreach (var key in Keyboard.current.allKeys)
            {
                if (key == null || !key.wasPressedThisFrame)
                    continue;

                if (key.keyCode == Key.None || key.keyCode == Key.Escape)
                    continue;

                Settings.SetToggleKey(key.keyCode);
                FinishHotkeyCapture();
                return;
            }
        }

        private void FinishHotkeyCapture()
        {
            listeningForHotkey = false;
            foundationUi?.UpdateHotkeyButtonText(Settings.FormatHotkeyLabel());
        }

        private void HandleToggleHotkey()
        {
            if (Settings.ToggleKey == Key.None || Keyboard.current == null)
                return;

            if (!CanHandleHotkey())
                return;

            var control = Keyboard.current[Settings.ToggleKey];
            if (control == null || !control.wasPressedThisFrame)
                return;

            if (Time.unscaledTime - lastToggleTime < 0.25f)
                return;

            lastToggleTime = Time.unscaledTime;
            ToggleEnabled(fromUser: true);
        }

        private bool CanHandleHotkey()
        {
            if (!Application.isFocused)
                return false;

            var ui = FoundationApi.UiAccess;
            if (!ui.IsGameInputEnabled || !ui.IsWorkspaceVisible || ui.IsMenuVisible || ui.IsAlertVisible || ui.HasPopup || ui.IsTutorialActive)
                return false;

            return !IsTextInputFocused();
        }

        private static bool IsTextInputFocused()
        {
            var selected = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            if (!selected)
                return false;

            var tmp = selected.GetComponentInParent<TMP_InputField>();
            if (tmp && tmp.isFocused)
                return true;

            var legacy = selected.GetComponentInParent<InputField>();
            return legacy && legacy.isFocused;
        }

        private static void DestroyAllOverlays()
        {
            var overlays = FindObjectsByType<DeadEndPortOverlay>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < overlays.Length; i++)
            {
                if (overlays[i] && overlays[i].gameObject.scene.IsValid())
                    Destroy(overlays[i]);
            }
        }
    }
}
