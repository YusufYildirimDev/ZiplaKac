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

            float startX = -((characterCards.Count - 1) * 140f) * 0.5f;

            for (int i = 0; i < characterCards.Count; i++)
            {
                var card = characterCards[i];

                if (card.selectButton == null)
                {
                    GameObject cardObj = new GameObject($"Card_{card.characterId}");
                    cardObj.transform.SetParent(transform, false);
                    RectTransform rt = cardObj.AddComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(startX + i * 140f, 0f);
                    rt.sizeDelta = new Vector2(110f, 150f);

                    Image cardBg = cardObj.AddComponent<Image>();
                    cardBg.color = new Color(0.15f, 0.15f, 0.2f, 0.85f);
                    card.borderHighlight = cardBg;

                    Button btn = cardObj.AddComponent<Button>();
                    card.selectButton = btn;

                    GameObject iconObj = new GameObject("Icon");
                    iconObj.transform.SetParent(cardObj.transform, false);
                    RectTransform iconRt = iconObj.AddComponent<RectTransform>();
                    iconRt.anchoredPosition = new Vector2(0f, 20f);
                    iconRt.sizeDelta = new Vector2(60f, 60f);
                    Image iconImg = iconObj.AddComponent<Image>();
                    iconImg.sprite = Utils.ProceduralSpriteGenerator.CreateCircleSprite(card.bodyColor, 64);
                    iconImg.raycastTarget = false;
                    card.iconImage = iconImg;

                    GameObject nameObj = new GameObject("Name");
                    nameObj.transform.SetParent(cardObj.transform, false);
                    RectTransform nameRt = nameObj.AddComponent<RectTransform>();
                    nameRt.anchoredPosition = new Vector2(0f, -40f);
                    nameRt.sizeDelta = new Vector2(100f, 30f);
                    Text nameTxt = nameObj.AddComponent<Text>();
                    nameTxt.font = GetSafeFont();
                    nameTxt.text = card.characterName;
                    nameTxt.fontSize = 20;
                    nameTxt.alignment = TextAnchor.MiddleCenter;
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
                    card.borderHighlight.enabled = isSelected;
                    card.borderHighlight.color = isSelected ? Color.white : new Color(1f, 1f, 1f, 0.2f);
                }

                if (card.selectButton != null)
                {
                    // Seçili kart için hafif büyüme ölçek animasyonu
                    Vector3 targetScale = isSelected ? new Vector3(1.1f, 1.1f, 1.0f) : Vector3.one;
                    card.selectButton.transform.localScale = targetScale;
                }
            }
        }

        private void CreateDefaultCardsData()
        {
            characterCards = new List<CharacterCardUI>
            {
                new CharacterCardUI { characterId = "classic", characterName = "Klasik", bodyColor = new Color(1f, 0.85f, 0.1f) },
                new CharacterCardUI { characterId = "fire", characterName = "Ateş", bodyColor = new Color(1f, 0.4f, 0f) },
                new CharacterCardUI { characterId = "ice", characterName = "Buz", bodyColor = new Color(0.2f, 0.7f, 1f) },
                new CharacterCardUI { characterId = "poison", characterName = "Zehir", bodyColor = new Color(0.2f, 0.9f, 0.3f) },
                new CharacterCardUI { characterId = "night", characterName = "Gece", bodyColor = new Color(0.6f, 0.2f, 0.9f) }
            };
        }
    }
}
