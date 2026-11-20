using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KingdomDeadEnd.UI
{
    internal sealed class DeadEndPickerSurface : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        public Action<Vector2> OnValuePicked { get; set; }

        public void OnPointerDown(PointerEventData eventData)
        {
            DispatchPointer(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            DispatchPointer(eventData);
        }

        private void DispatchPointer(PointerEventData eventData)
        {
            if (eventData == null || OnValuePicked == null)
                return;

            var rectTransform = transform as RectTransform;
            if (!rectTransform)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
            {
                return;
            }

            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            float u = Mathf.Clamp01((localPoint.x - rect.xMin) / rect.width);
            float v = Mathf.Clamp01((localPoint.y - rect.yMin) / rect.height);
            OnValuePicked(new Vector2(u, v));
        }
    }
}
