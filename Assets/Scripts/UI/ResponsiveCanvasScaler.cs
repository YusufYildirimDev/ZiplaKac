using UnityEngine;
using UnityEngine.UI;

namespace ZiplaKac.UI
{
    /// <summary>
    /// Ekranın en-boy oranına göre CanvasScaler'ın Match Width/Height değerini dinamik yöneten sınıf.
    /// Tablet/iPad gibi geniş ekranlarda yüksekliği korur (Match=1),
    /// iPhone/Android gibi uzun ekranlarda genişliği korur (Match=0).
    /// Böylece paneller hiçbir ekranda taşmaz veya kesilmez.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public class ResponsiveCanvasScaler : MonoBehaviour
    {
        public const float REFERENCE_WIDTH = 1080f;
        public const float REFERENCE_HEIGHT = 1920f;
        public const float TARGET_ASPECT = REFERENCE_WIDTH / REFERENCE_HEIGHT; // 0.5625 (9:16)

        private CanvasScaler canvasScaler;
        private int lastWidth = -1;
        private int lastHeight = -1;

        private void Awake()
        {
            canvasScaler = GetComponent<CanvasScaler>();
            ApplyResponsiveScale();
        }

        private void Update()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight)
            {
                ApplyResponsiveScale();
            }
        }

        public void ApplyResponsiveScale()
        {
            if (canvasScaler == null)
                canvasScaler = GetComponent<CanvasScaler>();

            if (canvasScaler == null || Screen.width <= 0 || Screen.height <= 0) return;

            lastWidth = Screen.width;
            lastHeight = Screen.height;

            float currentAspect = (float)Screen.width / Screen.height;

            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(REFERENCE_WIDTH, REFERENCE_HEIGHT);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            // 9:16 ve daha dar telefonlar (iPhone 14/15/16 Pro Max, modern Android: aspect <= 0.57): Match Width = 0.0f
            // Geniş ekranlar ve tabletler (iPad 3:4 / 4:3, tabletler: aspect >= 0.72): Match Height = 1.0f
            // Ara oranlar (örn. katlanabilir telefonlar veya 16:10 ekranlar): orantılı yumuşak geçiş
            if (currentAspect <= 0.57f)
            {
                canvasScaler.matchWidthOrHeight = 0.0f;
            }
            else if (currentAspect >= 0.72f)
            {
                canvasScaler.matchWidthOrHeight = 1.0f;
            }
            else
            {
                canvasScaler.matchWidthOrHeight = Mathf.InverseLerp(0.57f, 0.72f, currentAspect);
            }
        }
    }
}
