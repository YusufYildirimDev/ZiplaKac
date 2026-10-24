using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ZiplaKac.Core;
using ZiplaKac.Utils;

namespace ZiplaKac.UI
{
    /// <summary>
    /// Spesifikasyonda ve referans ekran görüntülerinde belirtilen Günlük Ödül Sandığı modalı.
    /// Kapalı ve açık sandık durumları, seri altın ödülü, yeşil kapsül ve lacivert kapat butonları ile birebir eşleşir.
    /// </summary>
    public class DailyRewardModalUI : MonoBehaviour
    {
        public static DailyRewardModalUI Instance { get; private set; }

        public Image chestImage;
        public Sprite closedChestSprite;
        public Sprite openChestSprite;

        public Text titleText;
        public Text descriptionText;
        public Text statusText;
        public Image coinIconImage;

        public Button claimButton;
        public Image claimButtonImage;
        public Text claimButtonText;
        public Sprite claimOpenSprite;
        public Sprite claimClaimedSprite;

        public Button closeButton;
        public Image closeButtonImage;

        private bool hasClaimed = false;

        private void Awake()
        {
            Instance = this;
            LoadSprites();
        }

        public void LoadSprites()
        {
            if (closedChestSprite == null)
            {
                closedChestSprite = Resources.Load<Sprite>("Sprites/UI_DailyReward/chest_closed");
                if (closedChestSprite == null)
                {
                    closedChestSprite = ProceduralSpriteGenerator.GetSpriteOrGenerate("Sprites/treasure_chest", () => ProceduralSpriteGenerator.CreateRectangleSprite(Color.yellow, 128, 128));
                }
            }
            if (openChestSprite == null)
            {
                openChestSprite = Resources.Load<Sprite>("Sprites/UI_DailyReward/chest_open");
                if (openChestSprite == null)
                {
                    openChestSprite = ProceduralSpriteGenerator.GetSpriteOrGenerate("Sprites/treasure_chest_open", () => closedChestSprite);
                }
            }
            string langSuffix = (LocalizationManager.CurrentLanguage == LocalizationManager.LANG_EN) ? "_en" : "";
            claimOpenSprite = Resources.Load<Sprite>("Sprites/UI_DailyReward/btn_sandigi_ac" + langSuffix);
            if (claimOpenSprite == null)
            {
                claimOpenSprite = Resources.Load<Sprite>("Sprites/UI_DailyReward/btn_sandigi_ac");
            }

            claimClaimedSprite = Resources.Load<Sprite>("Sprites/UI_DailyReward/btn_harika" + langSuffix);
            if (claimClaimedSprite == null)
            {
                claimClaimedSprite = Resources.Load<Sprite>("Sprites/UI_DailyReward/btn_harika");
            }

            if (closeButtonImage != null)
            {
                Sprite closeSp = Resources.Load<Sprite>("Sprites/UI_DailyReward/btn_kapat" + langSuffix);
                if (closeSp != null) closeButtonImage.sprite = closeSp;
                else closeButtonImage.sprite = Resources.Load<Sprite>("Sprites/UI_DailyReward/btn_kapat");
            }
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += RefreshUI;
            if (UIManager.Instance != null && UIManager.Instance.mainMenuPanel != null && GameManager.Instance != null && GameManager.Instance.State == GameState.MainMenu)
            {
                UIManager.Instance.mainMenuPanel.SetActive(false);
            }

            LoadSprites();
            RefreshUI();
            StartCoroutine(AnimateChestBreathing());
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= RefreshUI;
            if (UIManager.Instance != null && UIManager.Instance.mainMenuPanel != null && GameManager.Instance != null && GameManager.Instance.State == GameState.MainMenu)
            {
                UIManager.Instance.mainMenuPanel.SetActive(true);
            }
        }

        public void RefreshUI()
        {
            LoadSprites();
            if (titleText != null) titleText.text = LocalizationManager.Get("reward_title");
            if (descriptionText != null)
            {
                descriptionText.text = LocalizationManager.Get("reward_desc");
                descriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
                descriptionText.verticalOverflow = VerticalWrapMode.Truncate;
                descriptionText.resizeTextForBestFit = true;
                descriptionText.resizeTextMinSize = 18;
                descriptionText.resizeTextMaxSize = 28;
                RectTransform dRt = descriptionText.rectTransform;
                if (dRt != null) dRt.sizeDelta = new Vector2(800f, 85f);
            }

            bool canClaim = SaveSystem.CanClaimDailyReward();
            hasClaimed = !canClaim;

            if (chestImage != null)
            {
                chestImage.sprite = hasClaimed ? (openChestSprite != null ? openChestSprite : closedChestSprite) : closedChestSprite;
            }

            if (canClaim)
            {
                if (coinIconImage != null) coinIconImage.gameObject.SetActive(false);
                if (statusText != null)
                {
                    statusText.text = LocalizationManager.Get("reward_ready");
                    statusText.color = new Color(0.00f, 0.87f, 0.56f); // #00DE8E
                }

                if (claimButtonImage != null && claimOpenSprite != null)
                {
                    claimButtonImage.sprite = claimOpenSprite;
                }
                if (claimButtonText != null) claimButtonText.text = "";
                if (claimButton != null) claimButton.interactable = true;
            }
            else
            {
                int streak = SaveSystem.GetDailyStreak();
                int goldReward = streak * 10;
                if (coinIconImage != null) coinIconImage.gameObject.SetActive(true);
                if (statusText != null)
                {
                    statusText.text = LocalizationManager.GetFormat("reward_streak", streak, goldReward);
                    statusText.color = new Color(0.00f, 0.87f, 0.56f); // #00DE8E
                }

                if (claimButtonImage != null && claimClaimedSprite != null)
                {
                    claimButtonImage.sprite = claimClaimedSprite;
                }
                if (claimButtonText != null) claimButtonText.text = "";
                if (claimButton != null) claimButton.interactable = true;
            }
        }

        public void OnClaimButtonClicked()
        {
            if (hasClaimed)
            {
                CloseModal();
                return;
            }

            hasClaimed = true;
            int goldReward = SaveSystem.ClaimDailyReward();
            int streak = SaveSystem.GetDailyStreak();

            // Sandığı anında açık görseline dönüştür
            if (chestImage != null && openChestSprite != null)
            {
                chestImage.sprite = openChestSprite;
            }

            // Butonu "HARİKA!" butonuna çevir
            if (claimButtonImage != null && claimClaimedSprite != null)
            {
                claimButtonImage.sprite = claimClaimedSprite;
            }
            if (claimButtonText != null) claimButtonText.text = "";

            // Durum bilgisini seri ve altın olarak güncelle
            if (coinIconImage != null) coinIconImage.gameObject.SetActive(true);
            if (statusText != null)
            {
                statusText.text = LocalizationManager.GetFormat("reward_streak", streak, goldReward);
                statusText.color = new Color(0.00f, 0.87f, 0.56f); // #00DE8E
            }

            AudioManager.Instance?.PlayNewRecordSound();
            AudioManager.Instance?.TriggerHapticFeedback();

            // Sandık açılma ve zıplama efekti
            StartCoroutine(AnimateChestOpening());
        }

        public void CloseModal()
        {
            AudioManager.Instance?.PlayButtonClickSound();
            gameObject.SetActive(false);
        }

        private IEnumerator AnimateChestBreathing()
        {
            if (chestImage == null) yield break;
            Transform tr = chestImage.transform;

            while (gameObject.activeInHierarchy && !hasClaimed)
            {
                float t = Mathf.Sin(Time.time * 3f) * 0.05f;
                tr.localScale = new Vector3(1f + t, 1f - t, 1f);
                yield return null;
            }
            tr.localScale = Vector3.one;
        }

        private IEnumerator AnimateChestOpening()
        {
            if (chestImage == null) yield break;
            Transform tr = chestImage.transform;

            float elapsed = 0f;
            while (elapsed < 0.45f)
            {
                elapsed += Time.deltaTime;
                float s = 1f + Mathf.Sin(elapsed * Mathf.PI / 0.45f) * 0.28f;
                tr.localScale = Vector3.one * s;
                yield return null;
            }
            tr.localScale = Vector3.one;
        }
    }
}
