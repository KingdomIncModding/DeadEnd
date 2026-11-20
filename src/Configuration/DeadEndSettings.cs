using System;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KingdomDeadEnd.Configuration
{
    public sealed class DeadEndSettings
    {
        public static readonly string[] StyleLabels =
        {
            "Outline",
            "Outline + Marker",
            "Marker Only"
        };

        public static readonly string[] ThicknessLabels =
        {
            "Thin (1px)",
            "Bold (2px)"
        };

        private readonly ConfigFile config;

        public ConfigEntry<bool> EnabledEntry { get; }
        public ConfigEntry<bool> HighlightInputsEntry { get; }
        public ConfigEntry<bool> HighlightOutputsEntry { get; }
        public ConfigEntry<string> InputColorHexEntry { get; }
        public ConfigEntry<string> OutputColorHexEntry { get; }
        public ConfigEntry<bool> ShowModBarButtonEntry { get; }
        public ConfigEntry<bool> HighlightJunctionsEntry { get; }
        public ConfigEntry<Key> ToggleKeyEntry { get; }
        public ConfigEntry<DeadEndIndicatorStyle> StyleEntry { get; }
        public ConfigEntry<DeadEndOutlineThickness> ThicknessEntry { get; }
        public ConfigEntry<float> OpacityEntry { get; }
        public ConfigEntry<bool> PulseEnabledEntry { get; }

        public bool Enabled => EnabledEntry.Value;
        public bool HighlightInputs => HighlightInputsEntry.Value;
        public bool HighlightOutputs => HighlightOutputsEntry.Value;
        public string InputColorHex => InputColorHexEntry.Value;
        public string OutputColorHex => OutputColorHexEntry.Value;
        public Color InputColor => DeadEndColorPreset.ResolveColor(InputColorHexEntry.Value);
        public Color OutputColor => DeadEndColorPreset.ResolveColor(OutputColorHexEntry.Value);
        public bool ShowModBarButton => ShowModBarButtonEntry.Value;
        public bool HighlightJunctions => HighlightJunctionsEntry.Value;
        public Key ToggleKey => ToggleKeyEntry.Value;
        public DeadEndIndicatorStyle Style => StyleEntry.Value;
        public DeadEndOutlineThickness Thickness => ThicknessEntry.Value;
        public float Opacity => Mathf.Clamp(OpacityEntry.Value, 0.3f, 1.0f);
        public bool PulseEnabled => PulseEnabledEntry.Value;

        public DeadEndSettings(ConfigFile config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));

            EnabledEntry = config.Bind(
                "General",
                "Enabled",
                true,
                "Highlight unconnected building ports.");

            ShowModBarButtonEntry = config.Bind(
                "General",
                "ShowModBarButton",
                true,
                "Show a quick-toggle button for Dead End in the top Mod Bar.");

            HighlightJunctionsEntry = config.Bind(
                "General",
                "HighlightJunctions",
                true,
                "Also highlight unconnected ports on Collectors, Distributors, and Manifolds.");

            HighlightInputsEntry = config.Bind(
                "InputPorts",
                "Enabled",
                true,
                "Enable highlighting for unconnected input ports.");

            InputColorHexEntry = config.Bind(
                "InputPorts",
                "Color",
                DeadEndColorPreset.DefaultHex,
                "Hex color for unconnected input port highlights (default: Orange #FF8C1A).");

            HighlightOutputsEntry = config.Bind(
                "OutputPorts",
                "Enabled",
                true,
                "Enable highlighting for unconnected output ports.");

            OutputColorHexEntry = config.Bind(
                "OutputPorts",
                "Color",
                DeadEndColorPreset.DefaultHex,
                "Hex color for unconnected output port highlights (default: Orange #FF8C1A).");

            ToggleKeyEntry = config.Bind(
                "Input",
                "ToggleKey",
                Key.None,
                "Optional keyboard shortcut to toggle Dead End highlights on or off.");

            StyleEntry = config.Bind(
                "Appearance",
                "IndicatorStyle",
                DeadEndIndicatorStyle.Outline,
                "Visual indicator style for unconnected ports.");

            ThicknessEntry = config.Bind(
                "Appearance",
                "OutlineThickness",
                DeadEndOutlineThickness.Bold,
                "Pixel-art thickness for the unconnected port outline.");

            OpacityEntry = config.Bind(
                "Appearance",
                "Opacity",
                1.0f,
                new ConfigDescription(
                    "Opacity of unconnected port highlights (0.3 to 1.0).",
                    new AcceptableValueRange<float>(0.3f, 1.0f)));

            PulseEnabledEntry = config.Bind(
                "Appearance",
                "PulseEnabled",
                false,
                "Subtly pulse unconnected port highlights so they stand out in dense layouts.");
        }

        public void SetEnabled(bool enabled)
        {
            if (EnabledEntry.Value == enabled)
                return;

            EnabledEntry.Value = enabled;
            Save();
        }

        public void SetHighlightInputs(bool enabled)
        {
            if (HighlightInputsEntry.Value == enabled)
                return;

            HighlightInputsEntry.Value = enabled;
            Save();
        }

        public void SetHighlightOutputs(bool enabled)
        {
            if (HighlightOutputsEntry.Value == enabled)
                return;

            HighlightOutputsEntry.Value = enabled;
            Save();
        }

        public void SetInputColorHex(string hex)
        {
            string normalized = DeadEndColorPreset.ToHex(DeadEndColorPreset.ResolveColor(hex));
            if (string.Equals(InputColorHexEntry.Value, normalized, StringComparison.OrdinalIgnoreCase))
                return;

            InputColorHexEntry.Value = normalized;
            Save();
        }

        public void SetOutputColorHex(string hex)
        {
            string normalized = DeadEndColorPreset.ToHex(DeadEndColorPreset.ResolveColor(hex));
            if (string.Equals(OutputColorHexEntry.Value, normalized, StringComparison.OrdinalIgnoreCase))
                return;

            OutputColorHexEntry.Value = normalized;
            Save();
        }

        public void SetShowModBarButton(bool visible)
        {
            if (ShowModBarButtonEntry.Value == visible)
                return;

            ShowModBarButtonEntry.Value = visible;
            Save();
        }

        public void SetHighlightJunctions(bool includeJunctions)
        {
            if (HighlightJunctionsEntry.Value == includeJunctions)
                return;

            HighlightJunctionsEntry.Value = includeJunctions;
            Save();
        }

        public void SetToggleKey(Key key)
        {
            if (ToggleKeyEntry.Value == key)
                return;

            ToggleKeyEntry.Value = key;
            Save();
        }

        public void SetStyleIndex(int index)
        {
            var next = index switch
            {
                1 => DeadEndIndicatorStyle.OutlineAndMarker,
                2 => DeadEndIndicatorStyle.MarkerOnly,
                _ => DeadEndIndicatorStyle.Outline
            };

            if (StyleEntry.Value == next)
                return;

            StyleEntry.Value = next;
            Save();
        }

        public int GetStyleIndex()
        {
            return StyleEntry.Value switch
            {
                DeadEndIndicatorStyle.OutlineAndMarker => 1,
                DeadEndIndicatorStyle.MarkerOnly => 2,
                _ => 0
            };
        }

        public void SetThicknessIndex(int index)
        {
            var next = index == 0 ? DeadEndOutlineThickness.Thin : DeadEndOutlineThickness.Bold;
            if (ThicknessEntry.Value == next)
                return;

            ThicknessEntry.Value = next;
            Save();
        }

        public int GetThicknessIndex()
        {
            return ThicknessEntry.Value == DeadEndOutlineThickness.Thin ? 0 : 1;
        }

        public void SetOpacityPercent(float percent)
        {
            float normalized = Mathf.Clamp(percent / 100f, 0.3f, 1.0f);
            if (Mathf.Approximately(OpacityEntry.Value, normalized))
                return;

            OpacityEntry.Value = normalized;
            Save();
        }

        public float GetOpacityPercent()
        {
            return Mathf.Round(Opacity * 100f);
        }

        public void SetPulseEnabled(bool pulse)
        {
            if (PulseEnabledEntry.Value == pulse)
                return;

            PulseEnabledEntry.Value = pulse;
            Save();
        }

        public string FormatHotkeyLabel()
        {
            return ToggleKey == Key.None ? "[ None ]" : $"[ {ToggleKey} ]";
        }

        private void Save()
        {
            config.Save();
        }
    }
}
