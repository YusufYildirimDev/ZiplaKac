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
    /// Dokunulduğunda oyunu başlatmaz, PlayerPrefs seçimini günceller ve vurgulu çerçeveyi yeniler.
    /// </summary>
    public class CharacterSelectorUI : MonoBehaviour
    {
        [Header("Karakter Galerisi Kartları")]
        public List<CharacterCardUI> characterCards = new List<CharacterCardUI>();

        private string currentSelectedId;

        private void Start()
        {
            currentSelectedId = SaveSystem.GetSelectedCharacterId("classic");
            SetupCards();
            RefreshSelectionVisuals();
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

        private void SetupCards()
        {
            if (characterCards == null || characterCards.Count == 0)
            {
                CreateDefaultCardsData();
            }

            float cardSpacing = 145f;
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
                    rt.sizeDelta = new Vector2(120f, 160f);

                    Button btn = cardObj.AddComponent<Button>();
                    btn.transition = Selectable.Transition.None;
                    card.selectButton = btn;

                    // Swatch (Yuvarlatılmış Renk Kutusu)
                    GameObject swatchObj = new GameObject("Swatch");
                    swatchObj.transform.SetParent(cardObj.transform, false);
                    RectTransform swatchRt = swatchObj.AddComponent<RectTransform>();
                    swatchRt.anchoredPosition = new Vector2(0f, 25f);
                    swatchRt.sizeDelta = new Vector2(85f, 85f);
                    Image swatchImg = swatchObj.AddComponent<Image>();
                    swatchImg.type = Image.Type.Simple;
                    swatchImg.preserveAspect = true;
                    swatchImg.sprite = Utils.ProceduralSpriteGenerator.CreateMiniCharacterIcon(card.bodyColor, 128);
                    swatchImg.raycastTarget = false;
                    card.iconImage = swatchImg;

                    // Dış Vurgu Çerçevesi (Border)
                    GameObject borderObj = new GameObject("Border");
                    borderObj.transform.SetParent(swatchObj.transform, false);
                    RectTransform borderRt = borderObj.AddComponent<RectTransform>();
                    borderRt.anchorMin = Vector2.zero; borderRt.anchorMax = Vector2.one;
                    borderRt.offsetMin = new Vector2(-4f, -4f); borderRt.offsetMax = new Vector2(4f, 4f);
                    Image borderImg = borderObj.AddComponent<Image>();
                    borderImg.type = Image.Type.Sliced;
                    borderImg.sprite = Utils.ProceduralSpriteGenerator.CreateRoundedRectSprite(Color.clear, new Color(0.02f, 0.84f, 0.63f), 4f, 26);
                    borderImg.raycastTarget = false;
                    card.borderHighlight = borderImg;

                    // Karakter İsmi
                    GameObject nameObj = new GameObject("Name");
                    nameObj.transform.SetParent(cardObj.transform, false);
                    RectTransform nameRt = nameObj.AddComponent<RectTransform>();
                    nameRt.anchoredPosition = new Vector2(0f, -45f);
                    nameRt.sizeDelta = new Vector2(110f, 32f);
                    Text nameTxt = nameObj.AddComponent<Text>();
                    nameTxt.font = GetSafeFont();
                    nameTxt.text = card.characterName;
                    nameTxt.fontSize = 22;
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
            currentSelectedId = characterId;
            SaveSystem.SaveSelectedCharacterId(characterId);
            RefreshSelectionVisuals();

            // Oyundaki karakter görselini güncelle
            Player.PlayerController player = FindObjectOfType<Player.PlayerController>();
            if (player != null)
            {
                player.UpdateCharacterAppearance();
            }

            AudioManager.Instance?.PlayJumpSound();
        }

        private void RefreshSelectionVisuals()
        {
            foreach (var card in characterCards)
            {
                bool isSelected = card.characterId == currentSelectedId;

                if (card.borderHighlight != null)
                {
                    card.borderHighlight.gameObject.SetActive(isSelected);
                }

                if (card.selectButton != null)
                {
                    Vector3 targetScale = isSelected ? new Vector3(1.10f, 1.10f, 1.0f) : Vector3.one;
                    card.selectButton.transform.localScale = targetScale;
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
