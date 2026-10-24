using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ZiplaKac.Core;
using ZiplaKac.Utils;

namespace ZiplaKac.UI
{
    /// <summary>
    /// Oyun içi Duraklatma (Pause) modal penceresini yöneten sınıf.
    /// Devam et, Yeniden başla, Ses aç/kapat ve Ana Menü butonlarını içerir.
    /// Oyun duraklatıldığında arka planı karartır ve koşu özetini gösterir.
    /// </summary>
    public class PauseModalUI : MonoBehaviour
    {
        public static PauseModalUI Instance { get; private set; }

        [Header("Metin Bileşenleri")]
        public Text titleText;
        public Text scoreSummaryText;

        [Header("Butonlar")]
        public Button resumeButton;
        public Text resumeButtonText;

        public Button restartButton;
        public Text restartButtonText;

        public Button soundButton;
        public Text soundButtonText;

        public Button mainMenuButton;
        public Text mainMenuButtonText;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            SetupButtons();
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGamePaused += HandleGamePaused;
            }

            LocalizationManager.OnLanguageChanged += RefreshTexts;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.OnMuteStateChanged += HandleMuteChanged;
            }

            RefreshTexts();
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGamePaused -= HandleGamePaused;
            }
            LocalizationManager.OnLanguageChanged -= RefreshTexts;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.OnMuteStateChanged -= HandleMuteChanged;
            }
        }

        private void Update()
        {
            // Android Geri Tuşu veya Klavye ESC tuşu ile duraklatmayı kapat
            if (gameObject.activeSelf && (Input.GetKeyDown(KeyCode.Escape)))
            {
                OnResumeClicked();
            }
        }

        private void SetupButtons()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveAllListeners();
                resumeButton.onClick.AddListener(OnResumeClicked);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(OnRestartClicked);
            }

            if (soundButton != null)
            {
                soundButton.onClick.RemoveAllListeners();
                soundButton.onClick.AddListener(OnSoundToggleClicked);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveAllListeners();
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }
        }

        private void HandleGamePaused(bool isPaused)
        {
            if (isPaused)
            {
                UpdateScoreSummary();
                RefreshTexts();
                gameObject.SetActive(true);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void HandleMuteChanged(bool isMuted)
        {
            UpdateSoundButtonText();
        }

        private void UpdateScoreSummary()
        {
            if (scoreSummaryText == null) return;

            int curScore = (GameManager.Instance != null) ? Mathf.FloorToInt(GameManager.Instance.CurrentScore) : 0;
            int curGold = (GameManager.Instance != null) ? GameManager.Instance.CurrentRunGold : 0;

            string scoreLbl = LocalizationManager.Get("score");
            string goldLbl = LocalizationManager.Get("gold");
            scoreSummaryText.text = $"{scoreLbl}: <b>{curScore}</b>   •   {goldLbl}: <b>{curGold}</b>";
        }

        public void RefreshTexts()
        {
            if (titleText != null)
            {
                titleText.text = LocalizationManager.Get("pause_title");
            }

            if (resumeButtonText != null)
            {
                resumeButtonText.text = LocalizationManager.Get("pause_btn_resume");
            }

            if (restartButtonText != null)
            {
                restartButtonText.text = LocalizationManager.Get("pause_btn_restart");
            }

            if (mainMenuButtonText != null)
            {
                mainMenuButtonText.text = LocalizationManager.Get("pause_btn_menu");
            }

            UpdateSoundButtonText();
            UpdateScoreSummary();
        }

        private void UpdateSoundButtonText()
        {
            if (soundButtonText == null) return;
            bool isMuted = AudioManager.Instance != null && AudioManager.Instance.IsMuted;
            string key = isMuted ? "pause_sound_off" : "pause_sound_on";
            soundButtonText.text = LocalizationManager.Get(key);
        }

        private void OnResumeClicked()
        {
            AudioManager.Instance?.PlayButtonClickSound();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResumeGame();
            }
            else
            {
                Time.timeScale = 1.0f;
                gameObject.SetActive(false);
            }
        }

        private void OnRestartClicked()
        {
            AudioManager.Instance?.PlayButtonClickSound();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResumeGame();
                GameManager.Instance.StartGame();
            }
            gameObject.SetActive(false);
        }

        private void OnSoundToggleClicked()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.ToggleMute();
                AudioManager.Instance.PlayButtonClickSound();
                UpdateSoundButtonText();
            }
        }

        private void OnMainMenuClicked()
        {
            AudioManager.Instance?.PlayButtonClickSound();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResumeGame();
                GameManager.Instance.SetState(GameState.MainMenu);
            }
            gameObject.SetActive(false);
        }
    }
}
