using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ZiplaKac.Core;
using ZiplaKac.Utils;

namespace ZiplaKac.UI
{
    /// <summary>
    /// Spesifikasyonda belirtilen Ödül Sandığı (Treasure Chest) ve Günlük Giriş Ödülü modalı.
    /// Sandık görselini görüntüler, açılma animasyonu oynatır ve bonus puan kazandırır.
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
        public Button claimButton;
        public Text claimButtonText;
        public Button closeButton;

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
                closedChestSprite = ProceduralSpriteGenerator.GetSpriteOrGenerate("Sprites/treasure_chest", () => ProceduralSpriteGenerator.CreateRectangleSprite(Color.yellow, 128, 128));
            }
            if (openChestSprite == null)
            {
                openChestSprite = ProceduralSpriteGenerator.GetSpriteOrGenerate("Sprites/treasure_chest_open", () => closedChestSprite);
            }
        }

        private void OnEnable()
        {
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
            if (UIManager.Instance != null && UIManager.Instance.mainMenuPanel != null && GameManager.Instance != null && GameManager.Instance.State == GameState.MainMenu)
            {
                UIManager.Instance.mainMenuPanel.SetActive(true);
            }
        }

        public void RefreshUI()
        {
            bool canClaim = SaveSystem.CanClaimDailyReward();
            hasClaimed = !canClaim;

            if (chestImage != null)
            {
                chestImage.sprite = hasClaimed ? (openChestSprite != null ? openChestSprite : closedChestSprite) : closedChestSprite;
            }

            if (canClaim)
            {
                if (statusText != null) statusText.text = "🎁 Bugünün sandığı açılmaya hazır!";
                if (claimButtonText != null) claimButtonText.text = "SANDIĞI AÇ";
                if (claimButton != null) claimButton.interactable = true;
            }
            else
            {
                if (statusText != null) statusText.text = "✅ Bugünkü hediyeni aldın! Yarın tekrar gel.";
                if (claimButtonText != null) claimButtonText.text = "ALINDI";
                if (claimButton != null) claimButton.interactable = false;
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

            AudioManager.Instance?.PlayNewRecordSound();
            AudioManager.Instance?.TriggerHapticFeedback();

            // Sandık açılma ve zıplama efekti
            StartCoroutine(AnimateChestOpening());

            if (statusText != null) statusText.text = $"🎉 {streak}. GÜN SERİSİ! +{goldReward} 🪙 ALTIN KAZANDIN!";
            if (claimButtonText != null) claimButtonText.text = "HARİKA!";
        }

        public void CloseModal()
        {
            AudioManager.Instance?.PlayJumpSound();
            gameObject.SetActive(false);
        }

        private IEnumerator AnimateChestBreathing()
        {
            if (chestImage == null) yield break;
            Transform tr = chestImage.transform;

            while (gameObject.activeInHierarchy && !hasClaimed)
            {
                float t = Mathf.Sin(Time.time * 3f) * 0.06f;
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
                float s = 1f + Mathf.Sin(elapsed * Mathf.PI / 0.45f) * 0.35f;
                tr.localScale = Vector3.one * s;
                yield return null;
            }
            tr.localScale = Vector3.one;
        }
    }
}
