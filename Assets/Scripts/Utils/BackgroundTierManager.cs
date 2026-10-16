using System.Collections;
using UnityEngine;
using ZiplaKac.Core;

namespace ZiplaKac.Utils
{
    /// <summary>
    /// Zıpla Kaç için 5 Karakter × 3 Aşama tematik arka plan yöneticisi.
    /// Dünya ve kademe değişimlerinde arka plan görselleri arasında pürüzsüz cross-fade geçişi sağlar.
    /// </summary>
    public class BackgroundTierManager : MonoBehaviour
    {
        public static BackgroundTierManager Instance { get; private set; }

        [Header("Kademe Arka Plan Sprite'ları (Yedek / Fallback)")]
        public Sprite[] tierSprites = new Sprite[4];

        [Header("Sprite İşleyicileri (Cross-fade için çift katman)")]
        public SpriteRenderer primaryRenderer;
        public SpriteRenderer fadeRenderer;

        [Header("Geçiş Süresi")]
        public float fadeDuration = 0.8f;

        private int currentTier = 0;
        private string currentCharacterKey = "klasik";
        private Coroutine fadeCoroutine;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void OnEnable()
        {
            if (WorldThemeManager.Instance != null)
            {
                WorldThemeManager.Instance.OnWorldThemeChanged += HandleWorldThemeChanged;
            }
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTierChanged += HandleTierChanged;
            }
        }

        private void OnDisable()
        {
            if (WorldThemeManager.Instance != null)
            {
                WorldThemeManager.Instance.OnWorldThemeChanged -= HandleWorldThemeChanged;
            }
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
            if (WorldThemeManager.Instance != null)
            {
                currentCharacterKey = WorldThemeManager.Instance.CurrentCharacterKey;
                currentTier = WorldThemeManager.Instance.CurrentStageIndex;
                Sprite sp = WorldThemeManager.Instance.GetBackgroundSprite(currentCharacterKey, currentTier);
                if (sp != null && primaryRenderer != null)
                {
                    primaryRenderer.sprite = sp;
                    primaryRenderer.color = Color.white;
                }
            }
            else if (GameManager.Instance != null)
            {
                HandleTierChanged(GameManager.Instance.CurrentTierIndex);
            }
            else
            {
                SetInitialTier(0);
            }
        }

        public void HandleWorldThemeChanged(string characterKey, int stageIndex)
        {
            int targetTier = Mathf.Clamp(stageIndex, 0, 2);
            bool isCharacterChange = characterKey != currentCharacterKey;

            Sprite nextSprite = null;
            if (WorldThemeManager.Instance != null)
            {
                nextSprite = WorldThemeManager.Instance.GetBackgroundSprite(characterKey, targetTier);
            }

            if (nextSprite == null) return;

            if (isCharacterChange || primaryRenderer.sprite == null)
            {
                // Menüde karakter değiştiğinde veya ilk yüklemede anında uygula
                if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
                if (primaryRenderer != null)
                {
                    primaryRenderer.sprite = nextSprite;
                    primaryRenderer.color = Color.white;
                }
                if (fadeRenderer != null)
                {
                    fadeRenderer.sprite = null;
                    fadeRenderer.color = new Color(1f, 1f, 1f, 0f);
                }
                currentCharacterKey = characterKey;
                currentTier = targetTier;
            }
            else if (targetTier != currentTier)
            {
                // Oyun içinde aşama ilerlediğinde pürüzsüz cross-fade geçişi
                if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
                fadeCoroutine = StartCoroutine(DoCrossFade(nextSprite, targetTier));
                currentCharacterKey = characterKey;
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
            if (WorldThemeManager.Instance != null)
            {
                WorldThemeManager.Instance.SetStage(tierIndex);
                return;
            }

            int targetTier = Mathf.Clamp(tierIndex, 0, 3);
            if (targetTier == currentTier && primaryRenderer != null && primaryRenderer.sprite != null) return;

            if (tierSprites == null || targetTier >= tierSprites.Length || tierSprites[targetTier] == null) return;

            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(DoCrossFade(tierSprites[targetTier], targetTier));
        }

        private IEnumerator DoCrossFade(Sprite nextSprite, int targetTier)
        {
            if (nextSprite == null) yield break;

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

            // Geçiş tamamlandı: birincil katmana aktar
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

            currentTier = targetTier;
            fadeCoroutine = null;
        }
    }
}
