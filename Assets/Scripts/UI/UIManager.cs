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
            Color topColor, bottomColor, groundColor;
            bool showStars = (tierIndex == 3);

            switch (tierIndex)
            {
                case 1: // Tier 1 (40-99): Günbatımı
                    topColor = new Color(1.0f, 0.72f, 0.42f);   // #ffb86b
                    bottomColor = new Color(1.0f, 0.49f, 0.37f);// #ff7e5f
                    groundColor = new Color(0.54f, 0.29f, 0.18f);// #8a4a2d
                    break;
                case 2: // Tier 2 (100-199): Alacakaranlık
                    topColor = new Color(0.42f, 0.36f, 0.58f);   // #6b5b95
                    bottomColor = new Color(0.18f, 0.14f, 0.31f);// #2d2350
                    groundColor = new Color(0.23f, 0.16f, 0.33f);// #3a2a55
                    break;
                case 3: // Tier 3 (200+): Yıldızlı Gece
                    topColor = new Color(0.04f, 0.06f, 0.16f);   // #0b0f2a
                    bottomColor = new Color(0.0f, 0.0f, 0.0f);   // #000000
                    groundColor = new Color(0.08f, 0.08f, 0.19f);// #141430
                    break;
                default: // Tier 0 (0-39): Modern Gece Mavisi
                    topColor = new Color(0.12f, 0.16f, 0.29f);   // #1e2a4a
                    bottomColor = new Color(0.05f, 0.07f, 0.13f);// #0d1220
                    groundColor = new Color(0.17f, 0.23f, 0.39f);// #2b3a63
                    break;
            }

            if (gradientBgRenderer != null)
            {
                gradientBgRenderer.sprite = Utils.ProceduralSpriteGenerator.CreateGradientSkySprite(topColor, bottomColor);
            }
            else if (mainCamera != null)
            {
                mainCamera.backgroundColor = topColor;
            }

            if (groundRenderer != null)
            {
                groundRenderer.sprite = Utils.ProceduralSpriteGenerator.CreateGroundSprite(groundColor);
            }

            if (starsContainer != null)
            {
                starsContainer.SetActive(showStars);
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
            if (ObjectPooler.Instance != null)
            {
                GameObject txtObj = ObjectPooler.Instance.SpawnFromPool("FloatingText", worldPosition + Vector3.up * 1.5f, Quaternion.identity);
                if (txtObj != null)
                {
                    FloatingText ft = txtObj.GetComponent<FloatingText>();
                    if (ft != null)
                    {
                        ft.SetText($"Kombo x{comboCount}! +10", new Color(1.0f, 0.84f, 0.04f)); // #ffd60a
                    }
                }
            }
        }
    }
}
