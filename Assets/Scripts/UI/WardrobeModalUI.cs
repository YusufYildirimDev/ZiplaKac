using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZiplaKac.Core;
using ZiplaKac.Player;
using ZiplaKac.Utils;

namespace ZiplaKac.UI
{
    [System.Serializable]
    public class WardrobeItemConfig
    {
        public string outfitType;   // "pelerin", "gunes_gozlugu", "ozel_aksesuar", "parti_sapkasi"
        public string displayName;  // "Pelerin", "Güneş Gözlüğü", "Özel Aksesuar", "Parti Şapkası"
        public int price;           // 100, 150, 200, 250
    }

    /// <summary>
    /// Kıyafet ve Aksesuar Mağazası (Gardırop).
    /// Altınla kıyafet satın alma ve satın alınan eşyaları Giyme/Çıkarma (Equip/Unequip) modalı.
    /// </summary>
    public class WardrobeModalUI : MonoBehaviour
    {
        public static WardrobeModalUI Instance { get; private set; }

        [Header("Başlık ve Altın Göstergesi")]
        public Text titleText;
        public Text totalGoldText;
        public Text statusMessageText;
        public Button closeButton;

        [Header("Eşya Yuvaları (Prefab veya Dinamik Satırlar)")]
        public Transform itemsContainer;

        private readonly List<WardrobeItemConfig> itemConfigs = new List<WardrobeItemConfig>
        {
            new WardrobeItemConfig { outfitType = "pelerin",        displayName = "Pelerin",         price = 100 },
            new WardrobeItemConfig { outfitType = "gunes_gozlugu",  displayName = "Güneş Gözlüğü",   price = 150 },
            new WardrobeItemConfig { outfitType = "ozel_aksesuar",  displayName = "Özel Aksesuar",   price = 200 },
            new WardrobeItemConfig { outfitType = "parti_sapkasi",  displayName = "Parti Şapkası",   price = 250 }
        };

        private readonly List<GameObject> spawnedRows = new List<GameObject>();

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        public void RefreshUI()
        {
            int gold = SaveSystem.GetTotalGold();
            if (totalGoldText != null) totalGoldText.text = $"🪙 Toplam Altın: {gold}";
            if (statusMessageText != null) statusMessageText.text = "";

            string charKey = "klasik";
            if (WorldThemeManager.Instance != null)
            {
                charKey = WorldThemeManager.Instance.CurrentCharacterKey;
            }

            BuildOrUpdateItemRows(charKey);
        }

        private void BuildOrUpdateItemRows(string charKey)
        {
            if (itemsContainer == null) return;

            // Önceki satırları temizle
            for (int i = 0; i < spawnedRows.Count; i++)
            {
                if (spawnedRows[i] != null) Destroy(spawnedRows[i]);
            }
            spawnedRows.Clear();

            Font font = GetSafeFont();
            float startY = 85f;
            float rowHeight = 95f;

            for (int i = 0; i < itemConfigs.Count; i++)
            {
                var cfg = itemConfigs[i];
                float yPos = startY - (i * rowHeight);

                GameObject rowObj = new GameObject($"Row_{cfg.outfitType}");
                rowObj.transform.SetParent(itemsContainer, false);
                RectTransform rt = rowObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, yPos);
                rt.sizeDelta = new Vector2(620f, 85f);

                // Satır arka planı (Koyu kart)
                Image bg = rowObj.AddComponent<Image>();
                bg.color = new Color(0.10f, 0.13f, 0.24f, 0.90f);

                // 1. İkon
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(rowObj.transform, false);
                RectTransform iconRt = iconObj.AddComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0f, 0.5f);
                iconRt.anchorMax = new Vector2(0f, 0.5f);
                iconRt.anchoredPosition = new Vector2(50f, 0f);
                iconRt.sizeDelta = new Vector2(70f, 70f);

                Image iconImg = iconObj.AddComponent<Image>();
                Sprite itemSprite = Resources.Load<Sprite>($"Sprites/Kiyafetler/kiyafet_{charKey}_{cfg.outfitType}");
                if (itemSprite != null)
                {
                    iconImg.sprite = itemSprite;
                    iconImg.preserveAspect = true;
                }
                else
                {
                    iconImg.color = new Color(1f, 1f, 1f, 0.2f);
                }

                // 2. İsim & Fiyat Metni
                GameObject labelObj = new GameObject("Label");
                labelObj.transform.SetParent(rowObj.transform, false);
                RectTransform labelRt = labelObj.AddComponent<RectTransform>();
                labelRt.anchorMin = new Vector2(0f, 0.5f);
                labelRt.anchorMax = new Vector2(0f, 0.5f);
                labelRt.anchoredPosition = new Vector2(210f, 0f);
                labelRt.sizeDelta = new Vector2(230f, 70f);

                Text labelText = labelObj.AddComponent<Text>();
                labelText.font = font;
                labelText.fontSize = 24;
                labelText.fontStyle = FontStyle.Bold;
                labelText.alignment = TextAnchor.MiddleLeft;
                labelText.color = Color.white;

                bool isPurchased = SaveSystem.IsOutfitPurchased(charKey, cfg.outfitType);
                bool isEquipped = SaveSystem.IsOutfitEquipped(charKey, cfg.outfitType);

                if (isPurchased)
                {
                    labelText.text = $"{cfg.displayName}\n<size=18><color=#06d6a0>Sahipsin</color></size>";
                }
                else
                {
                    labelText.text = $"{cfg.displayName}\n<size=18><color=#ffd166>{cfg.price} 🪙</color></size>";
                }

                // 3. Eylem Butonu (Satın Al / Giy / Çıkar)
                GameObject btnObj = new GameObject("ActionBtn");
                btnObj.transform.SetParent(rowObj.transform, false);
                RectTransform btnRt = btnObj.AddComponent<RectTransform>();
                btnRt.anchorMin = new Vector2(1f, 0.5f);
                btnRt.anchorMax = new Vector2(1f, 0.5f);
                btnRt.anchoredPosition = new Vector2(-110f, 0f);
                btnRt.sizeDelta = new Vector2(190f, 60f);

                Image btnBg = btnObj.AddComponent<Image>();
                Button btn = btnObj.AddComponent<Button>();

                GameObject btnTxtObj = new GameObject("Text");
                btnTxtObj.transform.SetParent(btnObj.transform, false);
                RectTransform btnTxtRt = btnTxtObj.AddComponent<RectTransform>();
                btnTxtRt.anchorMin = Vector2.zero;
                btnTxtRt.anchorMax = Vector2.one;
                btnTxtRt.offsetMin = Vector2.zero;
                btnTxtRt.offsetMax = Vector2.zero;

                Text btnText = btnTxtObj.AddComponent<Text>();
                btnText.font = font;
                btnText.fontSize = 20;
                btnText.fontStyle = FontStyle.Bold;
                btnText.alignment = TextAnchor.MiddleCenter;

                string currentType = cfg.outfitType;
                int cost = cfg.price;

                if (!isPurchased)
                {
                    // Satın Al Butonu (Amber/Gold)
                    btnBg.color = new Color(1.0f, 0.82f, 0.40f);
                    btnText.color = new Color(0.08f, 0.10f, 0.18f);
                    btnText.text = $"SATIN AL";

                    btn.onClick.AddListener(() => OnPurchaseClicked(charKey, currentType, cost));
                }
                else if (isEquipped)
                {
                    // Çıkar Butonu (Koyu Kırmızı/Gölge)
                    btnBg.color = new Color(0.85f, 0.22f, 0.30f);
                    btnText.color = Color.white;
                    btnText.text = "ÇIKAR";

                    btn.onClick.AddListener(() => OnToggleEquipClicked(charKey, currentType));
                }
                else
                {
                    // Giy Butonu (Mint Yeşil)
                    btnBg.color = new Color(0.024f, 0.839f, 0.627f);
                    btnText.color = new Color(0.05f, 0.08f, 0.13f);
                    btnText.text = "GİY";

                    btn.onClick.AddListener(() => OnToggleEquipClicked(charKey, currentType));
                }

                spawnedRows.Add(rowObj);
            }
        }

        private void OnPurchaseClicked(string charKey, string outfitType, int cost)
        {
            bool success = SaveSystem.PurchaseOutfit(charKey, outfitType, cost);
            if (success)
            {
                AudioManager.Instance?.PlayNewRecordSound();
                AudioManager.Instance?.TriggerHapticFeedback();
                if (statusMessageText != null)
                {
                    statusMessageText.text = "🎉 Satın alındı ve giyildi!";
                    statusMessageText.color = new Color(0.024f, 0.839f, 0.627f);
                }

                if (PlayerController.Instance != null)
                {
                    PlayerController.Instance.UpdateCharacterAppearance();
                }
                RefreshUI();
            }
            else
            {
                AudioManager.Instance?.PlayGameOverSound();
                if (statusMessageText != null)
                {
                    statusMessageText.text = "⚠️ Yetersiz altın! Koşarak veya sandıktan altın topla.";
                    statusMessageText.color = new Color(0.95f, 0.35f, 0.40f);
                }
            }
        }

        private void OnToggleEquipClicked(string charKey, string outfitType)
        {
            bool nowEquipped = SaveSystem.ToggleOutfitEquipped(charKey, outfitType);
            AudioManager.Instance?.PlayJumpSound();
            AudioManager.Instance?.TriggerHapticFeedback();

            if (statusMessageText != null)
            {
                statusMessageText.text = nowEquipped ? "✅ Kıyafet giyildi!" : "ℹ️ Kıyafet çıkarıldı.";
                statusMessageText.color = nowEquipped ? new Color(0.024f, 0.839f, 0.627f) : new Color(0.7f, 0.8f, 0.9f);
            }

            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.UpdateCharacterAppearance();
            }

            RefreshUI();
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
