using System;
using KingdomDeadEnd.Configuration;
using KingdomFoundation.Api;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace KingdomDeadEnd.UI
{
    internal sealed class DeadEndColorPickerPopup : MonoBehaviour
    {
        private const int SvTextureSize = 64;
        private const int HueTextureWidth = 128;

        private static DeadEndColorPickerPopup activeInstance;

        private string titleText;
        private Color initialColor;
        private TMP_FontAsset fontAsset;
        private Material fontMaterial;
        private Action<Color> onConfirm;
        private float hue;
        private float saturation;
        private float value;
        private bool updatingHex;

        private Texture2D svTexture;
        private Texture2D hueTexture;
        private RawImage svImage;
        private RawImage hueImage;
        private RectTransform svCursor;
        private RectTransform hueCursor;
        private Image oldSwatch;
        private Image newSwatch;
        private TMP_InputField hexInput;

        public static void Open(
            Transform anchor,
            string title,
            Color currentColor,
            TMP_FontAsset font,
            Material fontMat,
            Action<Color> onConfirm)
        {
            CloseActive();

            Canvas rootCanvas = anchor ? anchor.GetComponentInParent<Canvas>()?.rootCanvas : null;
            if (!rootCanvas)
                rootCanvas = FindAnyObjectByType<Canvas>()?.rootCanvas;

            if (!rootCanvas)
                return;

            var backdrop = new GameObject(
                "DeadEndColorPickerPopup",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            backdrop.transform.SetParent(rootCanvas.transform, worldPositionStays: false);
            backdrop.transform.SetAsLastSibling();

            var backdropRect = (RectTransform)backdrop.transform;
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;

            var backdropImage = backdrop.GetComponent<Image>();
            backdropImage.color = new Color(0f, 0f, 0f, 0.58f);

            var popup = backdrop.AddComponent<DeadEndColorPickerPopup>();
            popup.Initialize(title, currentColor, font, fontMat, onConfirm);
            activeInstance = popup;

            var backdropButton = backdrop.GetComponent<Button>();
            backdropButton.transition = Selectable.Transition.None;
            backdropButton.onClick.AddListener(CloseActive);

            FoundationApi.NativeUiFactory?.BlockWorkspaceZoom(backdrop);
        }

        public static void CloseActive()
        {
            if (activeInstance)
            {
                Destroy(activeInstance.gameObject);
                activeInstance = null;
            }
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                CloseActive();
        }

        private void OnDestroy()
        {
            if (svTexture)
            {
                Destroy(svTexture);
                svTexture = null;
            }

            if (hueTexture)
            {
                Destroy(hueTexture);
                hueTexture = null;
            }

            if (ReferenceEquals(activeInstance, this))
                activeInstance = null;
        }

        private void Initialize(
            string title,
            Color currentColor,
            TMP_FontAsset font,
            Material fontMat,
            Action<Color> confirmCallback)
        {
            titleText = string.IsNullOrWhiteSpace(title) ? "Pick Color" : title;
            initialColor = new Color(currentColor.r, currentColor.g, currentColor.b, 1f);
            fontAsset = font;
            fontMaterial = fontMat;
            onConfirm = confirmCallback;
            Color.RGBToHSV(initialColor, out hue, out saturation, out value);

            BuildWindow();
            RebuildSvTexture();
            RefreshUi(updateHexText: true);
        }

        private void BuildWindow()
        {
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            panel.transform.SetParent(transform, worldPositionStays: false);

            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(380f, 428f);

            var panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0.14f, 0.11f, 0.10f, 0.98f);

            var panelOutline = panel.GetComponent<Outline>();
            panelOutline.effectColor = new Color(0.76f, 0.58f, 0.30f, 1f);
            panelOutline.effectDistance = new Vector2(2f, -2f);

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 14, 14);
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            BuildTitleRow(panel.transform);
            BuildSvBox(panel.transform);
            BuildHueBar(panel.transform);
            BuildQuickSwatches(panel.transform);
            BuildPreviewAndHexRow(panel.transform);
            BuildActionRow(panel.transform);
        }

        private void BuildTitleRow(Transform parent)
        {
            var titleObj = new GameObject("Title", typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(parent, false);
            var le = titleObj.GetComponent<LayoutElement>();
            le.preferredHeight = 28f;
            le.minHeight = 28f;

            var text = titleObj.GetComponent<TextMeshProUGUI>();
            ApplyFont(text, 20f);
            text.text = titleText;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.96f, 0.84f, 0.54f, 1f);
        }

        private void BuildSvBox(Transform parent)
        {
            var container = new GameObject("SvContainer", typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(Outline));
            container.transform.SetParent(parent, false);
            var le = container.GetComponent<LayoutElement>();
            le.preferredHeight = 180f;
            le.minHeight = 180f;
            container.GetComponent<Image>().color = Color.black;

            var outline = container.GetComponent<Outline>();
            outline.effectColor = new Color(0.55f, 0.44f, 0.28f, 0.9f);
            outline.effectDistance = new Vector2(1f, -1f);

            var surfaceObj = new GameObject("SvSurface", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(DeadEndPickerSurface));
            surfaceObj.transform.SetParent(container.transform, false);
            StretchFull((RectTransform)surfaceObj.transform);

            svTexture = new Texture2D(SvTextureSize, SvTextureSize, TextureFormat.RGBA32, mipChain: false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            svImage = surfaceObj.GetComponent<RawImage>();
            svImage.texture = svTexture;

            var surface = surfaceObj.GetComponent<DeadEndPickerSurface>();
            surface.OnValuePicked = uv =>
            {
                saturation = Mathf.Clamp01(uv.x);
                value = Mathf.Clamp01(uv.y);
                RefreshUi(updateHexText: true);
            };

            svCursor = CreateCursor(surfaceObj.transform, new Vector2(12f, 12f));
        }

        private void BuildHueBar(Transform parent)
        {
            var container = new GameObject("HueContainer", typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(Outline));
            container.transform.SetParent(parent, false);
            var le = container.GetComponent<LayoutElement>();
            le.preferredHeight = 24f;
            le.minHeight = 24f;
            container.GetComponent<Image>().color = Color.black;

            var outline = container.GetComponent<Outline>();
            outline.effectColor = new Color(0.55f, 0.44f, 0.28f, 0.9f);
            outline.effectDistance = new Vector2(1f, -1f);

            var surfaceObj = new GameObject("HueSurface", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(DeadEndPickerSurface));
            surfaceObj.transform.SetParent(container.transform, false);
            StretchFull((RectTransform)surfaceObj.transform);

            hueTexture = BuildHueTexture();
            hueImage = surfaceObj.GetComponent<RawImage>();
            hueImage.texture = hueTexture;

            var surface = surfaceObj.GetComponent<DeadEndPickerSurface>();
            surface.OnValuePicked = uv =>
            {
                hue = Mathf.Clamp01(uv.x);
                RebuildSvTexture();
                RefreshUi(updateHexText: true);
            };

            hueCursor = CreateCursor(surfaceObj.transform, new Vector2(6f, 26f));
        }

        private void BuildQuickSwatches(Transform parent)
        {
            var row = new GameObject("QuickSwatches", typeof(RectTransform), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            var le = row.GetComponent<LayoutElement>();
            le.preferredHeight = 26f;
            le.minHeight = 26f;

            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var presets = DeadEndColorPreset.Presets;
            for (int i = 0; i < presets.Length; i++)
            {
                Color swatchColor = presets[i].Color;
                var swatchObj = new GameObject("Swatch_" + i, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
                swatchObj.transform.SetParent(row.transform, false);

                swatchObj.GetComponent<Image>().color = swatchColor;
                var outline = swatchObj.GetComponent<Outline>();
                outline.effectColor = new Color(0.10f, 0.08f, 0.07f, 0.95f);
                outline.effectDistance = new Vector2(1f, -1f);

                swatchObj.GetComponent<Button>().onClick.AddListener(() => SelectColor(swatchColor));
            }
        }

        private void BuildPreviewAndHexRow(Transform parent)
        {
            var row = new GameObject("PreviewHexRow", typeof(RectTransform), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            var le = row.GetComponent<LayoutElement>();
            le.preferredHeight = 36f;
            le.minHeight = 36f;

            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            oldSwatch = CreatePreviewSwatch(row.transform, "OldSwatch", initialColor, 46f);
            newSwatch = CreatePreviewSwatch(row.transform, "NewSwatch", initialColor, 56f);

            var labelObj = new GameObject("HexLabel", typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
            labelObj.transform.SetParent(row.transform, false);
            var labelLe = labelObj.GetComponent<LayoutElement>();
            labelLe.preferredWidth = 48f;
            labelLe.minWidth = 48f;

            var labelText = labelObj.GetComponent<TextMeshProUGUI>();
            ApplyFont(labelText, 16f);
            labelText.text = "Hex:";
            labelText.alignment = TextAlignmentOptions.MidlineRight;
            labelText.color = new Color(0.88f, 0.84f, 0.78f, 1f);

            hexInput = CreateHexInput(row.transform);
        }

        private void BuildActionRow(Transform parent)
        {
            var row = new GameObject("Actions", typeof(RectTransform), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            var le = row.GetComponent<LayoutElement>();
            le.preferredHeight = 38f;
            le.minHeight = 38f;

            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            CreateActionButton(row.transform, "Cancel", new Color(0.32f, 0.25f, 0.22f, 1f), CloseActive);
            CreateActionButton(row.transform, "Select Color", new Color(0.72f, 0.40f, 0.12f, 1f), ConfirmSelection);
        }

        private TMP_InputField CreateHexInput(Transform parent)
        {
            var inputObj = new GameObject("HexInput", typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(Outline), typeof(TMP_InputField));
            inputObj.transform.SetParent(parent, false);
            var le = inputObj.GetComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.minWidth = 120f;

            inputObj.GetComponent<Image>().color = new Color(0.08f, 0.07f, 0.06f, 1f);
            var outline = inputObj.GetComponent<Outline>();
            outline.effectColor = new Color(0.55f, 0.44f, 0.28f, 0.9f);
            outline.effectDistance = new Vector2(1f, -1f);

            var textArea = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(inputObj.transform, false);
            var areaRect = (RectTransform)textArea.transform;
            areaRect.anchorMin = Vector2.zero;
            areaRect.anchorMax = Vector2.one;
            areaRect.offsetMin = new Vector2(8f, 4f);
            areaRect.offsetMax = new Vector2(-8f, -4f);

            var textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(textArea.transform, false);
            StretchFull((RectTransform)textObj.transform);

            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            ApplyFont(tmp, 16f);
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;

            var input = inputObj.GetComponent<TMP_InputField>();
            input.textViewport = areaRect;
            input.textComponent = tmp;
            input.characterLimit = 7;
            input.onValueChanged.AddListener(OnHexEdited);
            return input;
        }

        private void OnHexEdited(string raw)
        {
            if (updatingHex || string.IsNullOrWhiteSpace(raw))
                return;

            string candidate = raw.Trim();
            if (!candidate.StartsWith("#"))
                candidate = "#" + candidate;

            if (candidate.Length != 7)
                return;

            if (!ColorUtility.TryParseHtmlString(candidate, out Color parsed))
                return;

            Color.RGBToHSV(parsed, out hue, out saturation, out value);
            RebuildSvTexture();
            RefreshUi(updateHexText: false);
        }

        private void SelectColor(Color color)
        {
            Color.RGBToHSV(color, out hue, out saturation, out value);
            RebuildSvTexture();
            RefreshUi(updateHexText: true);
        }

        private void ConfirmSelection()
        {
            Color chosen = CurrentColor();
            var callback = onConfirm;
            CloseActive();
            callback?.Invoke(chosen);
        }

        private Color CurrentColor()
        {
            Color c = Color.HSVToRGB(hue, saturation, value);
            c.a = 1f;
            return c;
        }

        private void RebuildSvTexture()
        {
            if (!svTexture)
                return;

            var pixels = new Color32[SvTextureSize * SvTextureSize];
            float inv = 1f / (SvTextureSize - 1);

            for (int y = 0; y < SvTextureSize; y++)
            {
                float v = y * inv;
                for (int x = 0; x < SvTextureSize; x++)
                {
                    float s = x * inv;
                    Color c = Color.HSVToRGB(hue, s, v);
                    pixels[y * SvTextureSize + x] = (Color32)c;
                }
            }

            svTexture.SetPixels32(pixels);
            svTexture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
        }

        private void RefreshUi(bool updateHexText)
        {
            Color chosen = CurrentColor();

            if (newSwatch)
                newSwatch.color = chosen;

            if (svCursor)
            {
                svCursor.anchorMin = new Vector2(saturation, value);
                svCursor.anchorMax = new Vector2(saturation, value);
                svCursor.anchoredPosition = Vector2.zero;
            }

            if (hueCursor)
            {
                hueCursor.anchorMin = new Vector2(hue, 0.5f);
                hueCursor.anchorMax = new Vector2(hue, 0.5f);
                hueCursor.anchoredPosition = Vector2.zero;
            }

            if (updateHexText && hexInput)
            {
                updatingHex = true;
                hexInput.text = DeadEndColorPreset.ToHex(chosen);
                updatingHex = false;
            }
        }

        private void ApplyFont(TMP_Text text, float size)
        {
            if (fontAsset != null)
            {
                text.font = fontAsset;
                if (fontMaterial != null)
                    text.fontSharedMaterial = fontMaterial;
            }

            text.fontSize = size;
        }

        private static Texture2D BuildHueTexture()
        {
            var texture = new Texture2D(HueTextureWidth, 1, TextureFormat.RGBA32, mipChain: false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var pixels = new Color32[HueTextureWidth];
            float inv = 1f / (HueTextureWidth - 1);
            for (int x = 0; x < HueTextureWidth; x++)
                pixels[x] = (Color32)Color.HSVToRGB(x * inv, 1f, 1f);

            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            return texture;
        }

        private static RectTransform CreateCursor(Transform parent, Vector2 size)
        {
            var obj = new GameObject("Cursor", typeof(RectTransform), typeof(Image), typeof(Outline));
            obj.transform.SetParent(parent, false);

            var rect = (RectTransform)obj.transform;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;

            var image = obj.GetComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;

            var outline = obj.GetComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return rect;
        }

        private static Image CreatePreviewSwatch(Transform parent, string name, Color color, float width)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(Outline));
            obj.transform.SetParent(parent, false);
            var le = obj.GetComponent<LayoutElement>();
            le.preferredWidth = width;
            le.minWidth = width;

            var image = obj.GetComponent<Image>();
            image.color = color;

            var outline = obj.GetComponent<Outline>();
            outline.effectColor = new Color(0.65f, 0.52f, 0.30f, 0.9f);
            outline.effectDistance = new Vector2(1f, -1f);
            return image;
        }

        private void CreateActionButton(Transform parent, string label, Color background, Action onClick)
        {
            var obj = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            obj.transform.SetParent(parent, false);

            var image = obj.GetComponent<Image>();
            image.color = background;

            var outline = obj.GetComponent<Outline>();
            outline.effectColor = new Color(0.76f, 0.58f, 0.30f, 0.9f);
            outline.effectDistance = new Vector2(1f, -1f);

            var button = obj.GetComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());

            var textObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(obj.transform, false);
            StretchFull((RectTransform)textObj.transform);

            var text = textObj.GetComponent<TextMeshProUGUI>();
            ApplyFont(text, 16f);
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
