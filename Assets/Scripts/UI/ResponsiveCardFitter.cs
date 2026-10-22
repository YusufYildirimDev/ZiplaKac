using UnityEngine;
using UnityEngine.UI;

namespace ZiplaKac.UI
{
    /// <summary>
    /// Küçük telefon ekranları (örn. iPhone SE, 750x1334), çentikli/Dynamic Island telefonlar
    /// ve geniş tablet ekranlarında (iPad 4:3, Android tabletler) kartların ekran dışına taşmasını,
    /// kenarlara yapışmasını veya kesilmesini engelleyen akıllı responsive kart ölçekleyicisi.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ResponsiveCardFitter : MonoBehaviour
    {
        [Header("Maksimum Ekran Güvenli Alan Oranı")]
        [Range(0.5f, 1.0f)]
        public float maxSafeWidthRatio = 0.94f;

        [Range(0.5f, 1.0f)]
        public float maxSafeHeightRatio = 0.94f;

        [Range(0.4f, 1.0f)]
        public float minScale = 0.65f;

        [Header("Dikey Hizalama")]
        public bool adjustVerticalPosition = false;
        public float verticalOffset = 0f;

        private RectTransform rectTransform;
        private Canvas parentCanvas;
        private RectTransform canvasRect;
        private Vector2 baseSize;
        private Vector2 baseAnchoredPosition;
        private bool isInitialized = false;

        private int lastScreenWidth = -1;
        private int lastScreenHeight = -1;
        private Rect lastSafeArea = Rect.zero;

        private void Awake()
        {
            InitializeIfNeeded();
        }

        private void Start()
        {
            ApplyFit();
        }

        private void OnEnable()
        {
            ApplyFit();
        }

        private void InitializeIfNeeded()
        {
            if (isInitialized) return;

            rectTransform = GetComponent<RectTransform>();
            baseSize = rectTransform.sizeDelta;
            baseAnchoredPosition = rectTransform.anchoredPosition;

            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                canvasRect = parentCanvas.GetComponent<RectTransform>();
            }

            isInitialized = true;
        }

        private void Update()
        {
            if (Screen.width != lastScreenWidth || 
                Screen.height != lastScreenHeight || 
                Screen.safeArea != lastSafeArea)
            {
                ApplyFit();
            }
        }

        public void ApplyFit()
        {
            InitializeIfNeeded();

            if (rectTransform == null || Screen.width <= 0 || Screen.height <= 0) return;

            if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null && canvasRect == null) canvasRect = parentCanvas.GetComponent<RectTransform>();

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            lastSafeArea = Screen.safeArea;

            // Canvas boyutu (CanvasScaler tarafından ölçeklenmiş pikseller)
            float canvasW = canvasRect != null ? canvasRect.rect.width : 1080f;
            float canvasH = canvasRect != null ? canvasRect.rect.height : 1920f;

            // Safe Area oranları
            Rect safe = Screen.safeArea;
            float safeWidthRatio = safe.width / Screen.width;
            float safeHeightRatio = safe.height / Screen.height;

            float availableW = canvasW * safeWidthRatio * maxSafeWidthRatio;
            float availableH = canvasH * safeHeightRatio * maxSafeHeightRatio;

            // Kartın baz boyutunun sığıp sığmadığını hesapla
            float scaleX = availableW / Mathf.Max(1f, baseSize.x);
            float scaleY = availableH / Mathf.Max(1f, baseSize.y);
            float targetScale = Mathf.Min(scaleX, scaleY);

            // Eğer kart ekrandan büyükse orantılı scale down yap, küçükse 1.0f'te tut
            targetScale = Mathf.Clamp(targetScale, minScale, 1.0f);
            rectTransform.localScale = new Vector3(targetScale, targetScale, 1.0f);

            // Dikey pozisyon ince ayarı (örn. safe area merkezleme)
            if (adjustVerticalPosition)
            {
                // Safe area dikey merkez ofseti (canvas koordinatlarında)
                float safeCenterYNorm = (safe.y + (safe.height * 0.5f)) / Screen.height;
                float canvasSafeCenterY = (safeCenterYNorm - 0.5f) * canvasH;

                rectTransform.anchoredPosition = new Vector2(
                    baseAnchoredPosition.x,
                    canvasSafeCenterY + verticalOffset * targetScale
                );
            }
        }
    }
}
