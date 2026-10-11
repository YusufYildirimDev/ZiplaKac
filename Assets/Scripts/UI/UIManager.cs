using UnityEngine;
using ZiplaKac.Core;

namespace ZiplaKac.UI
{
    /// <summary>
    /// Paneller arası (Ana Menü, Oyun İçi HUD, Oyun Bitti) geçişleri ve
    /// zorluk kademelerine (Tier 0-3) göre arka plan tema renklerinin dinamik uyarlanmasını yöneten ana UI Yöneticisi.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("UI Panelleri")]
        public GameObject mainMenuPanel;
        public GameObject hudPanel;
        public GameObject gameOverPanel;

        [Header("Kamera & Arka Plan")]
        public Camera mainCamera;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (mainCamera == null) mainCamera = Camera.main;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
                GameManager.Instance.OnTierChanged += HandleTierChanged;
                GameManager.Instance.OnComboAchieved += ShowComboFloatingText;
                
                HandleGameStateChanged(GameManager.Instance.State);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
                GameManager.Instance.OnTierChanged -= HandleTierChanged;
                GameManager.Instance.OnComboAchieved -= ShowComboFloatingText;
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(state == GameState.MainMenu);
            if (hudPanel != null) hudPanel.SetActive(state == GameState.Playing);
            if (gameOverPanel != null) gameOverPanel.SetActive(state == GameState.GameOver);

            if (state == GameState.MainMenu)
            {
                SetCameraBackgroundColor(new Color(0.12f, 0.16f, 0.29f)); // #1e2a4a
            }
        }

        private void HandleTierChanged(int tierIndex)
        {
            Color targetColor;
            switch (tierIndex)
            {
                case 0: // Tier 0 (0-39): Modern Gece Mavisi
                    targetColor = new Color(0.12f, 0.16f, 0.29f); // #1e2a4a
                    break;
                case 1: // Tier 1 (40-99): Günbatımı Kızılı
                    targetColor = new Color(1.0f, 0.49f, 0.37f);  // #ff7e5f
                    break;
                case 2: // Tier 2 (100-199): Alacakaranlık Moru
                    targetColor = new Color(0.18f, 0.14f, 0.31f); // #2d2350
                    break;
                case 3: // Tier 3 (200+): Yıldızlı Gece
                    targetColor = new Color(0.04f, 0.06f, 0.16f); // #0b0f2a
                    break;
                default:
                    targetColor = new Color(0.12f, 0.16f, 0.29f);
                    break;
            }

            SetCameraBackgroundColor(targetColor);
        }

        public void SetCameraBackgroundColor(Color color)
        {
            if (mainCamera != null)
            {
                mainCamera.backgroundColor = color;
            }
        }

        private void ShowComboFloatingText(int comboCount, Vector3 worldPosition)
        {
            if (ObjectPooler.Instance != null)
            {
                GameObject txtObj = ObjectPooler.Instance.SpawnFromPool("FloatingText", worldPosition + Vector3.up * 1.2f, Quaternion.identity);
                if (txtObj != null)
                {
                    FloatingText ft = txtObj.GetComponent<FloatingText>();
                    if (ft != null)
                    {
                        ft.SetText($"Kombo x{comboCount}! +10", Color.yellow);
                    }
                }
            }
        }
    }
}
