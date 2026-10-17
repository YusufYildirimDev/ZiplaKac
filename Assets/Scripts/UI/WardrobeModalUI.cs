using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZiplaKac.Core;
using ZiplaKac.Player;
using ZiplaKac.Utils;

namespace ZiplaKac.UI
{
    public enum ShopTab
    {
        Upgrades,
        Outfits
    }

    [System.Serializable]
    public class WardrobeItemConfig
    {
        public string outfitType;
        public string displayName;
        public int price;
    }

    [System.Serializable]
    public class ConsumableItemConfig
    {
        public string id;
        public string displayName;
        public string description;
        public int price;
    }

    [System.Serializable]
    public class UpgradeItemConfig
    {
        public string id;
        public string displayName;
        public string description;
        public int price;
        public int maxLevel;
    }

    /// <summary>
    /// Zıpla Kaç Mağaza & Gardırop Modalı.
    /// 1. Sekme: Güçler & Yükseltmeler (Tek Kullanımlık Consumables + Kalıcı Karakter Yükseltmeleri).
    /// 2. Sekme: Gardırop (Kıyafet Satın Alma & Giyme/Çıkarma).
    /// Kaydırılabilir liste (ScrollRect) ve taşma yapmayan dinamik kart yuvaları barındırır.
    /// </summary>
    public class WardrobeModalUI : MonoBehaviour
    {
        public static WardrobeModalUI Instance { get; private set; }

        [Header("Başlık ve Altın Göstergesi")]
        public Text titleText;
        public Text totalGoldText;
        public Text statusMessageText;
        public Button closeButton;

        [Header("Sekme Butonları")]
        public Button tabUpgradesButton;
        public Button tabOutfitsButton;
        public Image tabUpgradesBg;
        public Image tabOutfitsBg;
        public Text tabUpgradesText;
        public Text tabOutfitsText;

        [Header("Eşya Yuvaları ve Kaydırma")]
        public Transform itemsContainer;
        public ScrollRect itemsScrollRect;

        public ShopTab currentTab = ShopTab.Upgrades;

        private readonly List<WardrobeItemConfig> outfitConfigs = new List<WardrobeItemConfig>
        {
            new WardrobeItemConfig { outfitType = "pelerin",        displayName = "Pelerin",         price = 80 },
            new WardrobeItemConfig { outfitType = "gunes_gozlugu",  displayName = "Güneş Gözlüğü",   price = 80 },
            new WardrobeItemConfig { outfitType = "parti_sapkasi",  displayName = "Parti Şapkası",   price = 80 },
            new WardrobeItemConfig { outfitType = "ozel_aksesuar",  displayName = "Özel Aksesuar",   price = 120 }
        };

        private readonly List<ConsumableItemConfig> consumableConfigs = new List<ConsumableItemConfig>
        {
            new ConsumableItemConfig { id = "shield",        displayName = "Başlangıç Kalkanı",    description = "Oyuna 1 kalkanla başla", price = 30 },
            new ConsumableItemConfig { id = "slow_start",    displayName = "Yavaş Başlangıç",      description = "İlk 5sn engel hızı %30 yavaş", price = 20 },
            new ConsumableItemConfig { id = "lucky_pu",      displayName = "Şanslı Güçlendirme",   description = "İlk küre garantili 3. saniyede", price = 15 },
            new ConsumableItemConfig { id = "second_chance", displayName = "İkinci Şans Bileti",   description = "Çarpışınca 1 kez ücretsiz devam et", price = 40 }
        };

        private readonly List<UpgradeItemConfig> upgradeConfigs = new List<UpgradeItemConfig>
        {
            new UpgradeItemConfig { id = "pu_duration",   displayName = "Güçlendirme Süresi",   description = "Kalkan/yavaş/x2 süreleri +%20 uzar", price = 150, maxLevel = 3 },
            new UpgradeItemConfig { id = "double_jump",   displayName = "Çift Zıplama Ustası",  description = "2. zıplama yüksekliği +%15 artar",  price = 200, maxLevel = 1 },
            new UpgradeItemConfig { id = "combo_memory",  displayName = "Kombo Hafızası",       description = "Çarpışmada kombonun yarısı korunur", price = 250, maxLevel = 1 },
            new UpgradeItemConfig { id = "signature",     displayName = "İmza Yeteneği +1",     description = "Karakterin özel yeteneği güçlenir",  price = 300, maxLevel = 1 }
        };

        private readonly List<GameObject> spawnedRows = new List<GameObject>();

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            // Ana Menü metinlerinin arkadan gözükmemesi için ana menü panelini geçici gizle
            if (UIManager.Instance != null && UIManager.Instance.mainMenuPanel != null && GameManager.Instance != null && GameManager.Instance.State == GameState.MainMenu)
            {
                UIManager.Instance.mainMenuPanel.SetActive(false);
            }

            RefreshUI();
            if (itemsScrollRect != null) itemsScrollRect.verticalNormalizedPosition = 1f;
        }

        private void OnDisable()
        {
            if (UIManager.Instance != null && UIManager.Instance.mainMenuPanel != null && GameManager.Instance != null && GameManager.Instance.State == GameState.MainMenu)
            {
                UIManager.Instance.mainMenuPanel.SetActive(true);
            }
        }

        public void SwitchTab(ShopTab tab)
        {
            currentTab = tab;
            AudioManager.Instance?.PlayJumpSound();
            RefreshUI();
            if (itemsScrollRect != null) itemsScrollRect.verticalNormalizedPosition = 1f;
        }

        public void RefreshUI()
        {
            int gold = SaveSystem.GetTotalGold();
            if (totalGoldText != null) totalGoldText.text = $"🪙 Toplam Altın: {gold}";
            if (statusMessageText != null) statusMessageText.text = "";

            UpdateTabButtonStyles();

            string charKey = "klasik";
            if (WorldThemeManager.Instance != null)
            {
                charKey = WorldThemeManager.Instance.CurrentCharacterKey;
            }

            if (currentTab == ShopTab.Upgrades)
            {
                if (titleText != null) titleText.text = "⚡ GÜÇLER & YÜKSELTMELER";
                BuildUpgradesTab(charKey);
            }
            else
            {
                if (titleText != null) titleText.text = "👔 GARDIROP (KIYAFETLER)";
                BuildOutfitsTab(charKey);
            }
        }

        private void UpdateTabButtonStyles()
        {
            Color activeColor = new Color(0.61f, 0.36f, 0.90f);
            Color inactiveColor = new Color(0.12f, 0.15f, 0.28f, 0.9f);

            if (tabUpgradesBg != null) tabUpgradesBg.color = (currentTab == ShopTab.Upgrades) ? activeColor : inactiveColor;
            if (tabOutfitsBg != null) tabOutfitsBg.color = (currentTab == ShopTab.Outfits) ? activeColor : inactiveColor;

            if (tabUpgradesText != null) tabUpgradesText.color = (currentTab == ShopTab.Upgrades) ? Color.white : new Color(0.7f, 0.8f, 0.9f);
            if (tabOutfitsText != null) tabOutfitsText.color = (currentTab == ShopTab.Outfits) ? Color.white : new Color(0.7f, 0.8f, 0.9f);
        }

        private void ClearSpawnedRows()
        {
            for (int i = 0; i < spawnedRows.Count; i++)
            {
                if (spawnedRows[i] != null) Destroy(spawnedRows[i]);
            }
            spawnedRows.Clear();
        }

        private void BuildUpgradesTab(string charKey)
        {
            if (itemsContainer == null) return;
            ClearSpawnedRows();

            Font font = GetSafeFont();
            float rowHeight = 132f;
            float spacing = 16f;
            float topPadding = 16f;
            int totalItems = consumableConfigs.Count + upgradeConfigs.Count;
            float totalHeight = topPadding + (totalItems * (rowHeight + spacing));

            RectTransform containerRt = itemsContainer.GetComponent<RectTransform>();
            if (containerRt != null)
            {
                containerRt.sizeDelta = new Vector2(0f, totalHeight);
                containerRt.anchoredPosition = Vector2.zero;
            }

            int rowIndex = 0;

            // 1. Tek Kullanımlıklar (Consumables)
            for (int i = 0; i < consumableConfigs.Count; i++)
            {
                var cfg = consumableConfigs[i];
                float yPos = -topPadding - (rowIndex * (rowHeight + spacing)) - (rowHeight / 2f);
                rowIndex++;

                int count = SaveSystem.GetConsumableCount(cfg.id);
                string subtitle = $"{cfg.description} · <color=#06d6a0>Envanter: {count}</color>";

                string emoji = "⚡";
                if (cfg.id == "shield") emoji = "🛡️";
                else if (cfg.id == "slow_start") emoji = "🐢";
                else if (cfg.id == "lucky_pu") emoji = "🍀";
                else if (cfg.id == "second_chance") emoji = "🎟️";

                GameObject row = CreateRowObject($"Consumable_{cfg.id}", yPos, rowHeight, cfg.displayName, subtitle, cfg.price, "AL", font, () => {
                    if (SaveSystem.PurchaseConsumable(cfg.id, cfg.price))
                    {
                        AudioManager.Instance?.PlayNewRecordSound();
                        AudioManager.Instance?.TriggerHapticFeedback();
                        ShowStatus($"🎉 {cfg.displayName} satın alındı! (+1 Envanter)", new Color(0.024f, 0.839f, 0.627f));
                        RefreshUI();
                    }
                    else
                    {
                        AudioManager.Instance?.PlayGameOverSound();
                        ShowStatus("⚠️ Yetersiz altın! Koşulardan altın topla.", new Color(0.95f, 0.35f, 0.40f));
                    }
                }, null, null, null, emoji, cfg.id);
                spawnedRows.Add(row);
            }

            // 2. Kalıcı Yükseltmeler (Permanent Upgrades)
            for (int i = 0; i < upgradeConfigs.Count; i++)
            {
                var cfg = upgradeConfigs[i];
                float yPos = -topPadding - (rowIndex * (rowHeight + spacing)) - (rowHeight / 2f);
                rowIndex++;

                int currentLvl = SaveSystem.GetUpgradeLevel(charKey, cfg.id);
                bool isMax = currentLvl >= cfg.maxLevel;

                string subtitle = cfg.maxLevel > 1
                    ? $"{cfg.description} · <color=#ffd166>Seviye {currentLvl}/{cfg.maxLevel}</color>"
                    : (isMax ? $"{cfg.description} · <color=#06d6a0>Açık</color>" : cfg.description);

                string btnLabel = isMax ? "MAX" : (cfg.maxLevel > 1 ? "YÜKSELT" : "AÇ");
                Color btnColor = isMax ? new Color(0.3f, 0.35f, 0.45f) : new Color(0.024f, 0.839f, 0.627f);

                string emoji = "⭐";
                if (cfg.id == "pu_duration") emoji = "⏳";
                else if (cfg.id == "double_jump") emoji = "🦘";
                else if (cfg.id == "combo_memory") emoji = "🧠";
                else if (cfg.id == "signature") emoji = "⚡";

                GameObject row = CreateRowObject($"Upgrade_{cfg.id}", yPos, rowHeight, cfg.displayName, subtitle, isMax ? 0 : cfg.price, btnLabel, font, () => {
                    if (isMax) return;
                    if (SaveSystem.PurchaseUpgrade(charKey, cfg.id, cfg.price, cfg.maxLevel))
                    {
                        AudioManager.Instance?.PlayNewRecordSound();
                        AudioManager.Instance?.TriggerHapticFeedback();
                        ShowStatus($"🎉 {cfg.displayName} seviye atladı!", new Color(0.024f, 0.839f, 0.627f));
                        RefreshUI();
                    }
                    else
                    {
                        AudioManager.Instance?.PlayGameOverSound();
                        ShowStatus("⚠️ Yetersiz altın! Koşulardan altın topla.", new Color(0.95f, 0.35f, 0.40f));
                    }
                }, btnColor, null, null, emoji, cfg.id);
                spawnedRows.Add(row);
            }
        }

        private void BuildOutfitsTab(string charKey)
        {
            if (itemsContainer == null) return;
            ClearSpawnedRows();

            Font font = GetSafeFont();
            float rowHeight = 146f;
            float spacing = 18f;
            float topPadding = 18f;
            int totalItems = outfitConfigs.Count;
            float totalHeight = topPadding + (totalItems * (rowHeight + spacing));

            RectTransform containerRt = itemsContainer.GetComponent<RectTransform>();
            if (containerRt != null)
            {
                containerRt.sizeDelta = new Vector2(0f, totalHeight);
                containerRt.anchoredPosition = Vector2.zero;
            }

            for (int i = 0; i < outfitConfigs.Count; i++)
            {
                var cfg = outfitConfigs[i];
                float yPos = -topPadding - (i * (rowHeight + spacing)) - (rowHeight / 2f);

                bool isPurchased = SaveSystem.IsOutfitPurchased(charKey, cfg.outfitType);
                bool isEquipped = SaveSystem.IsOutfitEquipped(charKey, cfg.outfitType);

                string subtitle = isPurchased ? "<color=#06d6a0>Sahipsin</color>" : $"{cfg.price} 🪙";
                string btnLabel = !isPurchased ? "SATIN AL" : (isEquipped ? "ÇIKAR" : "GİY");
                Color btnColor = !isPurchased ? new Color(1.0f, 0.82f, 0.40f) : (isEquipped ? new Color(0.85f, 0.22f, 0.30f) : new Color(0.024f, 0.839f, 0.627f));

                string currentType = cfg.outfitType;
                int cost = cfg.price;

                GameObject row = CreateRowObject($"Outfit_{cfg.outfitType}", yPos, rowHeight, cfg.displayName, subtitle, isPurchased ? 0 : cfg.price, btnLabel, font, () => {
                    if (!isPurchased)
                    {
                        if (SaveSystem.PurchaseOutfit(charKey, currentType, cost))
                        {
                            AudioManager.Instance?.PlayNewRecordSound();
                            AudioManager.Instance?.TriggerHapticFeedback();
                            ShowStatus("🎉 Satın alındı ve giyildi!", new Color(0.024f, 0.839f, 0.627f));
                            PlayerController.Instance?.UpdateCharacterAppearance();
                            RefreshUI();
                        }
                        else
                        {
                            AudioManager.Instance?.PlayGameOverSound();
                            ShowStatus("⚠️ Yetersiz altın! Koşulardan altın topla.", new Color(0.95f, 0.35f, 0.40f));
                        }
                    }
                    else
                    {
                        bool nowEq = SaveSystem.ToggleOutfitEquipped(charKey, currentType);
                        AudioManager.Instance?.PlayJumpSound();
                        AudioManager.Instance?.TriggerHapticFeedback();
                        ShowStatus(nowEq ? "✅ Kıyafet giyildi!" : "ℹ️ Kıyafet çıkarıldı.", nowEq ? new Color(0.024f, 0.839f, 0.627f) : new Color(0.7f, 0.8f, 0.9f));
                        PlayerController.Instance?.UpdateCharacterAppearance();
                        RefreshUI();
                    }
                }, btnColor, cfg.outfitType, charKey);

                spawnedRows.Add(row);
            }
        }

        private GameObject CreateRowObject(string name, float yPos, float rowHeight, string title, string subtitle, int price, string btnText, Font font, UnityEngine.Events.UnityAction onClick, Color? btnColor = null, string outfitType = null, string charKey = null, string iconEmoji = null, string shopItemId = null)
        {
            GameObject rowObj = new GameObject(name);
            rowObj.transform.SetParent(itemsContainer, false);
            RectTransform rt = rowObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, yPos);
            rt.sizeDelta = new Vector2(850f, rowHeight);

            Image bg = rowObj.AddComponent<Image>();
            bg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(
                new Color(0.10f, 0.13f, 0.24f, 0.96f),
                new Color(1f, 1f, 1f, 0.12f),
                1.5f, 22, 128, 128);
            bg.type = Image.Type.Sliced;

            // 1. Görsel Alanı: Kıyafet Sprite'ı veya Güçlendirme / Yetenek Rozet İkonu
            bool hasOutfit = !string.IsNullOrEmpty(outfitType);
            bool hasShopItem = !string.IsNullOrEmpty(shopItemId);
            bool hasEmoji = !string.IsNullOrEmpty(iconEmoji);
            bool hasVisual = hasOutfit || hasShopItem || hasEmoji;

            if (hasOutfit)
            {
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(rowObj.transform, false);
                RectTransform iconRt = iconObj.AddComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0f, 0.5f);
                iconRt.anchorMax = new Vector2(0f, 0.5f);
                iconRt.pivot = new Vector2(0f, 0.5f);
                iconRt.anchoredPosition = new Vector2(26f, 0f);
                iconRt.sizeDelta = new Vector2(104f, 104f);

                Image iconImg = iconObj.AddComponent<Image>();
                Sprite itemSprite = Resources.Load<Sprite>($"Sprites/Kiyafetler/kiyafet_{charKey}_{outfitType}");
                if (itemSprite != null)
                {
                    iconImg.sprite = itemSprite;
                    iconImg.preserveAspect = true;
                }
                else
                {
                    iconImg.color = new Color(1f, 1f, 1f, 0.25f);
                }
            }
            else if (hasShopItem || hasEmoji)
            {
                // Güçlendirme / Yetenek Rozet İkonu (Yeni yüklenen yüksek çözünürlüklü 2D ikonlar)
                GameObject badgeObj = new GameObject("IconBadge");
                badgeObj.transform.SetParent(rowObj.transform, false);
                RectTransform badgeRt = badgeObj.AddComponent<RectTransform>();
                badgeRt.anchorMin = new Vector2(0f, 0.5f);
                badgeRt.anchorMax = new Vector2(0f, 0.5f);
                badgeRt.pivot = new Vector2(0f, 0.5f);
                badgeRt.anchoredPosition = new Vector2(26f, 0f);
                badgeRt.sizeDelta = new Vector2(104f, 104f);

                Image badgeImg = badgeObj.AddComponent<Image>();
                badgeImg.preserveAspect = true;
                badgeImg.raycastTarget = false;

                Sprite shopIcon = null;
                if (!string.IsNullOrEmpty(shopItemId))
                {
                    shopIcon = ProceduralSpriteGenerator.GetSpriteOrGenerate($"Sprites/Shop/shop_{shopItemId}", null);
                }

                if (shopIcon != null)
                {
                    badgeImg.sprite = shopIcon;
                }
                else
                {
                    // Fallback rozet kutusu ve emoji
                    badgeImg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(
                        new Color(0.15f, 0.19f, 0.35f, 0.85f),
                        new Color(1f, 1f, 1f, 0.12f),
                        1f, 20, 128, 128);
                    badgeImg.type = Image.Type.Sliced;

                    GameObject emojiTxtObj = new GameObject("Emoji");
                    emojiTxtObj.transform.SetParent(badgeObj.transform, false);
                    RectTransform emojiRt = emojiTxtObj.AddComponent<RectTransform>();
                    emojiRt.anchorMin = Vector2.zero;
                    emojiRt.anchorMax = Vector2.one;
                    emojiRt.offsetMin = Vector2.zero;
                    emojiRt.offsetMax = Vector2.zero;

                    Text emojiTxt = emojiTxtObj.AddComponent<Text>();
                    emojiTxt.font = font;
                    emojiTxt.fontSize = 46;
                    emojiTxt.alignment = TextAnchor.MiddleCenter;
                    emojiTxt.text = iconEmoji;
                }
            }

            // 2. Metin Alanı (Daha büyük ve okunaklı)
            float textLeft = hasVisual ? 146f : 30f;
            float textWidth = hasVisual ? 460f : 570f;

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(rowObj.transform, false);
            RectTransform labelRt = labelObj.AddComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0.5f);
            labelRt.anchorMax = new Vector2(0f, 0.5f);
            labelRt.pivot = new Vector2(0f, 0.5f);
            labelRt.anchoredPosition = new Vector2(textLeft, 0f);
            labelRt.sizeDelta = new Vector2(textWidth, rowHeight - 16f);

            Text labelText = labelObj.AddComponent<Text>();
            labelText.font = font;
            labelText.fontSize = 28;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
            labelText.verticalOverflow = VerticalWrapMode.Truncate;
            labelText.color = Color.white;
            labelText.text = $"{title}\n<size=20><color=#b8c8dc>{subtitle}</color></size>";

            // 3. Buton Alanı (Genişletilmiş ve büyük)
            GameObject btnObj = new GameObject("ActionBtn");
            btnObj.transform.SetParent(rowObj.transform, false);
            RectTransform btnRt = btnObj.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1f, 0.5f);
            btnRt.anchorMax = new Vector2(1f, 0.5f);
            btnRt.pivot = new Vector2(1f, 0.5f);
            btnRt.anchoredPosition = new Vector2(-24f, 0f);
            btnRt.sizeDelta = new Vector2(200f, 68f);

            Image btnBg = btnObj.AddComponent<Image>();
            Color finalBtnColor = btnColor ?? new Color(1.0f, 0.82f, 0.40f);
            btnBg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(
                finalBtnColor,
                Color.clear,
                0f, 18, 128, 64);
            btnBg.type = Image.Type.Sliced;

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            GameObject btnTxtObj = new GameObject("Text");
            btnTxtObj.transform.SetParent(btnObj.transform, false);
            RectTransform btnTxtRt = btnTxtObj.AddComponent<RectTransform>();
            btnTxtRt.anchorMin = Vector2.zero;
            btnTxtRt.anchorMax = Vector2.one;
            btnTxtRt.offsetMin = Vector2.zero;
            btnTxtRt.offsetMax = Vector2.zero;

            Text bTxt = btnTxtObj.AddComponent<Text>();
            bTxt.font = font;
            bTxt.fontSize = 23;
            bTxt.fontStyle = FontStyle.Bold;
            bTxt.alignment = TextAnchor.MiddleCenter;
            bTxt.color = (finalBtnColor.r > 0.6f && finalBtnColor.g > 0.6f) ? new Color(0.05f, 0.08f, 0.13f) : Color.white;
            bTxt.text = (price > 0 && !btnText.Contains("🪙")) ? $"{btnText} ({price}🪙)" : btnText;

            return rowObj;
        }

        private void ShowStatus(string msg, Color col)
        {
            if (statusMessageText != null)
            {
                statusMessageText.text = msg;
                statusMessageText.color = col;
            }
        }

        public void CloseModal()
        {
            AudioManager.Instance?.PlayJumpSound();
            gameObject.SetActive(false);
        }

        private static Font GetSafeFont()
        {
            try
            {
                Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (f != null) return f;
            } catch { }
            try
            {
                Font f = Resources.GetBuiltinResource<Font>("Arial.ttf");
                if (f != null) return f;
            } catch { }
            Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
            return fonts != null && fonts.Length > 0 ? fonts[0] : null;
        }
    }
}
