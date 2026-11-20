using System;
using System.Collections.Generic;
using KingdomDeadEnd.Configuration;
using KingdomFoundation.Api;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomDeadEnd.UI
{
    internal sealed class DeadEndColorPickerRow : IModConfigElement
    {
        private readonly string label;
        private readonly Func<string> getter;
        private readonly Action<string> onChanged;
        private readonly List<(string Hex, Outline Outline)> swatchOutlines = new List<(string, Outline)>();

        private string committedHex;
        private string pendingHex;
        private Image previewSwatch;
        private TMP_Text hexButtonLabel;
        private TMP_FontAsset nativeFont;
        private Material nativeFontMaterial;

        public string Id { get; }
        public bool IsVisible { get; set; } = true;

        public DeadEndColorPickerRow(string label, string initialHex, Action<string> onChanged, Func<string> getter)
        {
            this.label = label ?? "Color";
            this.onChanged = onChanged;
            this.getter = getter;
            committedHex = NormalizeHex(initialHex);
            pendingHex = committedHex;
            Id = Guid.NewGuid().ToString("N");
        }

        public GameObject CreateView(IModConfigContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            swatchOutlines.Clear();
            committedHex = NormalizeHex(getter != null ? getter() : committedHex);
            pendingHex = committedHex;

            var root = context.Templates.CreateEmptyRow(context.Container, 88f);
            root.name = "DeadEndColorPickerRow";
            root.SetActive(IsVisible);

            var stack = root.AddComponent<VerticalLayoutGroup>();
            stack.spacing = 4f;
            stack.padding = new RectOffset(0, 0, 2, 4);
            stack.childControlWidth = true;
            stack.childControlHeight = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;

            BuildHeaderRow(root.transform, context);
            BuildSwatchStrip(root.transform, context);
            RefreshPreview();

            return root;
        }

        public void Commit()
        {
            if (string.Equals(pendingHex, committedHex, StringComparison.OrdinalIgnoreCase))
                return;

            onChanged?.Invoke(pendingHex);
            committedHex = NormalizeHex(getter != null ? getter() : pendingHex);
            pendingHex = committedHex;
            RefreshPreview();
        }

        public void Cancel()
        {
            DeadEndColorPickerPopup.CloseActive();
            committedHex = NormalizeHex(getter != null ? getter() : committedHex);
            pendingHex = committedHex;
            RefreshPreview();
        }

        private void BuildHeaderRow(Transform parent, IModConfigContext context)
        {
            GameObject row = context.Templates.CreateButtonRow(
                parent,
                label,
                pendingHex,
                () => OpenPicker(parent),
                out Button pickButton,
                out TMP_Text buttonLabel);

            var rowLayout = row.GetComponent<LayoutElement>();
            rowLayout.preferredHeight = 46f;
            rowLayout.minHeight = 46f;
            ((RectTransform)row.transform).sizeDelta = new Vector2(0f, 46f);

            hexButtonLabel = buttonLabel;
            if (buttonLabel != null)
            {
                nativeFont = buttonLabel.font;
                nativeFontMaterial = buttonLabel.fontSharedMaterial;
            }

            if (pickButton != null)
            {
                var btnRect = (RectTransform)pickButton.transform;
                btnRect.anchorMin = new Vector2(0.56f, 0.08f);
                btnRect.anchorMax = new Vector2(0.98f, 0.92f);
                btnRect.offsetMin = Vector2.zero;
                btnRect.offsetMax = Vector2.zero;
            }

            var previewObj = new GameObject("ColorPreview", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            previewObj.transform.SetParent(row.transform, false);

            var previewRect = (RectTransform)previewObj.transform;
            previewRect.anchorMin = new Vector2(0.45f, 0.14f);
            previewRect.anchorMax = new Vector2(0.54f, 0.86f);
            previewRect.offsetMin = Vector2.zero;
            previewRect.offsetMax = Vector2.zero;

            previewSwatch = previewObj.GetComponent<Image>();

            var previewOutline = previewObj.GetComponent<Outline>();
            previewOutline.effectColor = new Color(0.85f, 0.72f, 0.40f, 1f);
            previewOutline.effectDistance = new Vector2(1.5f, -1.5f);

            var previewButton = previewObj.GetComponent<Button>();
            previewButton.onClick.AddListener(() => OpenPicker(row.transform));
        }

        private void BuildSwatchStrip(Transform parent, IModConfigContext context)
        {
            var row = context.Templates.CreateEmptyRow(parent, 32f);
            row.name = "DeadEndQuickSwatches";

            var holder = new GameObject("Strip", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            holder.transform.SetParent(row.transform, false);

            var holderRect = (RectTransform)holder.transform;
            holderRect.anchorMin = new Vector2(0.02f, 0.12f);
            holderRect.anchorMax = new Vector2(0.98f, 0.88f);
            holderRect.offsetMin = Vector2.zero;
            holderRect.offsetMax = Vector2.zero;

            var layout = holder.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.padding = new RectOffset(2, 2, 0, 0);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var presets = DeadEndColorPreset.Presets;
            for (int i = 0; i < presets.Length; i++)
            {
                var preset = presets[i];
                var swatchObj = new GameObject("Swatch_" + i, typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(Button), typeof(Outline));
                swatchObj.transform.SetParent(holder.transform, false);

                var swatchRect = (RectTransform)swatchObj.transform;
                swatchRect.sizeDelta = new Vector2(28f, 22f);

                var sizing = swatchObj.GetComponent<LayoutElement>();
                sizing.minWidth = 18f;
                sizing.preferredWidth = 32f;
                sizing.preferredHeight = 22f;

                swatchObj.GetComponent<Image>().color = preset.Color;

                var outline = swatchObj.GetComponent<Outline>();
                outline.effectDistance = new Vector2(2f, -2f);
                swatchOutlines.Add((preset.Hex, outline));

                string hex = preset.Hex;
                swatchObj.GetComponent<Button>().onClick.AddListener(() => SetPendingHex(hex));
            }
        }

        private void OpenPicker(Transform anchor)
        {
            Color current = DeadEndColorPreset.ResolveColor(pendingHex);
            DeadEndColorPickerPopup.Open(anchor, label, current, nativeFont, nativeFontMaterial, picked =>
            {
                SetPendingHex(DeadEndColorPreset.ToHex(picked));
            });
        }

        private void SetPendingHex(string hex)
        {
            pendingHex = NormalizeHex(hex);
            RefreshPreview();
        }

        private void RefreshPreview()
        {
            Color color = DeadEndColorPreset.ResolveColor(pendingHex);

            if (previewSwatch)
                previewSwatch.color = color;

            if (hexButtonLabel)
                hexButtonLabel.text = $"{pendingHex}  Pick";

            for (int i = 0; i < swatchOutlines.Count; i++)
            {
                var item = swatchOutlines[i];
                if (!item.Outline)
                    continue;

                bool selected = string.Equals(item.Hex, pendingHex, StringComparison.OrdinalIgnoreCase);
                item.Outline.effectColor = selected
                    ? new Color(1f, 0.86f, 0.34f, 1f)
                    : new Color(0.14f, 0.11f, 0.09f, 0.9f);
            }
        }

        private static string NormalizeHex(string hex)
        {
            Color parsed = DeadEndColorPreset.ResolveColor(hex);
            return DeadEndColorPreset.ToHex(parsed);
        }
    }
}
