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

        public Button privacyButton;
        public Text privacyButtonText;
        public const string PRIVACY_POLICY_URL = "https://yusufyildirimdev.github.io/ZiplaKac/privacy_policy.html";

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
            TrySubscribeGameManager();
            LocalizationManager.OnLanguageChanged += RefreshTexts;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.OnMuteStateChanged += HandleMuteChanged;
            }
            RefreshTexts();
        }

        private void Start()
        {
            TrySubscribeGameManager();
        }

        private void TrySubscribeGameManager()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGamePaused -= HandleGamePaused;
                GameManager.Instance.OnGamePaused += HandleGamePaused;
            }
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

            if (privacyButton != null)
            {
                privacyButton.onClick.RemoveAllListeners();
                privacyButton.onClick.AddListener(OnPrivacyClicked);
            }
        }

        private void HandleGamePaused(bool isPaused)
        {
            // Kullanıcı tercihi: Duraklatıldığında herhangi bir ekran/modal açılmayacak.
            // Oyun sadece sağ üstteki buton ile doğrudan durdurulup devam ettirilir.
            gameObject.SetActive(false);
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

            if (privacyButtonText != null)
            {
                privacyButtonText.text = LocalizationManager.Get("privacy_policy_btn");
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

        private void OnPrivacyClicked()
        {
            AudioManager.Instance?.PlayButtonClickSound();
            Application.OpenURL(PRIVACY_POLICY_URL);
        }
    }
}
