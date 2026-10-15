using System.Collections;
using UnityEngine;
using ZiplaKac.Core;

namespace ZiplaKac.Utils
{
    /// <summary>
    /// Zıpla Kaç için 4 kademe (Gündüz, Günbatımı, Alacakaranlık, Gece) atmosferik arka plan yöneticisi.
    /// Kademe değişimlerinde (Tier 0-3) arka plan görselleri arasında pürüzsüz cross-fade geçişi sağlar.
    /// </summary>
    public class BackgroundTierManager : MonoBehaviour
    {
        public static BackgroundTierManager Instance { get; private set; }

        [Header("Kademe Arka Plan Sprite'ları (Tier 0-3)")]
        public Sprite[] tierSprites = new Sprite[4];

        [Header("Sprite İşleyicileri (Cross-fade için çift katman)")]
        public SpriteRenderer primaryRenderer;
        public SpriteRenderer fadeRenderer;

        [Header("Geçiş Süresi")]
        public float fadeDuration = 1.2f;

        private int currentTier = 0;
        private Coroutine fadeCoroutine;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
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
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                HandleTierChanged(GameManager.Instance.CurrentTierIndex);
            }
            else
            {
                SetInitialTier(0);
            }
        }

        public void SetInitialTier(int tierIndex)
        {
            currentTier = Mathf.Clamp(tierIndex, 0, 3);
            if (tierSprites != null && currentTier < tierSprites.Length && tierSprites[currentTier] != null)
            {
                if (primaryRenderer != null)
                {
                    primaryRenderer.sprite = tierSprites[currentTier];
                    primaryRenderer.color = Color.white;
                }
                if (fadeRenderer != null)
                {
                    fadeRenderer.sprite = null;
                    fadeRenderer.color = new Color(1f, 1f, 1f, 0f);
                }
            }
        }

        public void HandleTierChanged(int tierIndex)
        {
            int targetTier = Mathf.Clamp(tierIndex, 0, 3);
            if (targetTier == currentTier && primaryRenderer != null && primaryRenderer.sprite != null) return;

            if (tierSprites == null || targetTier >= tierSprites.Length || tierSprites[targetTier] == null) return;

            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(DoCrossFade(targetTier));
        }

        private IEnumerator DoCrossFade(int newTier)
        {
            Sprite nextSprite = tierSprites[newTier];
            if (fadeRenderer != null)
            {
                fadeRenderer.sprite = nextSprite;
                fadeRenderer.color = new Color(1f, 1f, 1f, 0f);
            }

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);

                if (fadeRenderer != null)
                {
                    fadeRenderer.color = new Color(1f, 1f, 1f, t);
                }

                yield return null;
            }

            // Geçiş tamamlandı
            if (primaryRenderer != null)
            {
                primaryRenderer.sprite = nextSprite;
                primaryRenderer.color = Color.white;
            }
            if (fadeRenderer != null)
            {
                fadeRenderer.color = new Color(1f, 1f, 1f, 0f);
                fadeRenderer.sprite = null;
            }

            currentTier = newTier;
            fadeCoroutine = null;
        }
    }
}
