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
        public Image namePillBg;
        public GameObject lockOverlay;
        public Text lockText;
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
            LocalizationManager.OnLanguageChanged += RefreshSelectionVisuals;
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

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= RefreshSelectionVisuals;
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
            lockNoticeText.resizeTextForBestFit = true;
            lockNoticeText.resizeTextMinSize = 14;
            lockNoticeText.resizeTextMaxSize = 22;
            lockNoticeText.horizontalOverflow = HorizontalWrapMode.Wrap;
            lockNoticeText.verticalOverflow = VerticalWrapMode.Truncate;
            lockNoticeText.color = new Color(1.0f, 0.85f, 0.35f);
            lockNoticeText.raycastTarget = false;

            lockNoticeObj.SetActive(false);
        }

        private static Sprite LoadUISprite(string name)
        {
            Sprite sp = Resources.Load<Sprite>("Sprites/UI_MainMenu/" + name);
            if (sp == null)
            {
                Texture2D tex = Resources.Load<Texture2D>("Sprites/UI_MainMenu/" + name);
                if (tex != null)
                {
                    sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }
            return sp;
        }

        private static Sprite GetCardAvatarSprite(string characterId)
        {
            switch (characterId)
            {
                case "classic": return LoadUISprite("card_klasik");
                case "fire": return LoadUISprite("card_ates");
                case "ice": return LoadUISprite("card_buz");
                case "toxic": return LoadUISprite("card_zehir");
                case "shadow": return LoadUISprite("card_gece");
                default: return null;
            }
        }

        private void SetupCards()
        {
            if (characterCards == null || characterCards.Count == 0)
            {
                CreateDefaultCardsData();
            }

            float cardSpacing = 152f;
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
                    rt.sizeDelta = new Vector2(136f, 185f);

                    // Tıklanabilir zemin kartı (raycastTarget = true)
                    Image cardBg = cardObj.AddComponent<Image>();
                    cardBg.color = Color.clear;
                    cardBg.raycastTarget = true;

                    Button btn = cardObj.AddComponent<Button>();
                    btn.targetGraphic = cardBg;
                    btn.transition = Selectable.Transition.ColorTint;
                    ColorBlock cb = btn.colors;
                    cb.normalColor = Color.white;
                    cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
                    cb.pressedColor = new Color(0.85f, 0.85f, 0.85f);
                    btn.colors = cb;
                    card.selectButton = btn;

                    // Swatch Konteyneri (126x126 Koyu Navy Squircle Zemin)
                    GameObject swatchObj = new GameObject("Swatch");
                    swatchObj.transform.SetParent(cardObj.transform, false);
                    RectTransform swatchRt = swatchObj.AddComponent<RectTransform>();
                    swatchRt.anchoredPosition = new Vector2(0f, 26f);
                    swatchRt.sizeDelta = new Vector2(126f, 126f);

                    Image swatchBg = swatchObj.AddComponent<Image>();
                    swatchBg.sprite = Utils.ProceduralSpriteGenerator.CreateRoundedRectSprite(
                        new Color(0.09f, 0.13f, 0.22f, 0.95f),
                        new Color(1f, 1f, 1f, 0.06f),
                        1f, 24, 128, 128
                    );
                    swatchBg.type = Image.Type.Sliced;
                    swatchBg.raycastTarget = false;

                    // Karakter Avatar İkonu (Mockup Kart Portresi)
                    GameObject avatarObj = new GameObject("Avatar");
                    avatarObj.transform.SetParent(swatchObj.transform, false);
                    RectTransform avatarRt = avatarObj.AddComponent<RectTransform>();
                    avatarRt.anchoredPosition = Vector2.zero;
                    avatarRt.sizeDelta = new Vector2(122f, 122f);
                    Image avatarImg = avatarObj.AddComponent<Image>();
                    avatarImg.type = Image.Type.Simple;
                    avatarImg.preserveAspect = true;

                    Sprite cardSprite = GetCardAvatarSprite(card.characterId);
                    if (cardSprite != null)
                    {
                        avatarImg.sprite = cardSprite;
                    }
                    else
                    {
                        avatarImg.sprite = Utils.ProceduralSpriteGenerator.GetSpriteOrGenerate($"Sprites/{card.characterId}_hero", () => Utils.ProceduralSpriteGenerator.CreateMiniCharacterIcon(card.bodyColor, 128));
                    }
                    avatarImg.raycastTarget = false;
                    card.iconImage = avatarImg;

                    // Dış Vurgu Çerçevesi (Border - Mockup Cyan Glow Çerçeve)
                    GameObject borderObj = new GameObject("Border");
                    borderObj.transform.SetParent(swatchObj.transform, false);
                    RectTransform borderRt = borderObj.AddComponent<RectTransform>();
                    borderRt.anchorMin = Vector2.zero; borderRt.anchorMax = Vector2.one;
                    borderRt.offsetMin = new Vector2(-4f, -4f); borderRt.offsetMax = new Vector2(4f, 4f);
                    Image borderImg = borderObj.AddComponent<Image>();
                    Sprite borderSp = LoadUISprite("border_selected_cyan");
                    if (borderSp != null)
                    {
                        borderImg.sprite = borderSp;
                        borderImg.type = Image.Type.Simple;
                        borderImg.preserveAspect = true;
                    }
                    else
                    {
                        borderImg.type = Image.Type.Sliced;
                        borderImg.sprite = Utils.ProceduralSpriteGenerator.CreateRoundedRectSprite(Color.clear, new Color(0.0f, 0.76f, 1.0f), 4.0f, 26);
                    }
                    borderImg.raycastTarget = false;
                    card.borderHighlight = borderImg;

                    // Kilit Katmanı (Lock Overlay)
                    GameObject lockObj = new GameObject("LockOverlay");
                    lockObj.transform.SetParent(swatchObj.transform, false);
                    RectTransform lockRt = lockObj.AddComponent<RectTransform>();
                    lockRt.anchorMin = Vector2.zero; lockRt.anchorMax = Vector2.one;
                    lockRt.offsetMin = Vector2.zero; lockRt.offsetMax = Vector2.zero;

                    Image lockBg = lockObj.AddComponent<Image>();
                    lockBg.color = new Color(0.02f, 0.04f, 0.08f, 0.35f);
                    lockBg.raycastTarget = false;

                    // 1. Köşedeki Kilit Rozeti (Mockup Sağ Üst Köşe - 36x36 px)
                    GameObject cornerBadge = new GameObject("CornerLockBadge");
                    cornerBadge.transform.SetParent(lockObj.transform, false);
                    RectTransform badgeRt = cornerBadge.AddComponent<RectTransform>();
                    badgeRt.anchorMin = new Vector2(1f, 1f);
                    badgeRt.anchorMax = new Vector2(1f, 1f);
                    badgeRt.pivot = new Vector2(1f, 1f);
                    badgeRt.anchoredPosition = new Vector2(-2f, -2f);
                    badgeRt.sizeDelta = new Vector2(36f, 36f);

                    Image badgeBg = cornerBadge.AddComponent<Image>();
                    Sprite lockBadgeSp = LoadUISprite("lock_badge");
                    if (lockBadgeSp != null)
                    {
                        badgeBg.sprite = lockBadgeSp;
                        badgeBg.preserveAspect = true;
                    }
                    else
                    {
                        badgeBg.sprite = Utils.ProceduralSpriteGenerator.CreateRoundedRectSprite(
                            new Color(0.06f, 0.08f, 0.16f, 0.95f),
                            new Color(1.0f, 0.82f, 0.35f, 0.95f),
                            1.5f, 17, 64, 64
                        );
                        badgeBg.type = Image.Type.Sliced;

                        GameObject lockIconObj = new GameObject("LockIcon");
                        lockIconObj.transform.SetParent(cornerBadge.transform, false);
                        RectTransform lockIconRt = lockIconObj.AddComponent<RectTransform>();
                        lockIconRt.anchorMin = new Vector2(0.5f, 0.5f);
                        lockIconRt.anchorMax = new Vector2(0.5f, 0.5f);
                        lockIconRt.pivot = new Vector2(0.5f, 0.5f);
                        lockIconRt.anchoredPosition = Vector2.zero;
                        lockIconRt.sizeDelta = new Vector2(20f, 20f);
                        Image lockIconImg = lockIconObj.AddComponent<Image>();
                        lockIconImg.sprite = Utils.ProceduralSpriteGenerator.GetSpriteOrGenerate("Sprites/icon_lock", () => Utils.ProceduralSpriteGenerator.CreateLockIconSprite(128));
                        lockIconImg.color = new Color(1.0f, 0.85f, 0.35f);
                        lockIconImg.preserveAspect = true;
                        lockIconImg.raycastTarget = false;
                    }
                    badgeBg.raycastTarget = false;

                    // 2. Kilit Gereksinim Şeridi (Mockup Alt Kenarda "2500 Skor" Şeridi)
                    GameObject lockPillObj = new GameObject("LockPill");
                    lockPillObj.transform.SetParent(lockObj.transform, false);
                    RectTransform lockPillRt = lockPillObj.AddComponent<RectTransform>();
                    lockPillRt.anchorMin = new Vector2(0.5f, 0f);
                    lockPillRt.anchorMax = new Vector2(0.5f, 0f);
                    lockPillRt.pivot = new Vector2(0.5f, 0f);
                    lockPillRt.anchoredPosition = new Vector2(0f, 4f);
                    lockPillRt.sizeDelta = new Vector2(96f, 28f);

                    Image pillBg = lockPillObj.AddComponent<Image>();
                    Sprite lockPillSp = LoadUISprite("lock_pill_2500");
                    if (lockPillSp != null)
                    {
                        pillBg.sprite = lockPillSp;
                        pillBg.preserveAspect = true;
                    }
                    else
                    {
                        pillBg.sprite = Utils.ProceduralSpriteGenerator.CreateRoundedRectSprite(
                            new Color(0.05f, 0.07f, 0.14f, 0.90f),
                            new Color(1f, 1f, 1f, 0.15f),
                            1f, 10, 64, 32
                        );
                        pillBg.type = Image.Type.Sliced;

                        GameObject lockTextObj = new GameObject("LockText");
                        lockTextObj.transform.SetParent(lockPillObj.transform, false);
                        RectTransform lockTextRt = lockTextObj.AddComponent<RectTransform>();
                        lockTextRt.anchorMin = Vector2.zero;
                        lockTextRt.anchorMax = Vector2.one;
                        lockTextRt.offsetMin = new Vector2(2f, 0f);
                        lockTextRt.offsetMax = new Vector2(-2f, 0f);

                        Text lockTxt = lockTextObj.AddComponent<Text>();
                        lockTxt.font = GetSafeFont();
                        lockTxt.text = LocalizationManager.GetFormat("char_lock_badge", 2500);
                        lockTxt.fontSize = 15;
                        lockTxt.fontStyle = FontStyle.Bold;
                        lockTxt.alignment = TextAnchor.MiddleCenter;
                        lockTxt.resizeTextForBestFit = true;
                        lockTxt.resizeTextMinSize = 10;
                        lockTxt.resizeTextMaxSize = 15;
                        lockTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
                        lockTxt.verticalOverflow = VerticalWrapMode.Truncate;
                        lockTxt.color = new Color(1f, 0.85f, 0.35f, 0.95f);
                        lockTxt.raycastTarget = false;
                        card.lockText = lockTxt;
                    }
                    pillBg.raycastTarget = false;

                    card.lockOverlay = lockObj;

                    // Karakter İsmi ve Seçili Pill Rozeti (HD 104x34 px)
                    GameObject nameObj = new GameObject("NamePill");
                    nameObj.transform.SetParent(cardObj.transform, false);
                    RectTransform nameRt = nameObj.AddComponent<RectTransform>();
                    nameRt.anchoredPosition = new Vector2(0f, -56f);
                    nameRt.sizeDelta = new Vector2(104f, 34f);

                    Image namePillBg = nameObj.AddComponent<Image>();
                    Sprite badgeSp = LoadUISprite("badge_selected_name");
                    if (badgeSp != null)
                    {
                        namePillBg.sprite = badgeSp;
                        namePillBg.type = Image.Type.Simple;
                        namePillBg.preserveAspect = true;
                    }
                    else
                    {
                        namePillBg.sprite = Utils.ProceduralSpriteGenerator.CreateRoundedRectSprite(
                            new Color(0.0f, 0.62f, 0.98f),
                            Color.clear,
                            0f, 16, 64, 32
                        );
                        namePillBg.type = Image.Type.Sliced;
                    }
                    namePillBg.raycastTarget = false;
                    namePillBg.enabled = false;
                    card.namePillBg = namePillBg;

                    GameObject nameTextObj = new GameObject("NameText");
                    nameTextObj.transform.SetParent(nameObj.transform, false);
                    RectTransform nameTextRt = nameTextObj.AddComponent<RectTransform>();
                    nameTextRt.anchorMin = Vector2.zero;
                    nameTextRt.anchorMax = Vector2.one;
                    nameTextRt.offsetMin = Vector2.zero;
                    nameTextRt.offsetMax = Vector2.zero;

                    Text nameTxt = nameTextObj.AddComponent<Text>();
                    nameTxt.font = GetSafeFont();
                    nameTxt.text = card.characterName;
                    nameTxt.fontSize = 20;
                    nameTxt.fontStyle = FontStyle.Bold;
                    nameTxt.alignment = TextAnchor.MiddleCenter;
                    nameTxt.resizeTextForBestFit = true;
                    nameTxt.resizeTextMinSize = 14;
                    nameTxt.resizeTextMaxSize = 20;
                    nameTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
                    nameTxt.verticalOverflow = VerticalWrapMode.Truncate;
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
                string prev = SaveSystem.GetPreviousCharacterInChain(characterId);
                string prevName = LocalizationManager.Get($"char_{prev}_name", SaveSystem.GetCharacterDisplayName(prev));
                string msg = LocalizationManager.GetFormat("char_lock_msg", prevName);
                ShowLockNotice(msg);
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

        private void ShowLockNotice(string message)
        {
            if (lockNoticeObj == null) CreateLockNoticeBanner();
            if (lockNoticeObj == null || lockNoticeText == null) return;

            lockNoticeText.text = message;
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

                if (card.lockText != null)
                {
                    card.lockText.text = LocalizationManager.GetFormat("char_lock_badge", 2500);
                }

                if (card.iconImage != null)
                {
                    card.iconImage.color = isUnlocked ? Color.white : new Color(0.65f, 0.65f, 0.70f, 0.88f);
                }

                if (card.borderHighlight != null)
                {
                    card.borderHighlight.gameObject.SetActive(isSelected);
                }

                if (card.namePillBg != null)
                {
                    card.namePillBg.enabled = isSelected;
                }

                if (card.nameText != null)
                {
                    card.nameText.text = LocalizationManager.Get($"char_{card.characterId}_name", card.characterName);
                    if (isSelected)
                    {
                        card.nameText.color = Color.white;
                    }
                    else if (isUnlocked)
                    {
                        card.nameText.color = new Color(0.615f, 0.737f, 0.878f); // #9DBCE0 Secondary text
                    }
                    else
                    {
                        card.nameText.color = new Color(0.45f, 0.55f, 0.70f);
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

