using UnityEngine;

namespace ZiplaKac.UI
{
    /// <summary>
    /// Mobil cihazların güvenli alanına (Dynamic Island, çentik, home bar vs.)
    /// otomatik uyum sağlayan ve ekran boyutu/yönü değiştikçe güncellenen SafeArea yöneticisi.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Rect lastSafeArea = Rect.zero;
        private Vector2Int lastScreenSize = Vector2Int.zero;
        private ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void OnEnable()
        {
            ApplySafeArea();
        }

        private void Update()
        {
            if (lastSafeArea != Screen.safeArea ||
                lastScreenSize.x != Screen.width ||
                lastScreenSize.y != Screen.height ||
                lastOrientation != Screen.orientation)
            {
                ApplySafeArea();
            }
        }

        public void ApplySafeArea()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            lastSafeArea = Screen.safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            lastOrientation = Screen.orientation;

            if (Screen.width <= 0 || Screen.height <= 0) return;

            Vector2 anchorMin = lastSafeArea.position;
            Vector2 anchorMax = lastSafeArea.position + lastSafeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Verilen parent altına tüm güvenli alanı kaplayan bir SafeArea konteyneri oluşturur.
        /// </summary>
        public static GameObject Create(Transform parent, string name = "SafeArea")
        {
            GameObject safeObj = new GameObject(name);
            safeObj.transform.SetParent(parent, false);

            RectTransform rt = safeObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            safeObj.AddComponent<SafeAreaFitter>();
            return safeObj;
        }
    }
}
