using UnityEngine;
using UnityEngine.UI;
using ZiplaKac.Core;

namespace ZiplaKac.UI
{
    /// <summary>
    /// Oyun içi HUD ekranını yöneten kontrolcü.
    /// Anlık skor, en iyi skor, aktif güçlendirme rozetleri ve kontrol ipucunu gösterir.
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

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
            {
                return;
            }

            // 1. Skor Güncellemeleri
            int scoreInt = Mathf.FloorToInt(GameManager.Instance.CurrentScore);
            if (currentScoreText != null) currentScoreText.text = $"SKOR: {scoreInt}";
            if (highScoreText != null) highScoreText.text = $"EN İYİ: {GameManager.Instance.HighScore}";

            // 2. Aktif Güçlendirme Rozetleri & Kalan Süreler
            UpdatePowerUpBadges();
        }

        private void UpdatePowerUpBadges()
        {
            // Kalkan Rozeti
            bool shield = GameManager.Instance.isShieldActive;
            if (shieldBadgeObject != null) shieldBadgeObject.SetActive(shield);
            if (shieldBadgeText != null && shield) shieldBadgeText.text = "KALKAN (S)";

            // Yavaşlatma Rozeti
            bool slow = GameManager.Instance.isSlowActive;
            if (slowBadgeObject != null) slowBadgeObject.SetActive(slow);
            if (slowBadgeText != null && slow)
            {
                slowBadgeText.text = $"YAVAŞ (T): {GameManager.Instance.slowTimer:F1}s";
            }

            // x2 Skor Rozeti
            bool multi = GameManager.Instance.isDoubleScoreActive;
            if (multiBadgeObject != null) multiBadgeObject.SetActive(multi);
            if (multiBadgeText != null && multi)
            {
                multiBadgeText.text = $"x2 PUAN (2X): {GameManager.Instance.doubleScoreTimer:F1}s";
            }
        }
    }
}
