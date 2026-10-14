using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZiplaKac.Core;

namespace ZiplaKac.Utils
{
    /// <summary>
    /// Zıpla Kaç için çok katmanlı, sonsuz döngülü (seamless loop) ve performansı optimize edilmiş paralaks arka plan yöneticisi.
    /// Uzak dağlar, orta tepeler, hareketli bulutlar ve nabız gibi parlayan gök cismini yönetir.
    /// </summary>
    public class ParallaxBackground : MonoBehaviour
    {
        public static ParallaxBackground Instance { get; private set; }

        [Header("Katman Nesneleri")]
        public Transform[] farMountainTiles;
        public Transform[] midHillTiles;
        public Transform[] cloudTiles;
        public Transform sunTransform;

        [Header("Sprite İşleyicileri (Renk Geçişleri İçin)")]
        public SpriteRenderer[] farMountainRenderers;
        public SpriteRenderer[] midHillRenderers;

        [Header("Hız Çarpanları")]
        public float farMountainFactor = 0.06f;
        public float midHillFactor = 0.20f;
        public float cloudFactor = 0.08f;
        public float ambientCloudSpeed = 0.12f;

        [Header("Karolar Arası Genişlik & Eşik")]
        public float tileWidth = 10.24f;
        public float resetThresholdX = -10.24f;

        private Vector3 sunInitialScale = Vector3.one;
        private Coroutine tierColorCoroutine;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (sunTransform != null) sunInitialScale = sunTransform.localScale;
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTierChanged += HandleTierChanged;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTierChanged -= HandleTierChanged;
            }
            if (tierColorCoroutine != null)
            {
                StopCoroutine(tierColorCoroutine);
                tierColorCoroutine = null;
            }
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                HandleTierChanged(GameManager.Instance.CurrentTierIndex);
            }
        }

        private void Update()
        {
            float effectiveSpeed = 0f;
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Playing)
            {
                effectiveSpeed = GameManager.Instance.GetEffectiveSpeed();
            }

            float dt = Time.deltaTime;

            // 1. Uzak Dağlar Paralaksı
            if (farMountainTiles != null && farMountainTiles.Length >= 2)
            {
                UpdateLayerPair(farMountainTiles[0], farMountainTiles[1], effectiveSpeed * farMountainFactor * dt);
            }

            // 2. Orta Tepeler Paralaksı
            if (midHillTiles != null && midHillTiles.Length >= 2)
            {
                UpdateLayerPair(midHillTiles[0], midHillTiles[1], effectiveSpeed * midHillFactor * dt);
            }

            // 3. Bulutlar Paralaksı (Oyun hızı + sabit ortam rüzgarı)
            if (cloudTiles != null && cloudTiles.Length >= 2)
            {
                float cloudMove = (effectiveSpeed * cloudFactor + ambientCloudSpeed) * dt;
                UpdateLayerPair(cloudTiles[0], cloudTiles[1], cloudMove);
            }

            // 4. Güneş / Ay Hafif Nabız Animasyonu
            if (sunTransform != null)
            {
                float pulse = 1.0f + Mathf.Sin(Time.time * 1.5f) * 0.04f;
                sunTransform.localScale = sunInitialScale * pulse;
            }
        }

        private void UpdateLayerPair(Transform tileA, Transform tileB, float deltaMove)
        {
            if (tileA == null || tileB == null) return;

            tileA.position += Vector3.left * deltaMove;
            tileB.position += Vector3.left * deltaMove;

            // Karo sola doğru tamamen çıkınca diğer karonun sağına yerleştir
            if (tileA.position.x < resetThresholdX)
            {
                tileA.position = new Vector3(tileB.position.x + tileWidth - 0.02f, tileA.position.y, tileA.position.z);
            }
            else if (tileB.position.x < resetThresholdX)
            {
                tileB.position = new Vector3(tileA.position.x + tileWidth - 0.02f, tileB.position.y, tileB.position.z);
            }
        }

        public void HandleTierChanged(int tierIndex)
        {
            Color targetMountainColor;
            Color targetHillColor;

            switch (tierIndex)
            {
                case 1: // Günbatımı
                    targetMountainColor = new Color(1.0f, 0.72f, 0.62f);
                    targetHillColor = new Color(1.0f, 0.80f, 0.70f);
                    break;
                case 2: // Mor Alacakaranlık
                    targetMountainColor = new Color(0.88f, 0.70f, 0.98f);
                    targetHillColor = new Color(0.82f, 0.68f, 0.92f);
                    break;
                case 3: // Gece & Uzay
                    targetMountainColor = new Color(0.55f, 0.58f, 0.82f);
                    targetHillColor = new Color(0.48f, 0.52f, 0.75f);
                    break;
                default: // Tier 0: Modern Siber Mavi
                    targetMountainColor = Color.white;
                    targetHillColor = Color.white;
                    break;
            }

            if (tierColorCoroutine != null) StopCoroutine(tierColorCoroutine);
            tierColorCoroutine = StartCoroutine(SmoothTransitionTierColors(targetMountainColor, targetHillColor, 1.5f));
        }

        private IEnumerator SmoothTransitionTierColors(Color targetMt, Color targetHl, float duration)
        {
            float elapsed = 0f;
            Color startMt = (farMountainRenderers != null && farMountainRenderers.Length > 0 && farMountainRenderers[0] != null) 
                ? farMountainRenderers[0].color : Color.white;
            Color startHl = (midHillRenderers != null && midHillRenderers.Length > 0 && midHillRenderers[0] != null) 
                ? midHillRenderers[0].color : Color.white;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                Color curMt = Color.Lerp(startMt, targetMt, t);
                Color curHl = Color.Lerp(startHl, targetHl, t);

                if (farMountainRenderers != null)
                {
                    foreach (var r in farMountainRenderers) if (r != null) r.color = curMt;
                }
                if (midHillRenderers != null)
                {
                    foreach (var r in midHillRenderers) if (r != null) r.color = curHl;
                }

                yield return null;
            }

            tierColorCoroutine = null;
        }
    }
}
