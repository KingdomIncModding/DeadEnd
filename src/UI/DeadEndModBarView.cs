using KingdomDeadEnd.Core;
using KingdomFoundation.Api;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomDeadEnd.UI
{
    internal sealed class DeadEndModBarView : IModBarView
    {
        private readonly DeadEndPlugin plugin;
        private readonly DeadEndFoundationUi owner;
        private readonly Button button;
        private readonly Image image;
        private readonly TMP_Text label;

        public DeadEndModBarView(DeadEndPlugin plugin, DeadEndFoundationUi owner, IModBarContext context)
        {
            this.plugin = plugin;
            this.owner = owner;

            button = context.CreateButton(BuildLabelText(), OnClick);
            image = button.GetComponent<Image>();
            label = button.GetComponentInChildren<TMP_Text>(true);

            ConfigureLabel();
            Refresh();
        }

        public void Refresh()
        {
            bool enabled = plugin.Settings.Enabled;

            if (label != null)
            {
                label.text = BuildLabelText();
                label.color = enabled
                    ? new Color(1.00f, 0.90f, 0.74f, 1f)
                    : new Color(0.72f, 0.72f, 0.75f, 1f);
            }

            if (button != null && image != null)
            {
                Color baseTint = enabled
                    ? new Color(1.00f, 0.86f, 0.68f, 1f)
                    : new Color(0.75f, 0.75f, 0.78f, 0.9f);

                ColorBlock colors = button.colors;
                colors.normalColor = baseTint;
                colors.highlightedColor = enabled
                    ? new Color(1.00f, 0.92f, 0.78f, 1f)
                    : new Color(0.85f, 0.85f, 0.88f, 1f);
                colors.selectedColor = baseTint;
                button.colors = colors;
                image.color = baseTint;
            }
        }

        public void Dispose()
        {
            owner.ClearActiveBarView(this);
        }

        private void ConfigureLabel()
        {
            if (label == null)
                return;

            label.enableAutoSizing = true;
            label.fontSizeMin = 13;
            label.fontSizeMax = 18;
            label.overflowMode = TextOverflowModes.Overflow;
            label.rectTransform.offsetMin = new Vector2(4f, 2f);
            label.rectTransform.offsetMax = new Vector2(-4f, -2f);
        }

        private string BuildLabelText()
        {
            return plugin.Settings.Enabled ? "Dead End: ON" : "Dead End: OFF";
        }

        private void OnClick()
        {
            plugin.ToggleEnabled(fromUser: true);
        }
    }
}
