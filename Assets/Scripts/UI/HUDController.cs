using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ZiplaKac.Core;

namespace ZiplaKac.UI
{
    /// <summary>
    /// Oyun içi HUD ekranını yöneten kontrolcü.
    /// Anlık skor, en iyi skor, aktif güçlendirme rozetleri, kombo bildirimi ve kontrol ipucunu gösterir.
    /// Aktif zamanlı güçlendirme varsa ekran ortasında büyük, belirgin bir geri sayaç gösterir.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Metin Bileşenleri")]
        public Text currentScoreText;
        public Text highScoreText;
        public Text goldText;
        public Text controlTipText;

        [Header("Güçlendirme Rozetleri (Badges)")]
        public GameObject shieldBadgeObject;
        public Text shieldBadgeText;

        public GameObject slowBadgeObject;
        public Text slowBadgeText;

        public GameObject multiBadgeObject;
        public Text multiBadgeText;

        [Header("Merkez Güçlendirme Geri Sayaç")]
        [Tooltip("Ekran ortasındaki büyük geri sayaç paneli (Image veya RectTransform içeren GameObject)")]
        public GameObject powerupCountdownPanel;

        [Tooltip("Geri sayaçtaki büyük sayı metni (örn. '5')")]
        public Text countdownNumberText;

        [Tooltip("Geri sayaç ikonu/adı metni (örn. 'YAVAŞ' veya 'X2 PUAN')")]
        public Text countdownLabelText;

        [Tooltip("Sayaç dairesinin arka plan Image bileşeni — renk değişimi için")]
        public Image countdownRingImage;

        [Header("Geri Sayaç Renkleri")]
        public Color slowColor = new Color(0.30f, 0.82f, 0.94f, 1f);   // Mavi — Yavaş
        public Color multiColor = new Color(1.00f, 0.84f, 0.04f, 1f);  // Altın — x2 Puan

        [Header("Kombo Bildirimi")]
        public GameObject comboBannerObject;
        public Text comboBannerText;
        public CanvasGroup comboCanvasGroup;

        private Coroutine comboCoroutine;
        private Coroutine pulseCoroutine;
        private readonly Vector2 comboBasePosition = new Vector2(0f, 520f);

        // Son gösterilen güçlendirme tipi — değişim animasyonu için
        private enum PowerupType { None, Slow, Multi }
        private PowerupType lastShownType = PowerupType.None;

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
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
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

            // 1. Skor ve Altın Güncellemeleri (Büyük ve Belirgin Arcade Formatı)
            int scoreInt = Mathf.FloorToInt(GameManager.Instance.CurrentScore);
            if (currentScoreText != null) currentScoreText.text = $"<size=20><color=#8da4c4>SKOR</color></size>\n<size=42><color=#ffffff><b>{scoreInt}</b></color></size>";
            if (highScoreText != null) highScoreText.text = $"<size=20><color=#ffd166>EN İYİ</color></size>\n<size=42><color=#ffffff><b>{GameManager.Instance.HighScore}</b></color></size>";
            if (goldText != null) goldText.text = $"<size=18><color=#ffd166>ALTIN</color></size>\n<size=38><color=#ffea79><b>{GameManager.Instance.CurrentRunGold}</b></color></size>";

            // 2. Aktif Güçlendirme Rozetleri & Kalan Süreler
            UpdatePowerUpBadges();

            // 3. Merkez Geri Sayaç
            UpdateCenterCountdown();
        }

        private void UpdatePowerUpBadges()
        {
            // Kalkan Rozeti
            bool shield = GameManager.Instance.isShieldActive;
            if (shieldBadgeObject != null) shieldBadgeObject.SetActive(shield);
            if (shieldBadgeText != null && shield) shieldBadgeText.text = "Kalkan";

            // Yavaşlatma Rozeti
            bool slow = GameManager.Instance.isSlowActive;
            if (slowBadgeObject != null) slowBadgeObject.SetActive(slow);
            if (slowBadgeText != null && slow)
            {
                int sec = Mathf.CeilToInt(GameManager.Instance.slowTimer);
                slowBadgeText.text = $"Yavaş {sec}s";
            }

            // x2 Skor Rozeti
            bool multi = GameManager.Instance.isDoubleScoreActive;
            if (multiBadgeObject != null) multiBadgeObject.SetActive(multi);
            if (multiBadgeText != null && multi)
            {
                int sec = Mathf.CeilToInt(GameManager.Instance.doubleScoreTimer);
                multiBadgeText.text = $"x2 Puan {sec}s";
            }
        }

        private void UpdateCenterCountdown()
        {
            if (powerupCountdownPanel == null) return;

            bool hasSlow  = GameManager.Instance.isSlowActive;
            bool hasMulti = GameManager.Instance.isDoubleScoreActive;

            if (!hasSlow && !hasMulti)
            {
                // Hiçbir zamanlı güçlendirme yok — paneli gizle
                if (powerupCountdownPanel.activeSelf)
                {
                    powerupCountdownPanel.SetActive(false);
                    lastShownType = PowerupType.None;
                }
                return;
            }

            // Aktif güçlendirme belirle (ikisi aynı anda aktifse multi öncelikli)
            PowerupType current = hasMulti ? PowerupType.Multi : PowerupType.Slow;
            int seconds = current == PowerupType.Multi
                ? Mathf.CeilToInt(GameManager.Instance.doubleScoreTimer)
                : Mathf.CeilToInt(GameManager.Instance.slowTimer);

            // İlk kez veya tür değiştiğinde animasyon başlat
            if (!powerupCountdownPanel.activeSelf || current != lastShownType)
            {
                powerupCountdownPanel.SetActive(true);
                lastShownType = current;

                // Renk ve yazı güncelle
                Color ringColor = current == PowerupType.Multi ? multiColor : slowColor;
                if (countdownRingImage != null) countdownRingImage.color = ringColor;
                if (countdownLabelText != null)
                    countdownLabelText.text = current == PowerupType.Multi ? "X2 PUAN" : "YAVAŞ";

                // Pulse animasyonu başlat
                if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
                pulseCoroutine = StartCoroutine(PulseCountdown());
            }

            // Sayıyı her frame güncelle
            if (countdownNumberText != null) countdownNumberText.text = seconds.ToString();
        }

        /// <summary>
        /// Paneli sürekli nefes alır gibi büyütüp küçültür (1 saniyeye 1 döngü).
        /// </summary>
        private IEnumerator PulseCountdown()
        {
            if (powerupCountdownPanel == null) yield break;
            RectTransform rt = powerupCountdownPanel.GetComponent<RectTransform>();
            if (rt == null) yield break;

            while (powerupCountdownPanel.activeSelf)
            {
                float t = 0f;
                while (t < 1f)
                {
                    t += Time.unscaledDeltaTime;
                    // sin dalgası: 1.00 -> 1.09 -> 1.00
                    float s = 1f + 0.09f * Mathf.Sin(t * Mathf.PI);
                    rt.localScale = Vector3.one * s;
                    yield return null;
                }
            }

            rt.localScale = Vector3.one;
            pulseCoroutine = null;
        }
    }
}

