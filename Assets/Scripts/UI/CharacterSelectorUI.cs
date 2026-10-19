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
        public Color lockedColor;
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
            lockNoticeText.resizeTextForBestFit = true;
            lockNoticeText.resizeTextMinSize = 14;
            lockNoticeText.resizeTextMaxSize = 22;
            lockNoticeText.horizontalOverflow = HorizontalWrapMode.Wrap;
            lockNoticeText.verticalOverflow = VerticalWrapMode.Truncate;
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

            float cardSpacing = 172f;
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
                    rt.sizeDelta = new Vector2(150f, 210f);

                    // Tıklanabilir şeffaf zemin kartı (raycastTarget = true)
                    Image cardBg = cardObj.AddComponent<Image>();
                    cardBg.color = Color.clear;
                    cardBg.raycastTarget = true;

                    Button btn = cardObj.AddComponent<Button>();
                    btn.targetGraphic = cardBg;
                    btn.transition = Selectable.Transition.None;
                    card.selectButton = btn;

                    // Swatch (Yuvarlatılmış Squircle Avatar - 116x116)
                    GameObject swatchObj = new GameObject("Swatch");
                    swatchObj.transform.SetParent(cardObj.transform, false);
                    RectTransform swatchRt = swatchObj.AddComponent<RectTransform>();
                    swatchRt.anchoredPosition = new Vector2(0f, 32f);
                    swatchRt.sizeDelta = new Vector2(116f, 116f);
                    Image swatchImg = swatchObj.AddComponent<Image>();
                    swatchImg.type = Image.Type.Simple;
                    swatchImg.preserveAspect = true;
                    swatchImg.sprite = Utils.ProceduralSpriteGenerator.CreateSquircleAvatarSprite(card.bodyColor, 128, 34);
                    swatchImg.raycastTarget = false;
                    card.iconImage = swatchImg;

                    // Dış Vurgu Çerçevesi (Seçili Cyan/Mint Halka)
                    GameObject borderObj = new GameObject("Border");
                    borderObj.transform.SetParent(swatchObj.transform, false);
                    RectTransform borderRt = borderObj.AddComponent<RectTransform>();
                    borderRt.anchorMin = Vector2.zero; borderRt.anchorMax = Vector2.one;
                    borderRt.offsetMin = new Vector2(-9f, -9f); borderRt.offsetMax = new Vector2(9f, 9f);
                    Image borderImg = borderObj.AddComponent<Image>();
                    borderImg.type = Image.Type.Sliced;
                    borderImg.sprite = Utils.ProceduralSpriteGenerator.CreateRoundedRectSprite(Color.clear, new Color(0.21f, 0.96f, 0.82f), 4.5f, 38, 140, 140);
                    borderImg.raycastTarget = false;
                    card.borderHighlight = borderImg;

                    // Kilit Katmanı (Lock Container)
                    GameObject lockObj = new GameObject("LockOverlay");
                    lockObj.transform.SetParent(cardObj.transform, false);
                    RectTransform lockRt = lockObj.AddComponent<RectTransform>();
                    lockRt.anchorMin = Vector2.zero; lockRt.anchorMax = Vector2.one;
                    lockRt.offsetMin = Vector2.zero; lockRt.offsetMax = Vector2.zero;

                    // 1. Köşedeki Kilit Rozeti (Squircle'ın Sağ Alt Köşesi - 34x34 px)
                    GameObject cornerBadge = new GameObject("CornerLockBadge");
                    cornerBadge.transform.SetParent(swatchObj.transform, false);
                    RectTransform badgeRt = cornerBadge.AddComponent<RectTransform>();
                    badgeRt.anchorMin = new Vector2(1f, 0f);
                    badgeRt.anchorMax = new Vector2(1f, 0f);
                    badgeRt.pivot = new Vector2(1f, 0f);
                    badgeRt.anchoredPosition = new Vector2(4f, -4f);
                    badgeRt.sizeDelta = new Vector2(34f, 34f);

                    Image badgeBg = cornerBadge.AddComponent<Image>();
                    badgeBg.sprite = Utils.ProceduralSpriteGenerator.CreateCircleSprite(new Color(0.08f, 0.10f, 0.14f, 0.92f), 64);
                    badgeBg.type = Image.Type.Simple;
                    badgeBg.raycastTarget = false;

                    // Kilit İkonu (Koyu Gri/Metalik Kilit)
                    GameObject lockIconObj = new GameObject("LockIcon");
                    lockIconObj.transform.SetParent(cornerBadge.transform, false);
                    RectTransform lockIconRt = lockIconObj.AddComponent<RectTransform>();
                    lockIconRt.anchorMin = new Vector2(0.5f, 0.5f);
                    lockIconRt.anchorMax = new Vector2(0.5f, 0.5f);
                    lockIconRt.pivot = new Vector2(0.5f, 0.5f);
                    lockIconRt.anchoredPosition = Vector2.zero;
                    lockIconRt.sizeDelta = new Vector2(18f, 18f);
                    Image lockIconImg = lockIconObj.AddComponent<Image>();
                    lockIconImg.sprite = Utils.ProceduralSpriteGenerator.GetSpriteOrGenerate("Sprites/icon_lock", () => Utils.ProceduralSpriteGenerator.CreateLockIconSprite(64));
                    lockIconImg.color = new Color(0.35f, 0.42f, 0.50f);
                    lockIconImg.preserveAspect = true;
                    lockIconImg.raycastTarget = false;

                    // Köşe rozetini de lockObj hiyerarşisine bağla
                    cornerBadge.transform.SetParent(lockObj.transform, true);

                    // 2. "2500 Skor" Rozeti (İsmin Altında Konumlandırılmış Koyu Pill)
                    GameObject lockPillObj = new GameObject("LockPill");
                    lockPillObj.transform.SetParent(lockObj.transform, false);
                    RectTransform lockPillRt = lockPillObj.AddComponent<RectTransform>();
                    lockPillRt.anchoredPosition = new Vector2(0f, -88f);
                    lockPillRt.sizeDelta = new Vector2(106f, 26f);

                    Image pillBg = lockPillObj.AddComponent<Image>();
                    pillBg.sprite = Utils.ProceduralSpriteGenerator.CreateRoundedRectSprite(
                        new Color(0.09f, 0.12f, 0.19f, 0.95f),
                        Color.clear,
                        0f, 13, 64, 32
                    );
                    pillBg.type = Image.Type.Sliced;
                    pillBg.raycastTarget = false;

                    GameObject lockTextObj = new GameObject("LockText");
                    lockTextObj.transform.SetParent(lockPillObj.transform, false);
                    RectTransform lockTextRt = lockTextObj.AddComponent<RectTransform>();
                    lockTextRt.anchorMin = Vector2.zero;
                    lockTextRt.anchorMax = Vector2.one;
                    lockTextRt.offsetMin = new Vector2(4f, 0f);
                    lockTextRt.offsetMax = new Vector2(-4f, 0f);

                    Text lockTxt = lockTextObj.AddComponent<Text>();
                    lockTxt.font = GetSafeFont();
                    lockTxt.text = "2500 Skor";
                    lockTxt.fontSize = 15;
                    lockTxt.fontStyle = FontStyle.Bold;
                    lockTxt.alignment = TextAnchor.MiddleCenter;
                    lockTxt.resizeTextForBestFit = true;
                    lockTxt.resizeTextMinSize = 11;
                    lockTxt.resizeTextMaxSize = 15;
                    lockTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
                    lockTxt.verticalOverflow = VerticalWrapMode.Truncate;
                    lockTxt.color = new Color(0.90f, 0.72f, 0.23f); // #E5B83B
                    lockTxt.raycastTarget = false;

                    card.lockOverlay = lockObj;

                    // Karakter İsmi (Klasik, Ateş, Buz, Zehir, Gece)
                    GameObject nameObj = new GameObject("Name");
                    nameObj.transform.SetParent(cardObj.transform, false);
                    RectTransform nameRt = nameObj.AddComponent<RectTransform>();
                    nameRt.anchoredPosition = new Vector2(0f, -50f);
                    nameRt.sizeDelta = new Vector2(140f, 34f);
                    Text nameTxt = nameObj.AddComponent<Text>();
                    nameTxt.font = GetSafeFont();
                    nameTxt.text = card.characterName;
                    nameTxt.fontSize = 24;
                    nameTxt.fontStyle = FontStyle.Bold;
                    nameTxt.alignment = TextAnchor.MiddleCenter;
                    nameTxt.resizeTextForBestFit = true;
                    nameTxt.resizeTextMinSize = 16;
                    nameTxt.resizeTextMaxSize = 24;
                    nameTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
                    nameTxt.verticalOverflow = VerticalWrapMode.Truncate;
                    nameTxt.color = Color.white;
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
                    Color targetCol = isUnlocked ? card.bodyColor : card.lockedColor;
                    card.iconImage.sprite = Utils.ProceduralSpriteGenerator.CreateSquircleAvatarSprite(targetCol, 128, 34);
                    card.iconImage.color = Color.white;
                }

                if (card.borderHighlight != null)
                {
                    card.borderHighlight.gameObject.SetActive(isSelected);
                }

                if (card.nameText != null)
                {
                    if (!isUnlocked)
                    {
                        card.nameText.color = new Color(0.55f, 0.62f, 0.72f);
                    }
                    else
                    {
                        card.nameText.color = isSelected ? Color.white : new Color(0.92f, 0.94f, 0.98f);
                    }
                }
            }
        }

        private void CreateDefaultCardsData()
        {
            characterCards = new List<CharacterCardUI>
            {
                new CharacterCardUI {
                    characterId = "classic",
                    characterName = "Klasik",
                    bodyColor = new Color(1.0f, 0.87f, 0.42f), // #FFDE6A
                    lockedColor = new Color(1.0f, 0.87f, 0.42f)
                },
                new CharacterCardUI {
                    characterId = "fire",
                    characterName = "Ateş",
                    bodyColor = new Color(1.0f, 0.51f, 0.28f), // #FF8248
                    lockedColor = new Color(0.48f, 0.40f, 0.38f)
                },
                new CharacterCardUI {
                    characterId = "ice",
                    characterName = "Buz",
                    bodyColor = new Color(0.22f, 0.83f, 0.96f), // #38D4F5
                    lockedColor = new Color(0.37f, 0.46f, 0.52f) // #5F7585
                },
                new CharacterCardUI {
                    characterId = "toxic",
                    characterName = "Zehir",
                    bodyColor = new Color(0.24f, 0.88f, 0.49f), // #3CE07E
                    lockedColor = new Color(0.42f, 0.52f, 0.44f) // #6B8472
                },
                new CharacterCardUI {
                    characterId = "shadow",
                    characterName = "Gece",
                    bodyColor = new Color(0.65f, 0.40f, 0.96f), // #A665F5
                    lockedColor = new Color(0.37f, 0.33f, 0.43f) // #5E566E
                }
            };
        }
    }
}

