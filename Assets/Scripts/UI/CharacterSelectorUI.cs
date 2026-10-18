using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZiplaKac.Core;

namespace ZiplaKac.UI
{
    [System.Serializable]
    public class CharacterCardUI
    {
        public string characterId;
        public string characterName;
        public Color bodyColor;
        public Button selectButton;
        public Image borderHighlight;
        public Image iconImage;
        public Text nameText;
        public GameObject lockOverlay;
    }

    /// <summary>
    /// Ana menüdeki 5 varyantlı (Klasik, Ateş, Buz, Zehir, Gece) karakter seçim galerisi.
    /// Başlangıçta sadece Klasik açıktır; sonraki avatarlar sırayla 2500 skor yapılarak açılır.
    /// Kilitli karakterlere dokunulduğunda görsel/işitsel uyarı verir.
    /// </summary>
    public class CharacterSelectorUI : MonoBehaviour
    {
        [Header("Karakter Galerisi Kartları")]
        public List<CharacterCardUI> characterCards = new List<CharacterCardUI>();

        private string currentSelectedId;
        private GameObject lockNoticeObj;
        private Text lockNoticeText;
        private Coroutine lockNoticeCoroutine;

        private void Start()
        {
            SaveSystem.EnsureAvatarChainInitialized();
            currentSelectedId = SaveSystem.GetSelectedCharacterId("classic");
            if (!SaveSystem.IsCharacterUnlocked(currentSelectedId))
            {
                currentSelectedId = "classic";
                SaveSystem.SaveSelectedCharacterId("classic");
            }

            SetupCards();
            CreateLockNoticeBanner();
            RefreshSelectionVisuals();
            WorldThemeManager.Instance?.SetCharacter(currentSelectedId);
        }

        private void OnEnable()
        {
            SaveSystem.EnsureAvatarChainInitialized();
            currentSelectedId = SaveSystem.GetSelectedCharacterId("classic");
            if (!SaveSystem.IsCharacterUnlocked(currentSelectedId))
            {
                currentSelectedId = "classic";
                SaveSystem.SaveSelectedCharacterId("classic");
            }

            RefreshSelectionVisuals();
            WorldThemeManager.Instance?.SetCharacter(currentSelectedId);
        }

        private static Font GetSafeFont()
        {
            try
            {
                Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (f != null) return f;
            } catch {}
            try
            {
                Font f = Resources.GetBuiltinResource<Font>("Arial.ttf");
                if (f != null) return f;
            } catch {}
            Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
            return fonts != null && fonts.Length > 0 ? fonts[0] : null;
        }

        private void CreateLockNoticeBanner()
        {
            if (lockNoticeObj != null) return;

            lockNoticeObj = new GameObject("LockNoticeBanner");
            lockNoticeObj.transform.SetParent(transform, false);
            RectTransform rt = lockNoticeObj.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0f, 135f);
            rt.sizeDelta = new Vector2(800f, 54f);

            Image bg = lockNoticeObj.AddComponent<Image>();
            bg.sprite = Utils.ProceduralSpriteGenerator.CreateRoundedRectSprite(
                new Color(0.12f, 0.08f, 0.06f, 0.95f),
                new Color(1f, 0.45f, 0.20f, 0.85f),
                2f, 20
            );
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = false;

            GameObject textObj = new GameObject("NoticeText");
            textObj.transform.SetParent(lockNoticeObj.transform, false);
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(16f, 0f); textRt.offsetMax = new Vector2(-16f, 0f);

            lockNoticeText = textObj.AddComponent<Text>();
            lockNoticeText.font = GetSafeFont();
            lockNoticeText.fontSize = 22;
            lockNoticeText.fontStyle = FontStyle.Bold;
            lockNoticeText.alignment = TextAnchor.MiddleCenter;
            lockNoticeText.color = new Color(1.0f, 0.85f, 0.35f);
            lockNoticeText.raycastTarget = false;

            lockNoticeObj.SetActive(false);
        }

        private void SetupCards()
        {
            if (characterCards == null || characterCards.Count == 0)
            {
                CreateDefaultCardsData();
            }

            float cardSpacing = 168f;
            float startX = -((characterCards.Count - 1) * cardSpacing) * 0.5f;

            for (int i = 0; i < characterCards.Count; i++)
            {
                var card = characterCards[i];

                if (card.selectButton == null)
                {
                    GameObject cardObj = new GameObject($"Card_{card.characterId}");
                    cardObj.transform.SetParent(transform, false);
                    RectTransform rt = cardObj.AddComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(startX + i * cardSpacing, 0f);
                    rt.sizeDelta = new Vector2(150f, 195f);

                    // Tıklanabilir zemin kartı (raycastTarget = true)
                    Image cardBg = cardObj.AddComponent<Image>();
                    cardBg.sprite = Utils.ProceduralSpriteGenerator.CreateRoundedRectSprite(new Color(1f, 1f, 1f, 0.04f), new Color(1f, 1f, 1f, 0.08f), 1f, 22);
                    cardBg.type = Image.Type.Sliced;
                    cardBg.raycastTarget = true;

                    Button btn = cardObj.AddComponent<Button>();
                    btn.targetGraphic = cardBg;
                    btn.transition = Selectable.Transition.ColorTint;
                    ColorBlock cb = btn.colors;
                    cb.normalColor = Color.white;
                    cb.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
                    cb.pressedColor = new Color(0.85f, 0.85f, 0.85f);
                    btn.colors = cb;
                    card.selectButton = btn;

                    // Swatch (Yuvarlatılmış Renk Kutusu - 115x115 Büyük Görsel)
                    GameObject swatchObj = new GameObject("Swatch");
                    swatchObj.transform.SetParent(cardObj.transform, false);
                    RectTransform swatchRt = swatchObj.AddComponent<RectTransform>();
                    swatchRt.anchoredPosition = new Vector2(0f, 28f);
                    swatchRt.sizeDelta = new Vector2(115f, 115f);
                    Image swatchImg = swatchObj.AddComponent<Image>();
                    swatchImg.type = Image.Type.Simple;
                    swatchImg.preserveAspect = true;
                    swatchImg.sprite = Utils.ProceduralSpriteGenerator.GetSpriteOrGenerate($"Sprites/{card.characterId}_hero", () => Utils.ProceduralSpriteGenerator.CreateMiniCharacterIcon(card.bodyColor, 128));
                    swatchImg.raycastTarget = false;
                    card.iconImage = swatchImg;

                    // Dış Vurgu Çerçevesi (Border)
                    GameObject borderObj = new GameObject("Border");
                    borderObj.transform.SetParent(swatchObj.transform, false);
                    RectTransform borderRt = borderObj.AddComponent<RectTransform>();
                    borderRt.anchorMin = Vector2.zero; borderRt.anchorMax = Vector2.one;
                    borderRt.offsetMin = new Vector2(-5f, -5f); borderRt.offsetMax = new Vector2(5f, 5f);
                    Image borderImg = borderObj.AddComponent<Image>();
                    borderImg.type = Image.Type.Sliced;
                    borderImg.sprite = Utils.ProceduralSpriteGenerator.CreateRoundedRectSprite(Color.clear, new Color(0.24f, 0.58f, 0.98f), 4.5f, 32);
                    borderImg.raycastTarget = false;
                    card.borderHighlight = borderImg;

                    // Kilit Katmanı (Lock Overlay - Swatch üzerine tam oturan cam buzlu panel)
                    GameObject lockObj = new GameObject("LockOverlay");
                    lockObj.transform.SetParent(swatchObj.transform, false);
                    RectTransform lockRt = lockObj.AddComponent<RectTransform>();
                    lockRt.anchorMin = Vector2.zero; lockRt.anchorMax = Vector2.one;
                    lockRt.offsetMin = Vector2.zero; lockRt.offsetMax = Vector2.zero;

                    Image lockBg = lockObj.AddComponent<Image>();
                    lockBg.sprite = Utils.ProceduralSpriteGenerator.CreateRoundedRectSprite(
                        new Color(0.04f, 0.06f, 0.10f, 0.84f),
                        new Color(1f, 1f, 1f, 0.14f),
                        1.5f, 24
                    );
                    lockBg.type = Image.Type.Sliced;
                    lockBg.raycastTarget = false;

                    // Asma Kilit İkonu (Padlock)
                    GameObject lockIconObj = new GameObject("LockIcon");
                    lockIconObj.transform.SetParent(lockObj.transform, false);
                    RectTransform lockIconRt = lockIconObj.AddComponent<RectTransform>();
                    lockIconRt.anchoredPosition = new Vector2(0f, 10f);
                    lockIconRt.sizeDelta = new Vector2(50f, 50f);
                    Image lockIconImg = lockIconObj.AddComponent<Image>();
                    lockIconImg.sprite = Utils.ProceduralSpriteGenerator.GetSpriteOrGenerate("Sprites/icon_lock", () => Utils.ProceduralSpriteGenerator.CreateLockIconSprite(128));
                    lockIconImg.preserveAspect = true;
                    lockIconImg.raycastTarget = false;

                    // Kilit Gereksinim Metni ("2500 Skor")
                    GameObject lockTextObj = new GameObject("LockText");
                    lockTextObj.transform.SetParent(lockObj.transform, false);
                    RectTransform lockTextRt = lockTextObj.AddComponent<RectTransform>();
                    lockTextRt.anchoredPosition = new Vector2(0f, -28f);
                    lockTextRt.sizeDelta = new Vector2(110f, 24f);
                    Text lockTxt = lockTextObj.AddComponent<Text>();
                    lockTxt.font = GetSafeFont();
                    lockTxt.text = "2500 Skor";
                    lockTxt.fontSize = 17;
                    lockTxt.fontStyle = FontStyle.Bold;
                    lockTxt.alignment = TextAnchor.MiddleCenter;
                    lockTxt.color = new Color(1f, 0.85f, 0.35f, 0.95f);
                    lockTxt.raycastTarget = false;

                    card.lockOverlay = lockObj;

                    // Karakter İsmi (Büyük ve Kalın Yazı)
                    GameObject nameObj = new GameObject("Name");
                    nameObj.transform.SetParent(cardObj.transform, false);
                    RectTransform nameRt = nameObj.AddComponent<RectTransform>();
                    nameRt.anchoredPosition = new Vector2(0f, -58f);
                    nameRt.sizeDelta = new Vector2(140f, 40f);
                    Text nameTxt = nameObj.AddComponent<Text>();
                    nameTxt.font = GetSafeFont();
                    nameTxt.text = card.characterName;
                    nameTxt.fontSize = 26;
                    nameTxt.fontStyle = FontStyle.Bold;
                    nameTxt.alignment = TextAnchor.MiddleCenter;
                    nameTxt.color = new Color(0.96f, 0.96f, 0.98f, 0.85f);
                    nameTxt.raycastTarget = false;
                    card.nameText = nameTxt;
                }

                string id = card.characterId;
                card.selectButton.onClick.RemoveAllListeners();
                card.selectButton.onClick.AddListener(() => OnCharacterSelected(id));
            }
        }

        public void OnCharacterSelected(string characterId)
        {
            // Eğer karakter kilitliyse seçilmesini engelle ve gereksinimi bildir
            if (!SaveSystem.IsCharacterUnlocked(characterId))
            {
                AudioManager.Instance?.PlayShieldHitSound();
                AudioManager.Instance?.TriggerHapticFeedback();
                string req = SaveSystem.GetUnlockRequirementText(characterId);
                ShowLockNotice(req);
                return;
            }

            currentSelectedId = characterId;
            SaveSystem.SaveSelectedCharacterId(characterId);
            RefreshSelectionVisuals();

            // Oyundaki karakter görselini güncelle
            if (Player.PlayerController.Instance != null)
            {
                Player.PlayerController.Instance.UpdateCharacterAppearance();
            }
            else
            {
                Player.PlayerController player = FindObjectOfType<Player.PlayerController>();
                if (player != null)
                {
                    player.UpdateCharacterAppearance();
                }
            }

            AudioManager.Instance?.PlayJumpSound();
            AudioManager.Instance?.TriggerHapticFeedback();

            // Dünya temasını anında güncelle
            WorldThemeManager.Instance?.SetCharacter(characterId);
        }

        private void ShowLockNotice(string requirementText)
        {
            if (lockNoticeObj == null) CreateLockNoticeBanner();
            if (lockNoticeObj == null || lockNoticeText == null) return;

            lockNoticeText.text = $"🔒 KİLİTLİ: Bu karakteri açmak için {requirementText}!";
            lockNoticeObj.SetActive(true);

            if (lockNoticeCoroutine != null) StopCoroutine(lockNoticeCoroutine);
            lockNoticeCoroutine = StartCoroutine(HideLockNoticeRoutine());
        }

        private IEnumerator HideLockNoticeRoutine()
        {
            yield return new WaitForSeconds(2.8f);
            if (lockNoticeObj != null)
            {
                lockNoticeObj.SetActive(false);
            }
            lockNoticeCoroutine = null;
        }

        private void RefreshSelectionVisuals()
        {
            foreach (var card in characterCards)
            {
                bool isUnlocked = SaveSystem.IsCharacterUnlocked(card.characterId);
                bool isSelected = isUnlocked && (card.characterId == currentSelectedId);

                if (card.lockOverlay != null)
                {
                    card.lockOverlay.SetActive(!isUnlocked);
                }

                if (card.iconImage != null)
                {
                    card.iconImage.color = isUnlocked ? Color.white : new Color(0.40f, 0.40f, 0.45f, 0.60f);
                }

                if (card.borderHighlight != null)
                {
                    card.borderHighlight.gameObject.SetActive(isSelected);
                }

                if (card.selectButton != null)
                {
                    Vector3 targetScale = isSelected ? new Vector3(1.10f, 1.10f, 1.0f) : Vector3.one;
                    card.selectButton.transform.localScale = targetScale;

                    Image bg = card.selectButton.GetComponent<Image>();
                    if (bg != null)
                    {
                        if (!isUnlocked)
                        {
                            bg.color = new Color(0f, 0f, 0f, 0.20f);
                        }
                        else
                        {
                            bg.color = isSelected ? new Color(0.14f, 0.38f, 0.92f, 0.25f) : new Color(1f, 1f, 1f, 0.04f);
                        }
                    }
                }

                if (card.nameText != null)
                {
                    if (!isUnlocked)
                    {
                        card.nameText.color = new Color(0.55f, 0.55f, 0.60f, 0.65f);
                    }
                    else
                    {
                        card.nameText.color = isSelected ? new Color(0.35f, 0.70f, 1.0f) : new Color(0.96f, 0.96f, 0.98f, 0.85f);
                    }
                }
            }
        }

        private void CreateDefaultCardsData()
        {
            characterCards = new List<CharacterCardUI>
            {
                new CharacterCardUI { characterId = "classic", characterName = "Klasik", bodyColor = new Color(1.0f, 0.82f, 0.40f) }, // #ffd166
                new CharacterCardUI { characterId = "fire", characterName = "Ateş", bodyColor = new Color(1.0f, 0.42f, 0.21f) },    // #ff6b35
                new CharacterCardUI { characterId = "ice", characterName = "Buz", bodyColor = new Color(0.30f, 0.79f, 0.94f) },     // #4cc9f0
                new CharacterCardUI { characterId = "toxic", characterName = "Zehir", bodyColor = new Color(0.49f, 0.95f, 0.61f) }, // #7cf29c
                new CharacterCardUI { characterId = "shadow", characterName = "Gece", bodyColor = new Color(0.61f, 0.36f, 0.90f) }  // #9b5de5
            };
        }
    }
}

