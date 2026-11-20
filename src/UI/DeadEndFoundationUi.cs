using System;
using KingdomDeadEnd.Configuration;
using KingdomDeadEnd.Core;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomDeadEnd.UI
{
    internal sealed class DeadEndFoundationUi : IDisposable
    {
        private readonly DeadEndPlugin plugin;
        private FoundationModContext context;
        private DeadEndModBarElement barElement;
        private IModBarRegistration barRegistration;
        private DeadEndModBarView activeBarView;
        private IModConfigButton hotkeyButton;

        public DeadEndFoundationUi(DeadEndPlugin plugin)
        {
            this.plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        }

        public void Register()
        {
            try
            {
                context = FoundationApi.ForMod(DeadEndPlugin.PluginId);
                RegisterSettings();
                RegisterModBar();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void SyncModBar()
        {
            if (barElement != null)
                barElement.IsVisible = plugin.Settings.ShowModBarButton;

            activeBarView?.Refresh();
            barRegistration?.Refresh();
        }

        public void UpdateHotkeyButtonText(string text)
        {
            if (hotkeyButton != null)
                hotkeyButton.ButtonText = text;
        }

        internal void SetActiveBarView(DeadEndModBarView view)
        {
            activeBarView = view;
        }

        internal void ClearActiveBarView(DeadEndModBarView view)
        {
            if (ReferenceEquals(activeBarView, view))
                activeBarView = null;
        }

        public void Dispose()
        {
            DeadEndColorPickerPopup.CloseActive();
            context?.Dispose();
            context = null;
            barElement = null;
            barRegistration = null;
            activeBarView = null;
            hotkeyButton = null;
        }

        private void RegisterSettings()
        {
            var settings = plugin.Settings;
            var section = context.Settings(
                "Dead End",
                "Highlights unconnected building input and output ports with customizable colors without altering existing port colors or glow effects.");

            section.AddText("General", isHeader: true);

            section.AddToggle(
                "Enable Highlights",
                settings.Enabled,
                val => plugin.SetEnabled(val, fromUser: false),
                getter: () => settings.Enabled);

            section.AddToggle(
                "Show Mod Bar Button",
                settings.ShowModBarButton,
                val =>
                {
                    settings.SetShowModBarButton(val);
                    SyncModBar();
                },
                getter: () => settings.ShowModBarButton);

            section.AddToggle(
                "Highlight Junctions",
                settings.HighlightJunctions,
                val =>
                {
                    settings.SetHighlightJunctions(val);
                    plugin.RefreshAllOverlays(force: true);
                },
                getter: () => settings.HighlightJunctions);

            hotkeyButton = section.AddButton(
                "Toggle Hotkey",
                () => plugin.StartListeningForHotkey(),
                buttonText: settings.FormatHotkeyLabel());

            section.AddText("Input Ports", isHeader: true);

            section.AddToggle(
                "Highlight Inputs",
                settings.HighlightInputs,
                val =>
                {
                    settings.SetHighlightInputs(val);
                    plugin.RefreshAllOverlays(force: true);
                },
                getter: () => settings.HighlightInputs);

            section.AddElement(new DeadEndColorPickerRow(
                "Input Color",
                settings.InputColorHex,
                hex =>
                {
                    settings.SetInputColorHex(hex);
                    plugin.RefreshAllOverlays(force: true);
                },
                () => settings.InputColorHex));

            section.AddText("Output Ports", isHeader: true);

            section.AddToggle(
                "Highlight Outputs",
                settings.HighlightOutputs,
                val =>
                {
                    settings.SetHighlightOutputs(val);
                    plugin.RefreshAllOverlays(force: true);
                },
                getter: () => settings.HighlightOutputs);

            section.AddElement(new DeadEndColorPickerRow(
                "Output Color",
                settings.OutputColorHex,
                hex =>
                {
                    settings.SetOutputColorHex(hex);
                    plugin.RefreshAllOverlays(force: true);
                },
                () => settings.OutputColorHex));

            section.AddText("Appearance", isHeader: true);

            section.AddDropdown(
                "Indicator Style",
                DeadEndSettings.StyleLabels,
                settings.GetStyleIndex(),
                idx =>
                {
                    settings.SetStyleIndex(idx);
                    plugin.RefreshAllOverlays(force: true);
                },
                getter: () => settings.GetStyleIndex());

            section.AddDropdown(
                "Outline Thickness",
                DeadEndSettings.ThicknessLabels,
                settings.GetThicknessIndex(),
                idx =>
                {
                    settings.SetThicknessIndex(idx);
                    plugin.RefreshAllOverlays(force: true);
                },
                getter: () => settings.GetThicknessIndex());

            section.AddSlider(
                "Highlight Opacity",
                30f,
                100f,
                settings.GetOpacityPercent(),
                val =>
                {
                    settings.SetOpacityPercent(val);
                    plugin.RefreshAllOverlays(force: true);
                },
                step: 5f,
                getter: () => settings.GetOpacityPercent(),
                formatValue: val => $"{Mathf.RoundToInt(val)}%");

            section.AddToggle(
                "Gentle Pulse Effect",
                settings.PulseEnabled,
                val =>
                {
                    settings.SetPulseEnabled(val);
                    plugin.RefreshAllOverlays(force: true);
                },
                getter: () => settings.PulseEnabled);
        }

        private void RegisterModBar()
        {
            if (FoundationApi.ModBar == null)
                return;

            barElement = new DeadEndModBarElement(plugin, this)
            {
                IsVisible = plugin.Settings.ShowModBarButton
            };

            barRegistration = context.Register(barElement);
        }
    }
}
