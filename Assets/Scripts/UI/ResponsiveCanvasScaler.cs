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

            // Ekran 9:16'dan daha genişse (örn. iPad 4:3, tabletler):
            // Dikeyde taşma olmaması için yüksekliğe eşitle (Match Height = 1.0)
            // Ekran 9:16 veya daha darsa (örn. iPhone 14 Pro Max 9:19.5, modern telefonlar):
            // Yatayda taşma olmaması için genişliğe eşitle (Match Width = 0.0)
            canvasScaler.matchWidthOrHeight = currentAspect > TARGET_ASPECT ? 1.0f : 0.0f;
        }
    }
}
