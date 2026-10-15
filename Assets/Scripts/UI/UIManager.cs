using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZiplaKac.Core;

namespace ZiplaKac.UI
{
    /// <summary>
    /// Paneller arası (Ana Menü, Oyun İçi HUD, Oyun Bitti) geçişleri,
    /// zorluk kademelerine (Tier 0-3) göre degrade gökyüzü ve zemin renklerini,
    /// kamera sarsıntısını (Camera Shake) ve kombo afişlerini yöneten ana UI Yöneticisi.
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
        public SpriteRenderer gradientBgRenderer;
        public GameObject starsContainer;
        public SpriteRenderer groundRenderer;

        private Coroutine shakeCoroutine;
        private Vector3 originalCameraPos = new Vector3(0f, 0f, -10f);

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
            if (mainCamera != null) originalCameraPos = mainCamera.transform.position;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
                GameManager.Instance.OnTierChanged += HandleTierChanged;
                GameManager.Instance.OnComboAchieved += ShowComboFloatingText;
                
                HandleGameStateChanged(GameManager.Instance.State);
                HandleTierChanged(GameManager.Instance.CurrentTierIndex);
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
                HandleTierChanged(0);
            }
        }

        public void HandleTierChanged(int tierIndex)
        {
            Color topColor;
            switch (tierIndex)
            {
                case 1: topColor = new Color(1.0f, 0.72f, 0.42f); break; // Günbatımı
                case 2: topColor = new Color(0.42f, 0.36f, 0.58f); break; // Alacakaranlık
                case 3: topColor = new Color(0.04f, 0.05f, 0.16f); break; // Gece
                default: topColor = new Color(0.68f, 0.91f, 1.0f); break; // Gündüz
            }

            if (mainCamera != null)
            {
                mainCamera.backgroundColor = topColor;
            }

            if (Utils.BackgroundTierManager.Instance != null)
            {
                Utils.BackgroundTierManager.Instance.HandleTierChanged(tierIndex);
            }

            if (Utils.ScrollingGround.Instance != null)
            {
                Utils.ScrollingGround.Instance.HandleTierChanged(tierIndex);
            }

            if (starsContainer != null)
            {
                starsContainer.SetActive(tierIndex == 3);
            }
        }

        public void TriggerCameraShake(float duration = 0.18f, float magnitude = 0.09f)
        {
            if (mainCamera == null) return;
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(DoCameraShake(duration, magnitude));
        }

        private IEnumerator DoCameraShake(float duration, float magnitude)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;
                mainCamera.transform.position = originalCameraPos + new Vector3(x, y, 0f);

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            mainCamera.transform.position = originalCameraPos;
            shakeCoroutine = null;
        }

        private void ShowComboFloatingText(int comboCount, Vector3 worldPosition)
        {
            if (hudPanel != null)
            {
                HUDController hud = hudPanel.GetComponent<HUDController>();
                if (hud != null)
                {
                    hud.ShowCombo(comboCount);
                    return;
                }
            }

            if (ObjectPooler.Instance != null)
            {
                // Ekran ortasında yedek pozisyonlama (asla ekran dışına veya soluna taşmaz)
                Vector3 safePos = new Vector3(0f, 1.5f, 0f);
                GameObject txtObj = ObjectPooler.Instance.SpawnFromPool("FloatingText", safePos, Quaternion.identity);
                if (txtObj != null)
                {
                    FloatingText ft = txtObj.GetComponent<FloatingText>();
                    if (ft != null)
                    {
                        ft.SetText($"KOMBO x{comboCount}! +10", new Color(1.0f, 0.84f, 0.04f));
                    }
                }
            }
        }
    }
}
