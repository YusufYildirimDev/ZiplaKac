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
        public Button prevButton;
        public Button nextButton;
        public Text nextButtonText;

        [Header("Sayfa Göstergeleri (Dots)")]
        public Image[] pageDots;

        [Header("Slide Kapsayıcıları (4 Adım)")]
        public GameObject[] slides;

        private int currentSlideIndex = 0;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            // İlk indirmede veya rehber henüz tamamlanmamışsa otomatik olarak aç
            if (PlayerPrefs.GetInt(KEY_ONBOARDING_SEEN, 0) == 0)
            {
                OpenModal();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void OpenModal()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            currentSlideIndex = 0;
            UpdateSlideDisplay();

            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }

            if (UIManager.Instance != null && UIManager.Instance.mainMenuPanel != null)
            {
                UIManager.Instance.mainMenuPanel.SetActive(false);
            }
        }

        public void CloseModal()
        {
            PlayerPrefs.SetInt(KEY_ONBOARDING_SEEN, 1);
            PlayerPrefs.Save();
            gameObject.SetActive(false);

            if (UIManager.Instance != null && UIManager.Instance.mainMenuPanel != null)
            {
                UIManager.Instance.mainMenuPanel.SetActive(true);
            }

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

                if (GameManager.Instance != null)
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
            if (stepBadgeText != null)
            {
                stepBadgeText.text = $"REHBER • ADIM {currentSlideIndex + 1} / {total}";
            }

            if (titleText != null)
            {
                switch (currentSlideIndex)
                {
                    case 0:
                        titleText.text = "OYUN KURALLARI & KONTROLLER";
                        break;
                    case 1:
                        titleText.text = "5 KARAKTER & KİLİT ZİNCİRİ";
                        break;
                    case 2:
                        titleText.text = "GÜÇLER & YÜKSELTMELER";
                        break;
                    case 3:
                        titleText.text = "GÜNLÜK SANDIK & GARDIROP";
                        break;
                }
            }

            // 3. Buton Yerleşimleri ve Standart Boyutları (Uygulamadaki standart 96px yükseklik)
            RectTransform nextRt = nextButton != null ? nextButton.GetComponent<RectTransform>() : null;
            RectTransform prevRt = prevButton != null ? prevButton.GetComponent<RectTransform>() : null;

            if (currentSlideIndex == 0)
            {
                // İlk slaytta sadece Sonraki var: Ekran ortasında tam boy (540x96)
                if (prevButton != null) prevButton.gameObject.SetActive(false);
                if (nextRt != null)
                {
                    nextRt.anchoredPosition = new Vector2(0f, -630f);
                    nextRt.sizeDelta = new Vector2(540f, 96f);
                }
            }
            else
            {
                // Diğer slaytlarda hem Önceki hem Sonraki var (380x96)
                if (prevButton != null)
                {
                    prevButton.gameObject.SetActive(true);
                    if (prevRt != null)
                    {
                        prevRt.anchoredPosition = new Vector2(-210f, -630f);
                        prevRt.sizeDelta = new Vector2(380f, 96f);
                    }
                }
                if (nextRt != null)
                {
                    nextRt.anchoredPosition = new Vector2(210f, -630f);
                    nextRt.sizeDelta = new Vector2(380f, 96f);
                }
            }

            // 4. Sonraki Butonu Metni (Son slaytta "HADİ BAŞLAYALIM! 🚀" olur)
            if (nextButtonText != null)
            {
                nextButtonText.text = (currentSlideIndex == total - 1) ? "HADİ BAŞLAYALIM! 🚀" : "SONRAKİ ➔";
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
