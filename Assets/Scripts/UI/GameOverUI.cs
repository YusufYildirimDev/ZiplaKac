using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZiplaKac.Core;

namespace ZiplaKac.UI
{
    /// <summary>
    /// Oyun bitti (GameOver) ekran kontrolcüsü.
    /// Final skorunu, en yüksek skoru, yeni rekor rozetini ve son 5 oyun skor geçmişini gösterir.
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        [Header("Başlık ve Etiket Metinleri")]
        public Text gameOverTitleText;
        public Text gameOverSubText;
        public Text gameOverScoreLabelText;

        [Header("Skor Metinleri")]
        public Text finalScoreText;
        public Text highScoreText;
        public Text goldEarnedText;
        public GameObject newRecordBadge;

        [Header("Skor Geçmişi")]
        public Text scoreHistoryListText;

        [Header("Butonlar")]
        public Button reviveButton;
        public Text reviveButtonText;
        public Image reviveButtonImage;
        public Image reviveCoinIcon;
        public Image reviveHeartIcon;
        public Text revivePriceText;

        public Button restartButton;
        public Text restartButtonText;
        public Button mainMenuButton;
        public Text mainMenuButtonText;
        public Button rewardedAdButton;
        public Text rewardedAdButtonText;
        public GameObject rewardedAdBadge;

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += RefreshGameOverStats;

            if (reviveButton != null)
            {
                reviveButton.onClick.RemoveAllListeners();
                reviveButton.onClick.AddListener(OnReviveButtonClicked);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(OnRestartButtonClicked);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveAllListeners();
                mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);
            }

            if (rewardedAdButton != null)
            {
                rewardedAdButton.interactable = true;
                rewardedAdButton.onClick.RemoveAllListeners();
                rewardedAdButton.onClick.AddListener(OnRewardedAdButtonClicked);
                if (rewardedAdBadge != null) rewardedAdBadge.SetActive(true);
                if (rewardedAdButtonText != null)
                {
                    rewardedAdButtonText.text = LocalizationManager.Get("go_btn_ad");
                }
            }

            RefreshGameOverStats();
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= RefreshGameOverStats;
        }

        private void OnRewardedAdButtonClicked()
        {
            if (MonetizationManager.Instance != null)
            {
                MonetizationManager.Instance.ShowRewardedAd(
                    () => {
                        SaveSystem.AddGold(100);
                        RefreshGameOverStats();
                        AudioManager.Instance?.PlayNewRecordSound();
                        AudioManager.Instance?.TriggerHapticFeedback();
                        if (rewardedAdButton != null) rewardedAdButton.interactable = false;
                        if (rewardedAdBadge != null) rewardedAdBadge.SetActive(false);
                        if (rewardedAdButtonText != null)
                        {
                            rewardedAdButtonText.text = LocalizationManager.Get("go_btn_ad_claimed");
                        }
                    },
                    () => {
                        Debug.Log("Reklam gösterilemedi.");
                    }
                );
            }
            else
            {
                SaveSystem.AddGold(100);
                RefreshGameOverStats();
                AudioManager.Instance?.PlayNewRecordSound();
                AudioManager.Instance?.TriggerHapticFeedback();
                if (rewardedAdButton != null) rewardedAdButton.interactable = false;
                if (rewardedAdBadge != null) rewardedAdBadge.SetActive(false);
                if (rewardedAdButtonText != null)
                {
                    rewardedAdButtonText.text = LocalizationManager.Get("go_btn_ad_claimed");
                }
            }
        }

        private void Update()
        {
            try
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                {
                    OnRestartButtonClicked();
                    return;
                }
            }
            catch { }

            try
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                {
                    OnRestartButtonClicked();
                }
            }
            catch { }
        }

        public void RefreshGameOverStats()
        {
            if (GameManager.Instance == null) return;

            if (gameOverTitleText != null) gameOverTitleText.text = LocalizationManager.Get("go_title");
            if (gameOverSubText != null) gameOverSubText.text = LocalizationManager.Get("go_sub");
            if (gameOverScoreLabelText != null) gameOverScoreLabelText.text = LocalizationManager.Get("go_score_label");

            int finalScore = Mathf.FloorToInt(GameManager.Instance.CurrentScore);
            int highScore = GameManager.Instance.HighScore;

            if (finalScoreText != null) finalScoreText.text = $"{finalScore:N0}";
            if (highScoreText != null) highScoreText.text = LocalizationManager.GetFormat("go_high_score", highScore.ToString("N0"));

            if (goldEarnedText != null)
            {
                int runGold = GameManager.Instance.CurrentRunGold;
                int totalGold = SaveSystem.GetTotalGold();
                goldEarnedText.supportRichText = true;
                goldEarnedText.text = LocalizationManager.GetFormat("go_gold_earned", runGold, totalGold.ToString("N0"));
                if (goldEarnedText.transform.parent != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(goldEarnedText.transform.parent.GetComponent<RectTransform>());
                }
            }

            // Yeni Rekor Rozeti Kontrolü
            bool isNewRecord = finalScore >= highScore && finalScore > 0;
            if (newRecordBadge != null)
            {
                newRecordBadge.SetActive(isNewRecord);
                Text badgeText = newRecordBadge.GetComponentInChildren<Text>();
                if (badgeText != null) badgeText.text = LocalizationManager.Get("go_new_record");
            }

            if (isNewRecord)
            {
                AudioManager.Instance?.PlayNewRecordSound();
            }

            // Yeni Karakter Kilidi Açılma Kutlaması
            if (!string.IsNullOrEmpty(GameManager.NewlyUnlockedCharacterId))
            {
                EnsureUnlockBadge();
                if (newCharacterUnlockedBadge != null)
                {
                    newCharacterUnlockedBadge.SetActive(true);
                    if (newCharacterUnlockedText != null)
                    {
                        string charName = LocalizationManager.Get($"char_{GameManager.NewlyUnlockedCharacterId}_name");
                        if (string.IsNullOrEmpty(charName) || charName == $"char_{GameManager.NewlyUnlockedCharacterId}_name")
                        {
                            charName = SaveSystem.GetCharacterDisplayName(GameManager.NewlyUnlockedCharacterId);
                        }
                        newCharacterUnlockedText.text = LocalizationManager.GetFormat("char_unlocked_toast", charName.ToUpper());
                    }
                }
            }
            else if (newCharacterUnlockedBadge != null)
            {
                newCharacterUnlockedBadge.SetActive(false);
            }

            // Son 5 Skor Listesi
            List<int> history = SaveSystem.GetLastFiveScores();
            if (scoreHistoryListText != null)
            {
                if (history == null || history.Count == 0)
                {
                    scoreHistoryListText.text = "";
                }
                else
                {
                    scoreHistoryListText.text = LocalizationManager.GetFormat("go_last_scores", string.Join(" • ", history));
                }
            }

            // Can Alma (Revive) Butonu Durumu
            int extraLives = SaveSystem.GetConsumableCount("second_chance");
            int currentGold = SaveSystem.GetTotalGold();
            bool canReviveWithLife = extraLives > 0;
            bool canReviveWithGold = currentGold >= 500;
            bool canRevive = canReviveWithLife || canReviveWithGold;

            if (reviveButton != null)
            {
                reviveButton.interactable = canRevive;

                if (reviveButtonText != null)
                {
                    if (canReviveWithLife)
                    {
                        reviveButtonText.text = LocalizationManager.Get("go_btn_revive_life", "CAN KULLAN\nDEVAM ET");
                        reviveButtonText.color = Color.white;
                    }
                    else if (canReviveWithGold)
                    {
                        reviveButtonText.text = LocalizationManager.Get("go_btn_revive_afford", "CAN AL\nDEVAM ET");
                        reviveButtonText.color = Color.white;
                    }
                    else
                    {
                        reviveButtonText.text = LocalizationManager.Get("go_btn_revive_cost", "CAN AL\n(500 ALTIN)");
                        reviveButtonText.color = new Color(1f, 1f, 1f, 0.45f);
                    }
                }

                if (reviveHeartIcon != null)
                {
                    reviveHeartIcon.color = canRevive ? Color.white : new Color(1f, 1f, 1f, 0.40f);
                }
            }

            if (restartButtonText != null)
            {
                restartButtonText.text = LocalizationManager.Get("go_btn_retry");
            }
            if (mainMenuButtonText != null)
            {
                mainMenuButtonText.text = LocalizationManager.Get("go_btn_menu");
            }
        }

        private GameObject newCharacterUnlockedBadge;
        private Text newCharacterUnlockedText;

        private void EnsureUnlockBadge()
        {
            if (newCharacterUnlockedBadge != null) return;

            Transform parentTransform = newRecordBadge != null ? newRecordBadge.transform.parent : transform;
            newCharacterUnlockedBadge = new GameObject("NewCharacterUnlockedBadge");
            newCharacterUnlockedBadge.transform.SetParent(parentTransform, false);

            RectTransform rt = newCharacterUnlockedBadge.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0f, 148f);
            rt.sizeDelta = new Vector2(750f, 50f);

            Image bg = newCharacterUnlockedBadge.AddComponent<Image>();
            bg.sprite = Utils.ProceduralSpriteGenerator.CreateRoundedRectSprite(
                new Color(0.10f, 0.28f, 0.18f, 0.95f),
                new Color(0.024f, 0.839f, 0.627f, 0.95f),
                2f, 18
            );
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = false;

            GameObject textObj = new GameObject("UnlockText");
            textObj.transform.SetParent(newCharacterUnlockedBadge.transform, false);
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero; textRt.offsetMax = Vector2.zero;

            newCharacterUnlockedText = textObj.AddComponent<Text>();
            try
            {
                newCharacterUnlockedText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            } catch {}
            if (newCharacterUnlockedText.font == null)
            {
                Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
                if (fonts != null && fonts.Length > 0) newCharacterUnlockedText.font = fonts[0];
            }
            newCharacterUnlockedText.fontSize = 24;
            newCharacterUnlockedText.fontStyle = FontStyle.Bold;
            newCharacterUnlockedText.alignment = TextAnchor.MiddleCenter;
            newCharacterUnlockedText.color = new Color(0.024f, 0.839f, 0.627f);
            newCharacterUnlockedText.raycastTarget = false;
        }

        private void OnReviveButtonClicked()
        {
            if (GameManager.Instance != null && GameManager.Instance.RevivePlayer(500))
            {
                gameObject.SetActive(false);
            }
            else
            {
                AudioManager.Instance?.PlayGameOverSound();
                AudioManager.Instance?.TriggerHapticFeedback();
            }
        }

        private void OnRestartButtonClicked()
        {
            AudioManager.Instance?.PlayJumpSound();
            MonetizationManager.Instance?.CheckAndShowInterstitialOnGameOver();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame();
            }
        }

        private void OnMainMenuButtonClicked()
        {
            AudioManager.Instance?.PlayJumpSound();
            MonetizationManager.Instance?.CheckAndShowInterstitialOnGameOver();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.MainMenu);
            }
        }
    }
}
