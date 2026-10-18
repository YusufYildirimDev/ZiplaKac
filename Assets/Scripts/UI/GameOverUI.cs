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
        public Text revivePriceText;

        public Button restartButton;
        public Button mainMenuButton;
        public Button rewardedAdButton;
        public Text rewardedAdButtonText;

        private void OnEnable()
        {
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
                if (rewardedAdButtonText != null) rewardedAdButtonText.text = "REKLAM İZLE: +100 ALTIN";
            }

            RefreshGameOverStats();
        }

        private void OnRewardedAdButtonClicked()
        {
            MonetizationManager.Instance?.ShowRewardedAd(
                () => {
                    SaveSystem.AddGold(100);
                    RefreshGameOverStats();
                    AudioManager.Instance?.PlayNewRecordSound();
                    AudioManager.Instance?.TriggerHapticFeedback();
                    if (rewardedAdButton != null) rewardedAdButton.interactable = false;
                    if (rewardedAdButtonText != null) rewardedAdButtonText.text = "🎉 +100 ALTIN KAZANDIN!";
                },
                () => {
                    Debug.Log("Reklam gösterilemedi.");
                }
            );
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

            int finalScore = Mathf.FloorToInt(GameManager.Instance.CurrentScore);
            int highScore = GameManager.Instance.HighScore;

            if (finalScoreText != null) finalScoreText.text = $"{finalScore:N0}";
            if (highScoreText != null) highScoreText.text = $"🏆 En İyi Skor: {highScore:N0}";

            if (goldEarnedText != null)
            {
                int runGold = GameManager.Instance.CurrentRunGold;
                int totalGold = SaveSystem.GetTotalGold();
                goldEarnedText.text = $"+{runGold} ALTIN KAZANDIN   •   KASA: {totalGold} ALTIN";
            }

            // Yeni Rekor Rozeti Kontrolü
            bool isNewRecord = finalScore >= highScore && finalScore > 0;
            if (newRecordBadge != null)
            {
                newRecordBadge.SetActive(isNewRecord);
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
                        string charName = SaveSystem.GetCharacterDisplayName(GameManager.NewlyUnlockedCharacterId);
                        newCharacterUnlockedText.text = $"🎉 YENİ KARAKTER AÇILDI: {charName.ToUpper()}! 🎉";
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
                    scoreHistoryListText.text = "Son Skorlar: " + string.Join(" • ", history);
                }
            }

            // Can Alma (Revive) Butonu Durumu
            int currentGold = SaveSystem.GetTotalGold();
            if (reviveButton != null)
            {
                bool canAfford = currentGold >= 500;
                reviveButton.interactable = canAfford;

                if (reviveButtonImage != null)
                {
                    reviveButtonImage.color = canAfford ? new Color(0.94f, 0.28f, 0.44f, 1f) : new Color(0.32f, 0.18f, 0.24f, 0.85f);
                }

                if (reviveButtonText != null)
                {
                    reviveButtonText.text = canAfford ? "CAN AL & DEVAM ET" : "CAN AL";
                    reviveButtonText.color = canAfford ? Color.white : new Color(0.75f, 0.75f, 0.75f);
                }

                if (revivePriceText != null)
                {
                    revivePriceText.text = canAfford ? "500" : "500 GEREKİR";
                    revivePriceText.color = canAfford ? new Color(1f, 0.90f, 0.2f) : new Color(0.9f, 0.4f, 0.4f);
                }

                if (reviveCoinIcon != null)
                {
                    reviveCoinIcon.gameObject.SetActive(true);
                }
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
            if (GameManager.Instance != null && GameManager.Instance.ReviveWithGold(500))
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
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame();
            }
        }

        private void OnMainMenuButtonClicked()
        {
            AudioManager.Instance?.PlayJumpSound();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.MainMenu);
            }
        }
    }
}
