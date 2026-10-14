using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ZiplaKac.Core;

namespace ZiplaKac.UI
{
    /// <summary>
    /// Oyun içi HUD ekranını yöneten kontrolcü.
    /// Anlık skor, en iyi skor, aktif güçlendirme rozetleri, kombo bildirimi ve kontrol ipucunu gösterir.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Metin Bileşenleri")]
        public Text currentScoreText;
        public Text highScoreText;
        public Text controlTipText;

        [Header("Güçlendirme Rozetleri (Badges)")]
        public GameObject shieldBadgeObject;
        public Text shieldBadgeText;

        public GameObject slowBadgeObject;
        public Text slowBadgeText;

        public GameObject multiBadgeObject;
        public Text multiBadgeText;

        [Header("Kombo Bildirimi")]
        public GameObject comboBannerObject;
        public Text comboBannerText;
        public CanvasGroup comboCanvasGroup;

        private Coroutine comboCoroutine;
        private readonly Vector2 comboBasePosition = new Vector2(0f, 520f);

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnComboAchieved += HandleComboAchieved;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnComboAchieved -= HandleComboAchieved;
            }
            if (comboCoroutine != null)
            {
                StopCoroutine(comboCoroutine);
                comboCoroutine = null;
            }
        }

        private void HandleComboAchieved(int comboCount, Vector3 worldPosition)
        {
            ShowCombo(comboCount);
        }

        public void ShowCombo(int comboCount)
        {
            if (comboBannerObject == null || comboBannerText == null) return;
            if (comboCoroutine != null)
            {
                StopCoroutine(comboCoroutine);
            }
            comboCoroutine = StartCoroutine(AnimateComboBanner(comboCount));
        }

        private IEnumerator AnimateComboBanner(int comboCount)
        {
            comboBannerText.text = $"KOMBO x{comboCount}! +10";
            comboBannerObject.SetActive(true);

            RectTransform rt = comboBannerObject.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = comboBasePosition;
                rt.localScale = Vector3.one * 0.5f;
            }

            if (comboCanvasGroup != null)
            {
                comboCanvasGroup.alpha = 1f;
            }

            float duration = 1.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // 1. Ölçek Efekti (Punch Bounce)
                // İlk 0.15 sn: 0.5 -> 1.15, sonraki 0.10 sn: 1.15 -> 1.0
                float scale = 1f;
                if (elapsed < 0.15f)
                {
                    scale = Mathf.Lerp(0.5f, 1.15f, elapsed / 0.15f);
                }
                else if (elapsed < 0.25f)
                {
                    scale = Mathf.Lerp(1.15f, 1.0f, (elapsed - 0.15f) / 0.10f);
                }
                else
                {
                    scale = 1.0f;
                }

                if (rt != null)
                {
                    rt.localScale = Vector3.one * scale;
                    // Yavaşça yukarı doğru süzülme (520f -> 570f)
                    rt.anchoredPosition = comboBasePosition + new Vector2(0f, Mathf.Lerp(0f, 50f, t));
                }

                // 2. Saydamlık (Alpha Fade)
                // İlk %60 süre boyunca tam görünür, son %40 sürede yumuşak sönümleme
                if (comboCanvasGroup != null)
                {
                    if (t > 0.60f)
                    {
                        comboCanvasGroup.alpha = Mathf.Lerp(1f, 0f, (t - 0.60f) / 0.40f);
                    }
                    else
                    {
                        comboCanvasGroup.alpha = 1f;
                    }
                }

                yield return null;
            }

            if (comboCanvasGroup != null)
            {
                comboCanvasGroup.alpha = 0f;
            }
            comboBannerObject.SetActive(false);
            comboCoroutine = null;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
            {
                return;
            }

            // 1. Skor Güncellemeleri (index.html: Skor: X, En iyi: Y)
            int scoreInt = Mathf.FloorToInt(GameManager.Instance.CurrentScore);
            if (currentScoreText != null) currentScoreText.text = $"Skor: {scoreInt}";
            if (highScoreText != null) highScoreText.text = $"En iyi: {GameManager.Instance.HighScore}";

            // 2. Aktif Güçlendirme Rozetleri & Kalan Süreler
            UpdatePowerUpBadges();
        }

        private void UpdatePowerUpBadges()
        {
            // Kalkan Rozeti (index.html: Kalkan)
            bool shield = GameManager.Instance.isShieldActive;
            if (shieldBadgeObject != null) shieldBadgeObject.SetActive(shield);
            if (shieldBadgeText != null && shield) shieldBadgeText.text = "Kalkan";

            // Yavaşlatma Rozeti (index.html: Yavaş 4s)
            bool slow = GameManager.Instance.isSlowActive;
            if (slowBadgeObject != null) slowBadgeObject.SetActive(slow);
            if (slowBadgeText != null && slow)
            {
                int sec = Mathf.CeilToInt(GameManager.Instance.slowTimer);
                slowBadgeText.text = $"Yavaş {sec}s";
            }

            // x2 Skor Rozeti (index.html: x2 Puan 5s)
            bool multi = GameManager.Instance.isDoubleScoreActive;
            if (multiBadgeObject != null) multiBadgeObject.SetActive(multi);
            if (multiBadgeText != null && multi)
            {
                int sec = Mathf.CeilToInt(GameManager.Instance.doubleScoreTimer);
                multiBadgeText.text = $"x2 Puan {sec}s";
            }
        }
    }
}
