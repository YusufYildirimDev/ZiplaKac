using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZiplaKac.Core;
using ZiplaKac.Utils;

namespace ZiplaKac.UI
{
    /// <summary>
    /// İlk indirmede ve ana menüdeki "Nasıl Oynanır" butonuna basıldığında açılan,
    /// 4 adımlı interaktif rehber ve kurallar modalı (Onboarding).
    /// 1. Adım: Temel Kurallar & Zıplama Mekanikleri
    /// 2. Adım: 5 Efsanevi Karakter & Sıralı 2500 Skor Kilit Açma Zinciri
    /// 3. Adım: Güçlendirmeler, Yükseltmeler ve İkinci Şans
    /// 4. Adım: Günlük Ödül Sandığı, Altın Ekonomisi ve Gardırop
    /// </summary>
    public class OnboardingModalUI : MonoBehaviour
    {
        public static OnboardingModalUI Instance { get; private set; }

        public const string KEY_ONBOARDING_SEEN = "ZiplaKac_OnboardingSeen_v1";

        [Header("UI Referansları")]
        public GameObject modalCard;
        public Text stepBadgeText;
        public Text titleText;
        public Button closeButton;
        public Text closeButtonText;
        public Button prevButton;
        public Text prevButtonText;
        public Button nextButton;
        public Text nextButtonText;

        [Header("Slide Alt Başlıkları")]
        public Text[] slideSubTexts;

        [Header("Sayfa Göstergeleri (Dots)")]
        public Image[] pageDots;

        [Header("Slide Kapsayıcıları (4 Adım)")]
        public GameObject[] slides;

        private int currentSlideIndex = 0;

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += UpdateSlideDisplay;
            transform.SetAsLastSibling();

            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }

            if (UIManager.Instance != null && UIManager.Instance.mainMenuPanel != null && GameManager.Instance != null && GameManager.Instance.State == GameState.MainMenu)
            {
                UIManager.Instance.mainMenuPanel.SetActive(false);
            }

            UpdateSlideDisplay();
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= UpdateSlideDisplay;
            if (UIManager.Instance != null && UIManager.Instance.mainMenuPanel != null && GameManager.Instance != null && GameManager.Instance.State == GameState.MainMenu)
            {
                UIManager.Instance.mainMenuPanel.SetActive(true);
            }
        }

        public void OpenModal()
        {
            currentSlideIndex = 0;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            UpdateSlideDisplay();
        }

        public void CloseModal()
        {
            PlayerPrefs.SetInt(KEY_ONBOARDING_SEEN, 1);
            PlayerPrefs.Save();
            gameObject.SetActive(false);
            AudioManager.Instance?.PlayJumpSound();
        }

        public void NextSlide()
        {
            int total = (slides != null && slides.Length > 0) ? slides.Length : 4;
            if (currentSlideIndex < total - 1)
            {
                currentSlideIndex++;
                UpdateSlideDisplay();
                AudioManager.Instance?.PlayJumpSound();
            }
            else
            {
                // Son adımdaysa ("HADİ BAŞLAYALIM! 🚀"), rehberi kapat ve oyunu doğrudan başlat!
                PlayerPrefs.SetInt(KEY_ONBOARDING_SEEN, 1);
                PlayerPrefs.Save();
                gameObject.SetActive(false);
                AudioManager.Instance?.PlayJumpSound();

                if (GameManager.Instance != null && GameManager.Instance.State == GameState.MainMenu)
                {
                    GameManager.Instance.StartGame();
                }
            }
        }

        public void PreviousSlide()
        {
            if (currentSlideIndex > 0)
            {
                currentSlideIndex--;
                UpdateSlideDisplay();
                AudioManager.Instance?.PlayJumpSound();
            }
        }

        private void UpdateSlideDisplay()
        {
            int total = (slides != null && slides.Length > 0) ? slides.Length : 4;
            currentSlideIndex = Mathf.Clamp(currentSlideIndex, 0, total - 1);

            // 1. Slaytları aç/kapat
            if (slides != null)
            {
                for (int i = 0; i < slides.Length; i++)
                {
                    if (slides[i] != null)
                    {
                        slides[i].SetActive(i == currentSlideIndex);
                    }
                }
            }

            // 2. Başlık ve Rozet Metinleri
            bool isEn = LocalizationManager.CurrentLanguage == LocalizationManager.LANG_EN;
            if (stepBadgeText != null)
            {
                stepBadgeText.text = isEn ? $"GUIDE • STEP {currentSlideIndex + 1} / {total}" : $"REHBER • ADIM {currentSlideIndex + 1} / {total}";
            }

            if (closeButtonText != null) closeButtonText.text = LocalizationManager.Get("btn_close");
            if (prevButtonText != null) prevButtonText.text = LocalizationManager.Get("btn_prev");

            if (slideSubTexts != null && currentSlideIndex < slideSubTexts.Length && slideSubTexts[currentSlideIndex] != null)
            {
                switch (currentSlideIndex)
                {
                    case 0:
                        slideSubTexts[0].text = isEn ? "Jump, dodge obstacles, and beat your record!" : "Zıpla, engellerden kaç ve rekorunu kır!";
                        break;
                    case 1:
                        slideSubTexts[1].text = isEn ? "Only Classic is unlocked at first! Reach 2500 score to unlock next:" : "İlk başta sadece Klasik açıktır! Sırayla 2500 skor yaparak kilidi aç:";
                        break;
                    case 2:
                        slideSubTexts[2].text = isEn ? "Collect power-ups and upgrade permanent abilities!" : "Güçlendirmeleri topla ve mağazadan kalıcı yetenekler edin!";
                        break;
                    case 3:
                        slideSubTexts[3].text = isEn ? "Log in daily, open the chest and style your runner!" : "Her gün giriş yap, sandığını aç ve altınlarınla tarzını yarat!";
                        break;
                }
            }

            if (titleText != null)
            {
                switch (currentSlideIndex)
                {
                    case 0:
                        titleText.text = isEn ? "GAME RULES & CONTROLS" : "OYUN KURALLARI & KONTROLLER";
                        break;
                    case 1:
                        titleText.text = isEn ? "5 CHARACTERS & UNLOCK CHAIN" : "5 KARAKTER & KİLİT ZİNCİRİ";
                        break;
                    case 2:
                        titleText.text = isEn ? "POWERS & UPGRADES" : "GÜÇLER & YÜKSELTMELER";
                        break;
                    case 3:
                        titleText.text = isEn ? "DAILY CHEST & WARDROBE" : "GÜNLÜK SANDIK & GARDIROP";
                        break;
                }
            }

            // 3. Buton Yerleşimleri ve Standart Boyutları (Referans mockupa birebir)
            RectTransform nextRt = nextButton != null ? nextButton.GetComponent<RectTransform>() : null;
            RectTransform prevRt = prevButton != null ? prevButton.GetComponent<RectTransform>() : null;

            if (currentSlideIndex == 0)
            {
                // İlk slaytta sadece Sonraki var: Ekran ortasında (480x96)
                if (prevButton != null) prevButton.gameObject.SetActive(false);
                if (nextRt != null)
                {
                    nextRt.anchoredPosition = new Vector2(0f, -575f);
                    nextRt.sizeDelta = new Vector2(480f, 96f);
                }
            }
            else if (currentSlideIndex == total - 1)
            {
                // Son slaytta Önceki (340x96) ve "🚀 HADİ BAŞLAYALIM!" (440x96)
                if (prevButton != null)
                {
                    prevButton.gameObject.SetActive(true);
                    if (prevRt != null)
                    {
                        prevRt.anchoredPosition = new Vector2(-225f, -575f);
                        prevRt.sizeDelta = new Vector2(340f, 96f);
                    }
                }
                if (nextRt != null)
                {
                    nextRt.anchoredPosition = new Vector2(205f, -575f);
                    nextRt.sizeDelta = new Vector2(440f, 96f);
                }
            }
            else
            {
                // Slide 1 ve 2: Önceki (360x96) ve Sonraki (360x96)
                if (prevButton != null)
                {
                    prevButton.gameObject.SetActive(true);
                    if (prevRt != null)
                    {
                        prevRt.anchoredPosition = new Vector2(-215f, -575f);
                        prevRt.sizeDelta = new Vector2(360f, 96f);
                    }
                }
                if (nextRt != null)
                {
                    nextRt.anchoredPosition = new Vector2(215f, -575f);
                    nextRt.sizeDelta = new Vector2(360f, 96f);
                }
            }

            // 4. Sonraki Butonu Metni (Son slaytta "🚀 HADİ BAŞLAYALIM!" olur)
            if (nextButtonText != null)
            {
                nextButtonText.text = (currentSlideIndex == total - 1)
                    ? (isEn ? "🚀 LET'S GO!" : "🚀 HADİ BAŞLAYALIM!")
                    : (isEn ? "NEXT ➔" : "SONRAKİ ➔");
            }

            // 5. Sayfa Göstergeleri (Dots)
            if (pageDots != null)
            {
                for (int i = 0; i < pageDots.Length; i++)
                {
                    if (pageDots[i] != null)
                    {
                        bool isActive = (i == currentSlideIndex);
                        pageDots[i].color = isActive ? new Color(0.024f, 0.839f, 0.627f, 1f) : new Color(1f, 1f, 1f, 0.22f);
                        RectTransform dotRt = pageDots[i].rectTransform;
                        if (dotRt != null)
                        {
                            dotRt.sizeDelta = isActive ? new Vector2(36f, 12f) : new Vector2(14f, 12f);
                        }
                    }
                }
            }
        }
    }
}
