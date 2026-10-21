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
        public Image headerIllustration;
        public Text titleText;
        public Text totalGoldText;
        public Text statusMessageText;
        public Button closeButton;
        public Text closeButtonText;
        public Button topCloseButton;
        public Text topCloseButtonText;

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
            new ConsumableItemConfig { id = "shield",        displayName = "Başlangıç Kalkanı",    description = "Oyuna 1 kalkanla başla.", price = 90 },
            new ConsumableItemConfig { id = "slow_start",    displayName = "Yavaş Başlangıç",      description = "İlk 5 sn engel hızı %30 yavaş.", price = 60 },
            new ConsumableItemConfig { id = "lucky_pu",      displayName = "Şanslı Güçlendirme",   description = "İlk küre garantili 3 saniyede.", price = 45 },
            new ConsumableItemConfig { id = "second_chance", displayName = "İkinci Şans Bileti",   description = "Çarpışınca 1 kez ücretsiz devam et.", price = 120 }
        };

        private readonly List<UpgradeItemConfig> upgradeConfigs = new List<UpgradeItemConfig>
        {
            new UpgradeItemConfig { id = "pu_duration",   displayName = "Güçlendirme Süresi",   description = "Kalkan/yavaş/x2 süreleri +%20 uzar.", price = 450, maxLevel = 3 },
            new UpgradeItemConfig { id = "double_jump",   displayName = "Çift Zıplama Ustası",  description = "2. zıplama yüksekliği +%15 artar.",  price = 600, maxLevel = 1 },
            new UpgradeItemConfig { id = "combo_memory",  displayName = "Kombo Hafızası",       description = "Çarpışmada kombonun yarısı korunur.", price = 750, maxLevel = 1 },
            new UpgradeItemConfig { id = "signature",     displayName = "İmza Yeteneği +1",     description = "Karakterin özel yeteneği güçlenir.",  price = 900, maxLevel = 1 }
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

            LocalizationManager.OnLanguageChanged += RefreshUI;
            RefreshUI();
            if (itemsScrollRect != null) itemsScrollRect.verticalNormalizedPosition = 1f;
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= RefreshUI;
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
            if (totalGoldText != null) totalGoldText.text = LocalizationManager.GetFormat("shop_total_gold", gold.ToString("N0"));
            if (closeButtonText != null) closeButtonText.text = LocalizationManager.Get("btn_close");
            if (topCloseButtonText != null) topCloseButtonText.text = LocalizationManager.Get("btn_close");
            if (statusMessageText != null) statusMessageText.text = "";

            UpdateTabButtonStyles();

            string charKey = "klasik";
            if (WorldThemeManager.Instance != null)
            {
                charKey = WorldThemeManager.Instance.CurrentCharacterKey;
            }

            if (currentTab == ShopTab.Upgrades)
            {
                if (titleText != null) titleText.text = LocalizationManager.Get("shop_title_upgrades");
                if (headerIllustration != null)
                {
                    Sprite upgHdr = Resources.Load<Sprite>("Sprites/UI_Shop/header_upgrades");
                    if (upgHdr != null) headerIllustration.sprite = upgHdr;
                }
                BuildUpgradesTab(charKey);
            }
            else
            {
                if (titleText != null) titleText.text = LocalizationManager.Get("shop_title_wardrobe");
                if (headerIllustration != null)
                {
                    Sprite wardHdr = Resources.Load<Sprite>("Sprites/UI_Shop/header_wardrobe");
                    if (wardHdr != null) headerIllustration.sprite = wardHdr;
                }
                BuildOutfitsTab(charKey);
            }
        }

        private void UpdateTabButtonStyles()
        {
            Color activeColor = new Color(0.55f, 0.36f, 0.96f); // #8B5CF6 Vibrant Purple
            Color inactiveColor = new Color(0.08f, 0.12f, 0.22f, 0.95f); // #141E38
            Color activeTextColor = Color.white;
            Color inactiveTextColor = new Color(0.58f, 0.64f, 0.72f); // #94A3B8

            if (tabUpgradesBg != null) tabUpgradesBg.color = (currentTab == ShopTab.Upgrades) ? activeColor : inactiveColor;
            if (tabOutfitsBg != null) tabOutfitsBg.color = (currentTab == ShopTab.Outfits) ? activeColor : inactiveColor;

            if (tabUpgradesText != null)
            {
                tabUpgradesText.text = LocalizationManager.Get("shop_tab_upgrades");
                tabUpgradesText.color = (currentTab == ShopTab.Upgrades) ? activeTextColor : inactiveTextColor;
                tabUpgradesText.fontStyle = FontStyle.Bold;
                tabUpgradesText.fontSize = (currentTab == ShopTab.Upgrades) ? 30 : 28;
            }
            if (tabOutfitsText != null)
            {
                tabOutfitsText.text = LocalizationManager.Get("shop_tab_outfits");
                tabOutfitsText.color = (currentTab == ShopTab.Outfits) ? activeTextColor : inactiveTextColor;
                tabOutfitsText.fontStyle = FontStyle.Bold;
                tabOutfitsText.fontSize = (currentTab == ShopTab.Outfits) ? 30 : 28;
            }
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
                string itemName = LocalizationManager.Get($"item_{cfg.id}_name", cfg.displayName);
                string itemDesc = LocalizationManager.Get($"item_{cfg.id}_desc", cfg.description);
                string invText = LocalizationManager.GetFormat("shop_inventory", count);
                string subtitle = $"{itemDesc}\n<color=#10B981>{invText}</color>";

                Color goldBtnColor = new Color(0.98f, 0.75f, 0.14f); // #FBBF24 Altın Sarı
                string btnBuy = LocalizationManager.Get("shop_btn_buy");

                GameObject row = CreateRowObject($"Consumable_{cfg.id}", yPos, rowHeight, itemName, subtitle, cfg.price, btnBuy, font, () => {
                    if (SaveSystem.PurchaseConsumable(cfg.id, cfg.price))
                    {
                        AudioManager.Instance?.PlayNewRecordSound();
                        AudioManager.Instance?.TriggerHapticFeedback();
                        ShowStatus(LocalizationManager.GetFormat("shop_toast_bought_consumable", itemName), new Color(0.06f, 0.84f, 0.58f));
                        RefreshUI();
                    }
                    else
                    {
                        AudioManager.Instance?.PlayGameOverSound();
                        ShowStatus(LocalizationManager.Get("shop_toast_no_gold"), new Color(0.95f, 0.35f, 0.40f));
                    }
                }, goldBtnColor, null, null, null, cfg.id);
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

                string itemName = LocalizationManager.Get($"item_{cfg.id}_name", cfg.displayName);
                string itemDesc = LocalizationManager.Get($"item_{cfg.id}_desc", cfg.description);

                string subtitle = cfg.maxLevel > 1
                    ? $"{itemDesc}\n<color=#FBBF24>{LocalizationManager.GetFormat("shop_level", currentLvl, cfg.maxLevel)}</color>"
                    : (isMax ? $"{itemDesc}\n<color=#10B981>{LocalizationManager.Get("shop_open")}</color>" : itemDesc);

                string btnLabel = isMax 
                    ? LocalizationManager.Get("shop_btn_max") 
                    : (cfg.maxLevel > 1 ? LocalizationManager.Get("shop_btn_upgrade") : LocalizationManager.Get("shop_btn_unlocked"));
                Color btnColor = isMax ? new Color(0.20f, 0.25f, 0.35f) : new Color(0.06f, 0.84f, 0.58f);

                GameObject row = CreateRowObject($"Upgrade_{cfg.id}", yPos, rowHeight, itemName, subtitle, isMax ? 0 : cfg.price, btnLabel, font, () => {
                    if (isMax) return;
                    if (SaveSystem.PurchaseUpgrade(charKey, cfg.id, cfg.price, cfg.maxLevel))
                    {
                        AudioManager.Instance?.PlayNewRecordSound();
                        AudioManager.Instance?.TriggerHapticFeedback();
                        ShowStatus(LocalizationManager.GetFormat("shop_toast_upgraded", itemName), new Color(0.06f, 0.84f, 0.58f));
                        RefreshUI();
                    }
                    else
                    {
                        AudioManager.Instance?.PlayGameOverSound();
                        ShowStatus(LocalizationManager.Get("shop_toast_no_gold"), new Color(0.95f, 0.35f, 0.40f));
                    }
                }, btnColor, null, null, null, cfg.id);
                spawnedRows.Add(row);
            }
        }

        private void BuildOutfitsTab(string charKey)
        {
            if (itemsContainer == null) return;
            ClearSpawnedRows();

            Font font = GetSafeFont();
            float rowHeight = 175f;
            float spacing = 16f;
            float topPadding = 16f;
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

                string itemName = LocalizationManager.Get($"outfit_{cfg.outfitType}_name", cfg.displayName);
                string subtitle = isPurchased 
                    ? $"<color=#10B981>{LocalizationManager.Get("shop_owned")}</color>" 
                    : LocalizationManager.GetFormat("shop_price_label", cfg.price);

                string btnLabel = !isPurchased 
                    ? LocalizationManager.Get("shop_btn_buy") 
                    : (isEquipped ? LocalizationManager.Get("shop_btn_equipped") : LocalizationManager.Get("shop_btn_equip"));
                Color btnColor = !isPurchased 
                    ? new Color(0.98f, 0.75f, 0.14f) 
                    : (isEquipped ? new Color(0.12f, 0.55f, 0.88f) : new Color(0.06f, 0.84f, 0.58f));

                string currentType = cfg.outfitType;
                int cost = cfg.price;

                GameObject row = CreateRowObject($"Outfit_{cfg.outfitType}", yPos, rowHeight, itemName, subtitle, isPurchased ? 0 : cfg.price, btnLabel, font, () => {
                    if (!isPurchased)
                    {
                        if (SaveSystem.PurchaseOutfit(charKey, currentType, cost))
                        {
                            AudioManager.Instance?.PlayNewRecordSound();
                            AudioManager.Instance?.TriggerHapticFeedback();
                            ShowStatus(LocalizationManager.Get("shop_toast_bought_outfit"), new Color(0.06f, 0.84f, 0.58f));
                            PlayerController.Instance?.UpdateCharacterAppearance();
                            RefreshUI();
                        }
                        else
                        {
                            AudioManager.Instance?.PlayGameOverSound();
                            ShowStatus(LocalizationManager.Get("shop_toast_no_gold"), new Color(0.95f, 0.35f, 0.40f));
                        }
                    }
                    else
                    {
                        bool nowEq = SaveSystem.ToggleOutfitEquipped(charKey, currentType);
                        AudioManager.Instance?.PlayJumpSound();
                        AudioManager.Instance?.TriggerHapticFeedback();
                        ShowStatus(nowEq ? LocalizationManager.Get("shop_toast_equipped") : LocalizationManager.Get("shop_toast_unequipped"), nowEq ? new Color(0.06f, 0.84f, 0.58f) : new Color(0.7f, 0.8f, 0.9f));
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
                new Color(0.06f, 0.11f, 0.22f, 0.98f),
                new Color(0.12f, 0.22f, 0.40f, 0.50f),
                1.5f, 20, 128, 128);
            bg.type = Image.Type.Sliced;

            // 1. Görsel Alanı: Kıyafet Sprite'ı veya Güçlendirme / Yetenek Rozet İkonu (118x118)
            bool hasOutfit = !string.IsNullOrEmpty(outfitType);
            bool hasShopItem = !string.IsNullOrEmpty(shopItemId);

            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(rowObj.transform, false);
            RectTransform iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = new Vector2(22f, 0f);
            iconRt.sizeDelta = new Vector2(125f, 125f);

            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            Sprite itemSprite = null;
            if (hasOutfit)
            {
                itemSprite = Resources.Load<Sprite>($"Sprites/Wardrobe/outfit_{outfitType}");
                if (itemSprite == null) itemSprite = Resources.Load<Sprite>($"Sprites/Kiyafetler/kiyafet_{charKey}_{outfitType}");
            }
            else if (hasShopItem)
            {
                itemSprite = Resources.Load<Sprite>($"Sprites/Shop/shop_{shopItemId}");
                if (itemSprite == null) itemSprite = ProceduralSpriteGenerator.GetSpriteOrGenerate($"Sprites/Shop/shop_{shopItemId}", null);
            }

            if (itemSprite != null)
            {
                iconImg.sprite = itemSprite;
            }
            else
            {
                iconImg.color = new Color(1f, 1f, 1f, 0.25f);
            }

            // 2. Metin Alanı
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(rowObj.transform, false);
            RectTransform labelRt = labelObj.AddComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0.5f);
            labelRt.anchorMax = new Vector2(0f, 0.5f);
            labelRt.pivot = new Vector2(0f, 0.5f);
            labelRt.anchoredPosition = new Vector2(165f, 0f);
            labelRt.sizeDelta = new Vector2(430f, 160f);

            Text labelText = labelObj.AddComponent<Text>();
            labelText.font = font;
            labelText.fontSize = 28;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
            labelText.verticalOverflow = VerticalWrapMode.Overflow;
            labelText.lineSpacing = 1.05f;
            labelText.color = Color.white;
            labelText.text = $"{title}\n<size=22><color=#94A3B8>{subtitle}</color></size>";

            // 3. Buton Alanı (245x76)
            GameObject btnObj = new GameObject("ActionBtn");
            btnObj.transform.SetParent(rowObj.transform, false);
            RectTransform btnRt = btnObj.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1f, 0.5f);
            btnRt.anchorMax = new Vector2(1f, 0.5f);
            btnRt.pivot = new Vector2(1f, 0.5f);
            btnRt.anchoredPosition = new Vector2(-16f, 0f);
            btnRt.sizeDelta = new Vector2(245f, 76f);

            Image btnBg = btnObj.AddComponent<Image>();
            Color finalBtnColor = btnColor ?? new Color(0.98f, 0.75f, 0.14f);
            btnBg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(
                finalBtnColor,
                Color.clear,
                0f, 22, 128, 64);
            btnBg.type = Image.Type.Sliced;

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            Color textCol = (finalBtnColor.r > 0.6f && finalBtnColor.g > 0.6f) 
                ? new Color(0.12f, 0.08f, 0.02f) 
                : ((finalBtnColor.g > 0.7f && finalBtnColor.r < 0.3f) 
                    ? new Color(0.02f, 0.14f, 0.08f) 
                    : Color.white);

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
                hlg.spacing = 6f;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;

                // 1. Eylem Adı (AL / YÜKSELT / AÇ)
                GameObject actObj = new GameObject("ActTxt");
                actObj.transform.SetParent(contentObj.transform, false);
                Text actTxt = actObj.AddComponent<Text>();
                actTxt.font = font;
                actTxt.fontSize = 22;
                actTxt.fontStyle = FontStyle.Bold;
                actTxt.color = textCol;
                actTxt.text = btnText;
                actTxt.alignment = TextAnchor.MiddleCenter;
                actObj.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

                // 2. Altın Sikke Görseli (24x24)
                GameObject coinObj = new GameObject("CoinIcon");
                coinObj.transform.SetParent(contentObj.transform, false);
                RectTransform coinRt = coinObj.AddComponent<RectTransform>();
                coinRt.sizeDelta = new Vector2(24f, 24f);
                Image coinImg = coinObj.AddComponent<Image>();
                coinImg.sprite = ProceduralSpriteGenerator.CreateGoldCoinSprite(64);
                coinImg.preserveAspect = true;
                coinImg.raycastTarget = false;

                // 3. Fiyat Rakamı
                GameObject priceObj = new GameObject("PriceTxt");
                priceObj.transform.SetParent(contentObj.transform, false);
                Text priceTxt = priceObj.AddComponent<Text>();
                priceTxt.font = font;
                priceTxt.fontSize = 22;
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
                btnTxtRt.offsetMin = new Vector2(8f, 0f);
                btnTxtRt.offsetMax = new Vector2(-8f, 0f);

                Text bTxt = btnTxtObj.AddComponent<Text>();
                bTxt.font = font;
                bTxt.fontSize = 24;
                bTxt.fontStyle = FontStyle.Bold;
                bTxt.alignment = TextAnchor.MiddleCenter;
                bTxt.resizeTextForBestFit = true;
                bTxt.resizeTextMinSize = 18;
                bTxt.resizeTextMaxSize = 26;
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
