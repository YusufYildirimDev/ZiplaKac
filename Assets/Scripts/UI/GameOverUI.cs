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
        public GameObject newRecordBadge;

        [Header("Skor Geçmişi")]
        public Text scoreHistoryListText;

        [Header("Butonlar")]
        public Button restartButton;
        public Button mainMenuButton;

        private void OnEnable()
        {
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

            RefreshGameOverStats();
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

            if (finalScoreText != null) finalScoreText.text = $"{finalScore}";
            if (highScoreText != null) highScoreText.text = $"EN İYİ SKOR: {highScore}";

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

            // Son 5 Skor Listesi
            List<int> history = SaveSystem.GetLastFiveScores();
            if (scoreHistoryListText != null)
            {
                if (history == null || history.Count == 0)
                {
                    scoreHistoryListText.text = "Henüz kayıtlı skor bulunmuyor.";
                }
                else
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.AppendLine("SON SKORLAR:");
                    for (int i = 0; i < history.Count; i++)
                    {
                        sb.AppendLine($"{i + 1}.  {history[i]} Puan");
                    }
                    scoreHistoryListText.text = sb.ToString();
                }
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
