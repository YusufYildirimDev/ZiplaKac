using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ZiplaKac.UI
{
    /// <summary>
    /// Uygulama ilk açıldığında Hoppzy Açılış Ekranını (Launch Screen / Splash Screen)
    /// ekranda gösterip ardından yumuşak bir kararma (fade-out) efektiyle ana menüye aktaran denetleyici.
    /// Ekrana dokunulduğunda beklemeden anında geçişi sağlar.
    /// </summary>
    public class SplashScreenController : MonoBehaviour
    {
        [Header("Zaman Ayarları")]
        [Tooltip("Açılış ekranının ekranda net kalma süresi (saniye)")]
        [SerializeField] private float displayDuration = 1.8f;

        [Tooltip("Ana menüye yumuşak geçiş süresi (saniye)")]
        [SerializeField] private float fadeDuration = 0.45f;

        [Header("Bileşenler")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button skipButton;

        private bool isDismissing = false;

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

            if (skipButton == null)
            {
                skipButton = GetComponent<Button>();
            }

            if (skipButton != null)
            {
                skipButton.onClick.AddListener(Skip);
            }
        }

        private void Start()
        {
            StartCoroutine(DisplayAndFadeRoutine());
        }

        public void Skip()
        {
            if (isDismissing) return;
            StopAllCoroutines();
            StartCoroutine(FadeOutRoutine());
        }

        private IEnumerator DisplayAndFadeRoutine()
        {
            // Belirlenen süre boyunca açılış görselini tam parlaklıkta tut
            yield return new WaitForSeconds(displayDuration);
            yield return FadeOutRoutine();
        }

        private IEnumerator FadeOutRoutine()
        {
            isDismissing = true;
            float elapsed = 0f;
            float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                // Yumuşak geçiş eğrisi (Ease Out)
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, smoothT);
                }
                yield return null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (skipButton != null)
            {
                skipButton.onClick.RemoveListener(Skip);
            }
        }
    }
}
