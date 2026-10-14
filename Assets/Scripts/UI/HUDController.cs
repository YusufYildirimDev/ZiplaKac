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
