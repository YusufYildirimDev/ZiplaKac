using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ZiplaKac.UI
{
    /// <summary>
    /// Hoppzy Açılış Ekranı (Launch Screen) Denetleyicisi.
    /// Gerçek zamanlı ilerleme çubuğu animasyonu, yumuşak kararma geçişi ve
    /// tek dokunuşla atlama (Tap-to-Skip) desteği sunar.
    /// </summary>
    public class SplashScreenController : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
    {
        [Header("Zamanlama Ayarları")]
        [Tooltip("Açılış ekranının kalma süresi (saniye)")]
        [SerializeField] private float displayDuration = 1.4f;

        [Tooltip("Ana menüye geçiş kararma süresi (saniye)")]
        [SerializeField] private float fadeDuration = 0.35f;

        [Header("Bileşenler")]
        public CanvasGroup canvasGroup;
        public Image progressBarFill;
        public Text loadingText;

        private bool isDismissing = false;
        private Coroutine activeRoutine;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        private void Start()
        {
            activeRoutine = StartCoroutine(SplashSequence());
        }

        private void Update()
        {
            if (!isDismissing)
            {
                // Yeni veya eski girdi sistemi fark etmeksizin dokunmayı yakala
                if (Input.anyKeyDown || Input.touchCount > 0 || Input.GetMouseButtonDown(0))
                {
                    Skip();
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Skip();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Skip();
        }

        public void Skip()
        {
            if (isDismissing) return;
            isDismissing = true;
            // Çubuğu anında doldur
            if (progressBarFill != null)
            {
                progressBarFill.fillAmount = 1f;
            }
        }

        private IEnumerator SplashSequence()
        {
            float elapsed = 0f;

            // 1. İlerleme Çubuğu ve Görsel Gösterim Döngüsü (Unscaled time ile timescale'e bağımsız)
            while (elapsed < displayDuration && !isDismissing)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / displayDuration);

                if (progressBarFill != null)
                {
                    progressBarFill.fillAmount = progress;
                }

                if (loadingText != null)
                {
                    int pct = Mathf.RoundToInt(progress * 100f);
                    loadingText.text = $"{pct}%";
                }

                yield return null;
            }

            // Tamamlandı
            isDismissing = true;
            if (progressBarFill != null)
            {
                progressBarFill.fillAmount = 1f;
            }

            if (loadingText != null)
            {
                loadingText.text = "100%";
            }

            // Kısa bir bekleme (0.15s)
            float pause = 0f;
            while (pause < 0.15f)
            {
                pause += Time.unscaledDeltaTime;
                yield return null;
            }

            // 2. Yumuşak Kararma (Fade Out) Döngüsü
            float fadeElapsed = 0f;
            float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;

            while (fadeElapsed < fadeDuration)
            {
                fadeElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(fadeElapsed / fadeDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, smoothT);
                }
                yield return null;
            }

            // 3. Temizlik
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            Destroy(gameObject);
        }
    }
}
