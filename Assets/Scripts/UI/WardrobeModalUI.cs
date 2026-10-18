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
            new WardrobeItemConfig { outfitType = "pelerin",        displayName = "Pelerin",         price = 240 },
            new WardrobeItemConfig { outfitType = "gunes_gozlugu",  displayName = "Güneş Gözlüğü",   price = 240 },
            new WardrobeItemConfig { outfitType = "parti_sapkasi",  displayName = "Parti Şapkası",   price = 240 },
            new WardrobeItemConfig { outfitType = "ozel_aksesuar",  displayName = "Özel Aksesuar",   price = 360 }
        };

        private readonly List<ConsumableItemConfig> consumableConfigs = new List<ConsumableItemConfig>
        {
            new ConsumableItemConfig { id = "shield",        displayName = "Başlangıç Kalkanı",    description = "Oyuna 1 kalkanla başla", price = 90 },
            new ConsumableItemConfig { id = "slow_start",    displayName = "Yavaş Başlangıç",      description = "İlk 5 saniye engel hızı %30 yavaşlar", price = 60 },
            new ConsumableItemConfig { id = "lucky_pu",      displayName = "Şanslı Güçlendirme",   description = "İlk güçlendirme 3. saniyede gelir", price = 45 },
            new ConsumableItemConfig { id = "second_chance", displayName = "İkinci Şans Bileti",   description = "Yandığında baştan başlamak yerine devam et", price = 120 }
        };

        private readonly List<UpgradeItemConfig> upgradeConfigs = new List<UpgradeItemConfig>
        {
            new UpgradeItemConfig { id = "pu_duration",   displayName = "Güçlendirme Süresi",   description = "Güçlendirme süreleri +%20 uzar", price = 450, maxLevel = 3 },
            new UpgradeItemConfig { id = "double_jump",   displayName = "Çift Zıplama Ustası",  description = "İkinci zıplama yüksekliği +%15 artar",  price = 600, maxLevel = 1 },
            new UpgradeItemConfig { id = "combo_memory",  displayName = "Kombo Hafızası",       description = "Çarpışmada kombonun yarısı korunur", price = 750, maxLevel = 1 },
            new UpgradeItemConfig { id = "signature",     displayName = "İmza Yeteneği +1",     description = "Karakterin özel yeteneği güçlenir",  price = 900, maxLevel = 1 }
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
            float rowHeight = 175f;
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
                string countBadge = count > 0 ? $"<color=#4ade80><b>[Mevcut: {count}]</b></color>" : "<color=#94a3b8>[Mevcut: 0]</color>";
                string subtitle = $"{cfg.description} · {countBadge}";

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

                string lvlBadge = isMax
                    ? "<color=#4ade80><b>[MAKSİMUM SEVİYE]</b></color>"
                    : (cfg.maxLevel > 1 ? $"<color=#ffd166><b>[Seviye {currentLvl}/{cfg.maxLevel}]</b></color>" : "<color=#94a3b8>[Pasif Kilitli]</color>");

                string subtitle = $"{cfg.description} · {lvlBadge}";

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
            float rowHeight = 175f;
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

                string subtitle = !isPurchased 
                    ? "<color=#ffd166><b>[Henüz Alınmadı]</b></color>" 
                    : (isEquipped ? "<color=#4ade80><b>[Şu An Giyili ✓]</b></color>" : "<color=#60a5fa><b>[Gardırobunda]</b></color>");

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

            // 1. Görsel Alanı: Kıyafet Sprite'ı veya Güçlendirme / Yetenek Rozet İkonu (136x136)
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
                iconRt.anchoredPosition = new Vector2(20f, 0f);
                iconRt.sizeDelta = new Vector2(136f, 136f);

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
                // Güçlendirme / Yetenek Rozet İkonu: 136x136
                GameObject badgeObj = new GameObject("IconBadge");
                badgeObj.transform.SetParent(rowObj.transform, false);
                RectTransform badgeRt = badgeObj.AddComponent<RectTransform>();
                badgeRt.anchorMin = new Vector2(0f, 0.5f);
                badgeRt.anchorMax = new Vector2(0f, 0.5f);
                badgeRt.pivot = new Vector2(0f, 0.5f);
                badgeRt.anchoredPosition = new Vector2(20f, 0f);
                badgeRt.sizeDelta = new Vector2(136f, 136f);

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
                    emojiTxt.fontSize = 64;
                    emojiTxt.alignment = TextAnchor.MiddleCenter;
                    emojiTxt.text = iconEmoji;
                }
            }

            // 2. Metin Alanı (Daha büyük ve okunaklı: 36pt başlık / 26pt alt yazı)
            float textLeft = hasVisual ? 172f : 24f;
            float textWidth = hasVisual ? 425f : 570f;

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
            labelText.fontSize = 36;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
            labelText.verticalOverflow = VerticalWrapMode.Truncate;
            labelText.color = Color.white;
            labelText.text = $"{title}\n<size=26><color=#cbd5e1>{subtitle}</color></size>";

            // 3. Buton Alanı (Genişletilmiş ve büyük: 240x86)
            GameObject btnObj = new GameObject("ActionBtn");
            btnObj.transform.SetParent(rowObj.transform, false);
            RectTransform btnRt = btnObj.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1f, 0.5f);
            btnRt.anchorMax = new Vector2(1f, 0.5f);
            btnRt.pivot = new Vector2(1f, 0.5f);
            btnRt.anchoredPosition = new Vector2(-16f, 0f);
            btnRt.sizeDelta = new Vector2(240f, 86f);

            Image btnBg = btnObj.AddComponent<Image>();
            Color finalBtnColor = btnColor ?? new Color(1.0f, 0.82f, 0.40f);
            btnBg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(
                finalBtnColor,
                Color.clear,
                0f, 18, 128, 64);
            btnBg.type = Image.Type.Sliced;

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            Color textCol = (finalBtnColor.r > 0.6f && finalBtnColor.g > 0.6f) ? new Color(0.05f, 0.08f, 0.13f) : Color.white;

            if (price > 0)
            {
                // Buton İçi: Eylem Metni + Altın İkonu Görseli + Fiyat Sayısı
                GameObject contentObj = new GameObject("BtnContent");
                contentObj.transform.SetParent(btnObj.transform, false);
                RectTransform contentRt = contentObj.AddComponent<RectTransform>();
                contentRt.anchorMin = Vector2.zero;
                contentRt.anchorMax = Vector2.one;
                contentRt.offsetMin = Vector2.zero;
                contentRt.offsetMax = Vector2.zero;

                HorizontalLayoutGroup hlg = contentObj.AddComponent<HorizontalLayoutGroup>();
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.spacing = 8f;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;

                // 1. Eylem Adı (AL / YÜKSELT)
                GameObject actObj = new GameObject("ActTxt");
                actObj.transform.SetParent(contentObj.transform, false);
                Text actTxt = actObj.AddComponent<Text>();
                actTxt.font = font;
                actTxt.fontSize = 26;
                actTxt.fontStyle = FontStyle.Bold;
                actTxt.color = textCol;
                actTxt.text = btnText;
                actTxt.alignment = TextAnchor.MiddleCenter;
                actObj.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

                // 2. Altın Sikke Görseli (32x32)
                GameObject coinObj = new GameObject("CoinIcon");
                coinObj.transform.SetParent(contentObj.transform, false);
                RectTransform coinRt = coinObj.AddComponent<RectTransform>();
                coinRt.sizeDelta = new Vector2(32f, 32f);
                Image coinImg = coinObj.AddComponent<Image>();
                coinImg.sprite = ProceduralSpriteGenerator.CreateGoldCoinSprite(64);
                coinImg.preserveAspect = true;
                coinImg.raycastTarget = false;

                // 3. Fiyat Rakamı
                GameObject priceObj = new GameObject("PriceTxt");
                priceObj.transform.SetParent(contentObj.transform, false);
                Text priceTxt = priceObj.AddComponent<Text>();
                priceTxt.font = font;
                priceTxt.fontSize = 26;
                priceTxt.fontStyle = FontStyle.Bold;
                priceTxt.color = textCol;
                priceTxt.text = price.ToString();
                priceTxt.alignment = TextAnchor.MiddleCenter;
                priceObj.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            else
            {
                // Standart Buton Metni (GİY, ÇIKAR, MAX, AÇIK)
                GameObject btnTxtObj = new GameObject("Text");
                btnTxtObj.transform.SetParent(btnObj.transform, false);
                RectTransform btnTxtRt = btnTxtObj.AddComponent<RectTransform>();
                btnTxtRt.anchorMin = Vector2.zero;
                btnTxtRt.anchorMax = Vector2.one;
                btnTxtRt.offsetMin = Vector2.zero;
                btnTxtRt.offsetMax = Vector2.zero;

                Text bTxt = btnTxtObj.AddComponent<Text>();
                bTxt.font = font;
                bTxt.fontSize = 28;
                bTxt.fontStyle = FontStyle.Bold;
                bTxt.alignment = TextAnchor.MiddleCenter;
                bTxt.color = textCol;
                bTxt.text = btnText;
            }

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
