using KingdomDeadEnd.Configuration;
using KingdomDeadEnd.Core;
using KingdomFoundation.Api;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomDeadEnd.Rendering
{
    public sealed class DeadEndPortOverlay : MonoBehaviour
    {
        private const string HighlightObjectName = "DeadEndPortHighlight";
        private const float PollIntervalSeconds = 0.1f;

        private WorkspacePort port;
        private Image portSpriteImage;
        private Image highlightImage;
        private DeadEndIndicatorStyle appliedStyle;
        private DeadEndOutlineThickness appliedThickness;
        private bool spriteInitialized;
        private float nextCheckTime;
        private bool currentlyVisible;

        public static DeadEndPortOverlay Ensure(Component portView, WorkspacePort port)
        {
            if (!portView || port == null || (!port.IsInput && !port.IsOutput))
                return null;

            var overlay = portView.GetComponent<DeadEndPortOverlay>();
            if (!overlay)
                overlay = portView.gameObject.AddComponent<DeadEndPortOverlay>();

            overlay.Bind(portView, port);
            return overlay;
        }

        public void Bind(Component portView, WorkspacePort workspacePort)
        {
            port = workspacePort;
            portSpriteImage = FoundationApi.WorkspaceVisuals.PortGraphic(portView, GamePortGraphic.Sprite) as Image;
            EnsureHighlightGraphic();
            RefreshVisuals(force: true);
        }

        public void RefreshVisuals(bool force)
        {
            var settings = DeadEndPlugin.Instance?.Settings;
            if (settings == null || port == null)
            {
                SetVisible(false);
                return;
            }

            if (!highlightImage)
            {
                EnsureHighlightGraphic();
                if (!highlightImage)
                    return;
            }

            bool shouldShow = ShouldShowHighlight(settings);
            if (!shouldShow)
            {
                SetVisible(false);
                return;
            }

            if (force || !spriteInitialized || appliedStyle != settings.Style || appliedThickness != settings.Thickness)
            {
                highlightImage.sprite = DeadEndSpriteFactory.GetSprite(settings.Style, settings.Thickness);
                appliedStyle = settings.Style;
                appliedThickness = settings.Thickness;
                spriteInitialized = true;
            }

            ApplyColor(settings);
            SetVisible(true);
        }

        private void Update()
        {
            var settings = DeadEndPlugin.Instance?.Settings;
            if (settings == null)
                return;

            if (Time.unscaledTime >= nextCheckTime)
            {
                nextCheckTime = Time.unscaledTime + PollIntervalSeconds;
                RefreshVisuals(force: false);
                return;
            }

            if (currentlyVisible && settings.PulseEnabled && highlightImage)
                ApplyColor(settings);
        }

        private void OnDestroy()
        {
            if (highlightImage)
            {
                Destroy(highlightImage.gameObject);
                highlightImage = null;
            }
        }

        private bool ShouldShowHighlight(DeadEndSettings settings)
        {
            if (!settings.Enabled || port == null)
                return false;

            if (port.IsInput)
            {
                if (!settings.HighlightInputs)
                    return false;
            }
            else if (port.IsOutput)
            {
                if (!settings.HighlightOutputs)
                    return false;
            }
            else
            {
                return false;
            }

            if (!settings.HighlightJunctions && port.Parent != null && port.Parent.IsJunction)
                return false;

            return port.ConnectedPipe == null;
        }

        private void EnsureHighlightGraphic()
        {
            if (highlightImage || !portSpriteImage)
                return;

            Transform existing = portSpriteImage.transform.Find(HighlightObjectName);
            GameObject highlightObject;
            if (existing)
            {
                highlightObject = existing.gameObject;
                highlightImage = highlightObject.GetComponent<Image>() ?? highlightObject.AddComponent<Image>();
            }
            else
            {
                highlightObject = new GameObject(
                    HighlightObjectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                highlightObject.transform.SetParent(portSpriteImage.transform, worldPositionStays: false);
                highlightImage = highlightObject.GetComponent<Image>();
            }

            var rect = (RectTransform)highlightObject.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.sizeDelta = new Vector2(DeadEndSpriteFactory.OverlayUiSize, DeadEndSpriteFactory.OverlayUiSize);

            highlightImage.raycastTarget = false;
            highlightImage.maskable = true;
            highlightImage.type = Image.Type.Simple;
            highlightImage.preserveAspect = false;
            highlightImage.enabled = false;
            currentlyVisible = false;
        }

        private void ApplyColor(DeadEndSettings settings)
        {
            Color color = port != null && port.IsOutput ? settings.OutputColor : settings.InputColor;
            float alpha = settings.Opacity;

            if (settings.PulseEnabled)
            {
                float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4.5f);
                alpha *= Mathf.Lerp(0.55f, 1.0f, wave);
            }

            color.a = Mathf.Clamp01(alpha);
            highlightImage.color = color;
        }

        private void SetVisible(bool visible)
        {
            currentlyVisible = visible;
            if (highlightImage && highlightImage.enabled != visible)
                highlightImage.enabled = visible;
        }
    }
}
