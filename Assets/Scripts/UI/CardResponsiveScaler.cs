using UnityEngine;

namespace ZiplaKac.UI
{
    /// <summary>
    /// Modal ve Menü kartlarının ekran/SafeArea sınırlarını aşmasını önleyen,
    /// ekran küçükse kartı orantılı olarak ölçekleyen (clamp scale) bileşen.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CardResponsiveScaler : MonoBehaviour
    {
        public float targetWidth = 920f;
        public float targetHeight = 1340f;
        public float padding = 30f;

        private RectTransform rectTransform;
        private RectTransform parentRect;
        private Vector2 lastParentSize = Vector2.zero;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (transform.parent != null)
                parentRect = transform.parent.GetComponent<RectTransform>();
            AdjustScale();
        }

        private void OnEnable()
        {
            AdjustScale();
        }

        private void Update()
        {
            if (parentRect != null && (parentRect.rect.size != lastParentSize))
            {
                AdjustScale();
            }
        }

        public void AdjustScale()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (parentRect == null && transform.parent != null) parentRect = transform.parent.GetComponent<RectTransform>();
            if (parentRect == null) return;

            lastParentSize = parentRect.rect.size;

            float availableW = parentRect.rect.width - (padding * 2f);
            float availableH = parentRect.rect.height - (padding * 2f);

            if (availableW <= 0f || availableH <= 0f) return;

            float scaleW = availableW / targetWidth;
            float scaleH = availableH / targetHeight;
            float finalScale = Mathf.Min(1.0f, Mathf.Min(scaleW, scaleH));

            if (finalScale < 0.999f && finalScale > 0.1f)
            {
                rectTransform.localScale = new Vector3(finalScale, finalScale, 1f);
            }
            else
            {
                rectTransform.localScale = Vector3.one;
            }
        }
    }
}
