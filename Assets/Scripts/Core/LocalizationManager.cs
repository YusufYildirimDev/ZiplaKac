using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZiplaKac.Core
{
    /// <summary>
    /// Çoklu dil yöneticisi (Türkçe & İngilizce).
    /// Otomatik cihaz dili tespiti, PlayerPrefs ile kalıcı dil tercihi,
    /// anlık dil değiştirme ve tüm arayüzlerin dinamik güncellenmesini sağlar.
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }

        public const string PREF_KEY_LANGUAGE = "app_language";
        public const string LANG_TR = "tr";
        public const string LANG_EN = "en";

        public static event Action OnLanguageChanged;

        private static string currentLanguage = LANG_TR;
        public static string CurrentLanguage => currentLanguage;

        private static readonly Dictionary<string, string> localizedStrings = new Dictionary<string, string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            InitializeLanguage();
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeLanguage();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public static string GetDeviceSystemLanguage()
        {
            try
            {
                if (Application.systemLanguage == SystemLanguage.Turkish)
                {
                    return LANG_TR;
                }

                string sysLangStr = Application.systemLanguage.ToString();
                if (sysLangStr.IndexOf("turk", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return LANG_TR;
                }

                if (System.Globalization.CultureInfo.CurrentUICulture != null &&
                    System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("tr", StringComparison.OrdinalIgnoreCase))
                {
                    return LANG_TR;
                }

                if (System.Globalization.CultureInfo.CurrentCulture != null &&
                    System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("tr", StringComparison.OrdinalIgnoreCase))
                {
                    return LANG_TR;
                }

                if (System.Globalization.CultureInfo.InstalledUICulture != null &&
                    System.Globalization.CultureInfo.InstalledUICulture.TwoLetterISOLanguageName.Equals("tr", StringComparison.OrdinalIgnoreCase))
                {
                    return LANG_TR;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LocalizationManager] Sistem dili algılanırken hata: {ex.Message}");
            }

            return LANG_EN;
        }

        public static void InitializeLanguage()
        {
            // Cihazın sistem dilinden otomatik olarak çek ve oyunu o dilde aç
            currentLanguage = GetDeviceSystemLanguage();
            LoadLanguageStrings(currentLanguage);
        }

        public static void SetLanguage(string langCode)
        {
            if (langCode != LANG_TR && langCode != LANG_EN) langCode = LANG_EN;
            if (currentLanguage == langCode && localizedStrings.Count > 0) return;

            currentLanguage = langCode;
            LoadLanguageStrings(currentLanguage);
            OnLanguageChanged?.Invoke();
        }

        public static void ToggleLanguage()
        {
            SetLanguage(currentLanguage == LANG_TR ? LANG_EN : LANG_TR);
        }

        public static string Get(string key, string fallback = null)
        {
            if (string.IsNullOrEmpty(key)) return "";
            if (localizedStrings.Count == 0)
            {
                InitializeLanguage();
            }
            if (localizedStrings.TryGetValue(key, out string value))
            {
                return value;
            }
            return fallback ?? key;
        }

        public static string GetFormat(string key, params object[] args)
        {
            string format = Get(key);
            try
            {
                return string.Format(format, args);
            }
            catch
            {
                return format;
            }
        }

        private static void LoadLanguageStrings(string lang)
        {
            localizedStrings.Clear();

            // 1. Önce kod içi dahili sözlüğü yükle (Garanti fallback)
            var defaults = (lang == LANG_EN) ? EnglishDictionary : TurkishDictionary;
            foreach (var kvp in defaults)
            {
                localizedStrings[kvp.Key] = kvp.Value;
            }

            // 2. Varsa Resources/Localization/strings_{lang}.json üzerinden üzerine yaz
            try
            {
                TextAsset jsonFile = Resources.Load<TextAsset>($"Localization/strings_{lang}");
                if (jsonFile != null && !string.IsNullOrEmpty(jsonFile.text))
                {
                    ParseJsonIntoDictionary(jsonFile.text, localizedStrings);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LocalizationManager] JSON yüklenirken hata: {ex.Message}");
            }
        }

        private static void ParseJsonIntoDictionary(string json, Dictionary<string, string> dict)
        {
            // Hafif JSON sözlük ayrıştırıcı ("key": "value")
            int idx = 0;
            while (idx < json.Length)
            {
                int keyStart = json.IndexOf('"', idx);
                if (keyStart < 0) break;
                int keyEnd = json.IndexOf('"', keyStart + 1);
                if (keyEnd < 0) break;
                string key = json.Substring(keyStart + 1, keyEnd - keyStart - 1);

                int colon = json.IndexOf(':', keyEnd + 1);
                if (colon < 0) break;

                int valStart = json.IndexOf('"', colon + 1);
                if (valStart < 0) break;

                // Handle escaped quotes inside value
                int valEnd = valStart + 1;
                while (valEnd < json.Length)
                {
                    if (json[valEnd] == '"' && json[valEnd - 1] != '\\')
                    {
                        break;
                    }
                    valEnd++;
                }
                if (valEnd >= json.Length) break;

                string val = json.Substring(valStart + 1, valEnd - valStart - 1)
                    .Replace("\\\"", "\"")
                    .Replace("\\n", "\n")
                    .Replace("\\r", "");

                dict[key] = val;
                idx = valEnd + 1;
            }
        }

        #region Builtin Dictionaries

        private static readonly Dictionary<string, string> TurkishDictionary = new Dictionary<string, string>
        {
            // Genel
            { "lang_code", "TR" },
            { "lang_display", "🇹🇷 TR" },
            { "btn_close", "KAPAT" },
            { "btn_prev", "◀ ÖNCEKİ" },
            { "gold", "ALTIN" },
            { "score", "SKOR" },
            { "best", "EN İYİ" },

            // Ana Menü
            { "app_title", "= HOPPZY =" },
            { "app_subtitle", "tek dokunuş, sonsuz kaçış" },
            { "menu_select_character", "KARAKTER SEÇ" },
            { "menu_play", "▶   OYNA" },
            { "menu_shop_line1", "MAĞAZA &" },
            { "menu_shop_line2", "GARDIROP" },
            { "menu_reward_line1", "GÜNLÜK" },
            { "menu_reward_line2", "ÖDÜL" },
            { "menu_rules_line1", "NASIL" },
            { "menu_rules_line2", "OYNANIR?" },
            { "menu_ad_line1", "REKLAM İZLE" },
            { "menu_ad_line2", "CAN AL  ❤️" },
            { "menu_toast_reward", "❤️ +1 CAN & +25 ALTIN KAZANDIN!" },
            { "menu_toast_ad_not_ready", "Reklam henüz hazır değil!" },

            // HUD
            { "hud_score_label", "SKOR" },
            { "hud_gold_label", "ALTIN" },
            { "hud_life_label", "CAN" },
            { "hud_best_label", "EN İYİ" },
            { "hud_life_used", "❤️ CAN KURTARDI! (-1)" },
            { "hud_badge_shield", "Kalkan" },
            { "hud_badge_slow", "Yavaş" },
            { "hud_badge_multi", "x2 Puan" },
            { "hud_cd_shield", "KALKAN" },
            { "hud_cd_slow", "YAVAŞ" },
            { "hud_cd_multi", "X2 PUAN" },
            { "hud_control_tip", "DOKUN: ZIPLA  •  ÇİFT DOKUN: ÇİFT ZIPLA" },
            { "hud_combo_banner", "{0}X KOMBO!" },
            { "menu_life_label", "CAN" },
            { "menu_life_toast_info", "❤️ Ekstra Can ({0}): Bir engele çarptığında koşunu kurtarır ve devam etmeni sağlar!" },

            // Mağaza & Gardırop
            { "shop_title_upgrades", "GÜÇLER &\nYÜKSELTMELER" },
            { "shop_title_wardrobe", "GARDIROP\n(KIYAFETLER)" },
            { "shop_tab_upgrades", "📊  YÜKSELTMELER" },
            { "shop_tab_outfits", "👕  GARDIROP" },
            { "shop_total_gold", "🪙 Toplam Altın: {0}" },
            { "shop_btn_buy", "AL" },
            { "shop_btn_upgrade", "YÜKSELT" },
            { "shop_btn_equip", "GİY" },
            { "shop_btn_equipped", "GİYİLDİ" },
            { "shop_btn_unequip", "ÇIKAR" },
            { "shop_btn_max", "MAX" },
            { "shop_btn_unlocked", "AÇ" },
            { "shop_owned", "Sahipsin" },
            { "shop_open", "Açık" },
            { "shop_inventory", "Envanter: {0}" },
            { "shop_level", "Seviye {0}/{1}" },
            { "shop_price_label", "Fiyat: {0} Altın" },
            { "shop_toast_bought_consumable", "🎉 {0} satın alındı! (+1 Envanter)" },
            { "shop_toast_upgraded", "🎉 {0} seviye atladı!" },
            { "shop_toast_bought_outfit", "🎉 Satın alındı ve giyildi!" },
            { "shop_toast_equipped", "✅ Kıyafet giyildi!" },
            { "shop_toast_unequipped", "ℹ️ Kıyafet çıkarıldı." },
            { "shop_toast_no_gold", "⚠️ Yetersiz altın! Koşulardan altın topla." },

            // Mağaza Eşya İsimleri & Açıklamaları
            { "item_shield_name", "Başlangıç Kalkanı" },
            { "item_shield_desc", "Oyuna 1 kalkanla başla." },
            { "item_slow_start_name", "Yavaş Başlangıç" },
            { "item_slow_start_desc", "İlk 5 sn engel hızı %30 yavaş." },
            { "item_lucky_pu_name", "Şanslı Güçlendirme" },
            { "item_lucky_pu_desc", "İlk küre garantili 3 saniyede." },
            { "item_second_chance_name", "İkinci Şans Bileti" },
            { "item_second_chance_desc", "Çarpışınca 1 kez ücretsiz devam et." },
            { "item_pu_duration_name", "Güçlendirme Süresi" },
            { "item_pu_duration_desc", "Kalkan/yavaş/x2 süreleri +%20 uzar." },
            { "item_double_jump_name", "Çift Zıplama Ustası" },
            { "item_double_jump_desc", "2. zıplama yüksekliği +%15 artar." },
            { "item_combo_memory_name", "Kombo Hafızası" },
            { "item_combo_memory_desc", "Çarpışmada kombonun yarısı korunur." },
            { "item_signature_name", "İmza Yeteneği +1" },
            { "item_signature_desc", "Karakterin özel yeteneği güçlenir." },

            // Gardırop Kıyafetleri
            { "outfit_pelerin_name", "Pelerin" },
            { "outfit_gunes_gozlugu_name", "Güneş Gözlüğü" },
            { "outfit_parti_sapkasi_name", "Parti Şapkası" },
            { "outfit_ozel_aksesuar_name", "Özel Aksesuar" },

            // Karakterler
            { "char_classic_name", "Klasik" },
            { "char_classic_ability", "Dengeli Koşucu" },
            { "char_fire_name", "Ateş" },
            { "char_fire_ability", "Alev İzi & Hız" },
            { "char_ice_name", "Buz" },
            { "char_ice_ability", "Kayma & Yavaş Düşüş" },
            { "char_poison_name", "Zehir" },
            { "char_poison_ability", "Zehir Kalkanı" },
            { "char_toxic_name", "Zehir" },
            { "char_toxic_ability", "Zehir Kalkanı" },
            { "char_night_name", "Gece" },
            { "char_night_ability", "Gölge Adımı & Çift Zıplama" },
            { "char_shadow_name", "Gece" },
            { "char_shadow_ability", "Gölge Adımı & Çift Zıplama" },
            { "char_unlock_hint", "{0} Skora Ulaşarak Aç" },
            { "char_unlocked_toast", "🎉 YENİ KARAKTER AÇILDI: {0}! 🎉" },
            { "char_lock_badge", "{0} Skor" },

            // Oyun Sonu (Game Over)
            { "go_title", "OYUN BİTTİ" },
            { "go_sub", "Bir engele çarptın" },
            { "go_score_label", "BU KOŞUDAKİ SKORUN" },
            { "go_new_record", "★ YENİ REKOR! ★" },
            { "go_high_score", "En İyi Skor: {0}" },
            { "go_gold_earned", "+<color=#f6c445><b>{0}</b></color> <color=#cbd5e1><b>ALTIN KAZANDIN</b></color>    <color=#334a66>|</color>    <color=#7e9ebc><b>KASA:</b></color> <color=#f6c445><b>{1}</b></color> <color=#cbd5e1><b>ALTIN</b></color>" },
            { "go_last_scores", "Son Skorlar: {0}" },
            { "go_btn_revive_afford", "CAN AL\nDEVAM ET" },
            { "go_btn_revive_cost", "CAN AL\n(500 ALTIN)" },
            { "go_btn_retry", "TEKRAR DENE" },
            { "go_btn_menu", "ANA MENÜ" },
            { "go_btn_ad", "REKLAM İZLE" },
            { "go_btn_ad_claimed", "✓ +100 ALTIN" },

            // Günlük Ödül
            { "reward_title", "GÜNLÜK ÖDÜL" },
            { "reward_desc", "Her gün giriş yap, sandığı aç ve artan altın ödüllerini topla!" },
            { "reward_ready", "Bugünün sandığı açılmaya hazır!" },
            { "reward_streak", "{0}. GÜN SERİSİ! +{1} ALTIN KAZANDIN!" },
            { "reward_claimed", "Bugünkü ödülünü zaten aldın. Yarın tekrar gel!" },
            { "reward_btn_claim", "ÖDÜLÜ AL" },

            // Nasıl Oynanır (Onboarding)
            { "onb_s0_sub", "Zıpla, engellerden kaç ve rekorunu kır!" },
            { "onb_s0_r1_title", "Tek Dokunuşla Zıpla" },
            { "onb_s0_r1_desc", "Ekrana dokunarak veya Space tuşuna basarak yaklaşan sivri dikenlerin, kaktüs ve kayaların üzerinden atla." },
            { "onb_s0_r2_title", "Çift Zıplama (Double Jump)" },
            { "onb_s0_r2_desc", "Havada bir kez daha dokunarak ikinci kez zıpla! Yüksek ve arka arkaya gelen engelleri aşmak için en önemli yeteneğin." },
            { "onb_s0_r3_title", "Kombo Serisi & Bonus Altın" },
            { "onb_s0_r3_desc", "Engelleri çarpmadan art arda geç. Her 5 engelde ekstra kombo puanı ve çarpanlı altın ödülleri kazanırsın!" },
            { "onb_s0_r4_title", "3 Zorluk Aşaması (Gündüz, Akşam, Gece)" },
            { "onb_s0_r4_desc", "Puanın arttıkça dünya değişir, oyun hızlanır ve reflekslerini sonuna kadar zorlar!" },

            { "onb_s1_sub", "İlk başta sadece Klasik açıktır! Sırayla 2500 skor yaparak kilidi aç:" },
            { "onb_s1_c0_badge", "BAŞLANGIÇTA AÇIK" },
            { "onb_s1_c0_desc", "Çayır Diyarı • Kalıcı +%15 ekstra skor yeteneği!" },
            { "onb_s1_c1_badge", "🔒 Klasik ile 2500 Skor Yap" },
            { "onb_s1_c1_desc", "Volkan & Lav Diyarı • Ateş topu ve kor alev dünyası!" },
            { "onb_s1_c2_badge", "🔒 Ateş ile 2500 Skor Yap" },
            { "onb_s1_c2_desc", "Buzul & Kar Dağları • Dondurucu soğuk ve kutup gecesi!" },
            { "onb_s1_c3_badge", "🔒 Buz ile 2500 Skor Yap" },
            { "onb_s1_c3_desc", "Toksik Sis Ormanı • Kombo ödüllerini x1.25 katına çıkarır!" },
            { "onb_s1_c4_badge", "🔒 Zehir ile 2500 Skor Yap" },
            { "onb_s1_c4_desc", "Derin Uzay & Nebula • Engellerden %28 otomatik sıyrılma!" },

            { "onb_s2_sub", "Güçlendirmeleri topla ve mağazadan kalıcı yetenekler edin!" },
            { "onb_s2_r1_title", "Koruyucu Kalkan (Shield)" },
            { "onb_s2_r1_desc", "Bir engele çarpışmayı tamamen emer, engelleri temizler ve koşunu kurtarır." },
            { "onb_s2_r2_title", "Zamanı Yavaşlatma (Slow-Mo)" },
            { "onb_s2_r2_desc", "5 saniye boyunca dünya hızını %40 düşürerek karmaşık engellerden kolayca sıyrılmanı sağlar." },
            { "onb_s2_r3_title", "x2 Çift Skor & Altın Çarpanı" },
            { "onb_s2_r3_desc", "Aktif olduğu sürece kazanılan tüm koşu puanlarını ve engellerden gelen altınları 2 katına çıkarır!" },
            { "onb_s2_r4_title", "İkinci Şans Bileti & Kalıcı Güçler" },
            { "onb_s2_r4_desc", "Mağaza'dan İkinci Şans Bileti alıp yandığın yerden devam edebilir, zıplama ve kombo süreni geliştirebilirsin." },

            { "onb_s3_sub", "Her gün giriş yap, sandığını aç ve altınlarınla tarzını yarat!" },
            { "onb_s3_r1_title", "Günlük Ödül Sandığı" },
            { "onb_s3_r1_desc", "Her gün oyuna girerek sandığını aç! Günlük seri yaptıkça (Streak) kazandığın altın miktarı 20'den 50'ye artar!" },
            { "onb_s3_r2_title", "Altın Toplama & Yeni Rekorlar" },
            { "onb_s3_r2_desc", "Engelleri aştıkça, kombo yaptıkça ve rekor kırdıkça altın topla. Her koşu sonunda kasan zenginleşir!" },
            { "onb_s3_r3_title", "Gardırop & Kostümler" },
            { "onb_s3_r3_desc", "Topladığın altınlarla kahramanlarına süper kahraman pelerini, havalı güneş gözlükleri ve maskeler satın al!" }
        };

        private static readonly Dictionary<string, string> EnglishDictionary = new Dictionary<string, string>
        {
            // General
            { "lang_code", "EN" },
            { "lang_display", "🇬🇧 EN" },
            { "btn_close", "CLOSE" },
            { "btn_prev", "◀ BACK" },
            { "gold", "GOLD" },
            { "score", "SCORE" },
            { "best", "BEST" },

            // Main Menu
            { "app_title", "= HOPPZY =" },
            { "app_subtitle", "one tap, endless escape" },
            { "menu_select_character", "SELECT CHARACTER" },
            { "menu_play", "▶   PLAY" },
            { "menu_shop_line1", "SHOP &" },
            { "menu_shop_line2", "WARDROBE" },
            { "menu_reward_line1", "DAILY" },
            { "menu_reward_line2", "REWARD" },
            { "menu_rules_line1", "HOW TO" },
            { "menu_rules_line2", "PLAY?" },
            { "menu_ad_line1", "WATCH AD" },
            { "menu_ad_line2", "GET LIFE  ❤️" },
            { "menu_toast_reward", "❤️ +1 LIFE & +25 GOLD EARNED!" },
            { "menu_toast_ad_not_ready", "Ad is not ready yet!" },

            // HUD
            { "hud_score_label", "SCORE" },
            { "hud_gold_label", "GOLD" },
            { "hud_life_label", "LIFE" },
            { "hud_best_label", "BEST" },
            { "hud_life_used", "❤️ EXTRA LIFE SAVED! (-1)" },
            { "hud_badge_shield", "Shield" },
            { "hud_badge_slow", "Slow" },
            { "hud_badge_multi", "x2 Score" },
            { "hud_cd_shield", "SHIELD" },
            { "hud_cd_slow", "SLOW" },
            { "hud_cd_multi", "X2 SCORE" },
            { "hud_control_tip", "TAP: JUMP  •  DOUBLE TAP: DOUBLE JUMP" },
            { "hud_combo_banner", "{0}X COMBO!" },
            { "menu_life_label", "LIVES" },
            { "menu_life_toast_info", "❤️ Extra Life ({0}): Saves your run and lets you keep running after a crash!" },

            // Shop & Wardrobe
            { "shop_title_upgrades", "POWERS &\nUPGRADES" },
            { "shop_title_wardrobe", "WARDROBE\n(OUTFITS)" },
            { "shop_tab_upgrades", "📊  UPGRADES" },
            { "shop_tab_outfits", "👕  WARDROBE" },
            { "shop_total_gold", "🪙 Total Gold: {0}" },
            { "shop_btn_buy", "BUY" },
            { "shop_btn_upgrade", "UPGRADE" },
            { "shop_btn_equip", "EQUIP" },
            { "shop_btn_equipped", "EQUIPPED" },
            { "shop_btn_unequip", "UNEQUIP" },
            { "shop_btn_max", "MAX" },
            { "shop_btn_unlocked", "UNLOCK" },
            { "shop_owned", "Owned" },
            { "shop_open", "Active" },
            { "shop_inventory", "Inventory: {0}" },
            { "shop_level", "Level {0}/{1}" },
            { "shop_price_label", "Price: {0} Gold" },
            { "shop_toast_bought_consumable", "🎉 {0} purchased! (+1 Inventory)" },
            { "shop_toast_upgraded", "🎉 {0} upgraded!" },
            { "shop_toast_bought_outfit", "🎉 Purchased & equipped!" },
            { "shop_toast_equipped", "✅ Outfit equipped!" },
            { "shop_toast_unequipped", "ℹ️ Outfit removed." },
            { "shop_toast_no_gold", "⚠️ Not enough gold! Collect gold in runs." },

            // Shop Items & Upgrades
            { "item_shield_name", "Starter Shield" },
            { "item_shield_desc", "Start the run with 1 shield." },
            { "item_slow_start_name", "Slow Start" },
            { "item_slow_start_desc", "Obstacles 30% slower for first 5s." },
            { "item_lucky_pu_name", "Lucky Power-Up" },
            { "item_lucky_pu_desc", "First power-up orb within 3s." },
            { "item_second_chance_name", "Second Chance Ticket" },
            { "item_second_chance_desc", "Continue once for free upon crash." },
            { "item_pu_duration_name", "Power Duration" },
            { "item_pu_duration_desc", "Shield/slow/x2 timers +20% longer." },
            { "item_double_jump_name", "Double Jump Master" },
            { "item_double_jump_desc", "2nd jump height increased +15%." },
            { "item_combo_memory_name", "Combo Memory" },
            { "item_combo_memory_desc", "Retain 50% combo upon crashing." },
            { "item_signature_name", "Signature Ability +1" },
            { "item_signature_desc", "Boosts character unique skill." },

            // Wardrobe Outfits
            { "outfit_pelerin_name", "Cape" },
            { "outfit_gunes_gozlugu_name", "Sunglasses" },
            { "outfit_parti_sapkasi_name", "Party Hat" },
            { "outfit_ozel_aksesuar_name", "Royal Crown" },

            // Characters
            { "char_classic_name", "Classic" },
            { "char_classic_ability", "Balanced Runner" },
            { "char_fire_name", "Blaze" },
            { "char_fire_ability", "Fire Trail & Speed" },
            { "char_ice_name", "Frost" },
            { "char_ice_ability", "Glide & Slow Fall" },
            { "char_poison_name", "Venom" },
            { "char_poison_ability", "Poison Shield" },
            { "char_toxic_name", "Venom" },
            { "char_toxic_ability", "Poison Shield" },
            { "char_night_name", "Shadow" },
            { "char_night_ability", "Shadow Step & Double Jump" },
            { "char_shadow_name", "Shadow" },
            { "char_shadow_ability", "Shadow Step & Double Jump" },
            { "char_unlock_hint", "Reach {0} Score to Unlock" },
            { "char_unlocked_toast", "🎉 NEW CHARACTER UNLOCKED: {0}! 🎉" },
            { "char_lock_badge", "{0} Score" },

            // Game Over
            { "go_title", "GAME OVER" },
            { "go_sub", "You hit an obstacle" },
            { "go_score_label", "SCORE THIS RUN" },
            { "go_new_record", "★ NEW RECORD! ★" },
            { "go_high_score", "Best Score: {0}" },
            { "go_gold_earned", "+<color=#f6c445><b>{0}</b></color> <color=#cbd5e1><b>GOLD EARNED</b></color>    <color=#334a66>|</color>    <color=#7e9ebc><b>VAULT:</b></color> <color=#f6c445><b>{1}</b></color> <color=#cbd5e1><b>GOLD</b></color>" },
            { "go_last_scores", "Recent Scores: {0}" },
            { "go_btn_revive_afford", "REVIVE &\nCONTINUE" },
            { "go_btn_revive_cost", "REVIVE\n(500 GOLD)" },
            { "go_btn_retry", "TRY AGAIN" },
            { "go_btn_menu", "MAIN MENU" },
            { "go_btn_ad", "WATCH AD" },
            { "go_btn_ad_claimed", "✓ +100 GOLD" },

            // Daily Reward
            { "reward_title", "DAILY REWARD" },
            { "reward_desc", "Log in every day, open the chest and collect increasing gold rewards!" },
            { "reward_ready", "Today's chest is ready to open!" },
            { "reward_streak", "DAY {0} STREAK! +{1} GOLD EARNED!" },
            { "reward_claimed", "You already claimed today's reward. Come back tomorrow!" },
            { "reward_btn_claim", "CLAIM REWARD" },

            // How to Play (Onboarding)
            { "onb_s0_sub", "Jump, dodge obstacles, and beat your high score!" },
            { "onb_s0_r1_title", "One-Tap Jump" },
            { "onb_s0_r1_desc", "Tap the screen or press Space to jump over upcoming spikes, cacti, and rolling rocks." },
            { "onb_s0_r2_title", "Double Jump" },
            { "onb_s0_r2_desc", "Tap again mid-air to double jump! Your essential skill to clear tall and rapid consecutive obstacles." },
            { "onb_s0_r3_title", "Combo Streak & Bonus Gold" },
            { "onb_s0_r3_desc", "Clear obstacles consecutively without crashing. Earn extra combo points and multiplied gold every 5 obstacles!" },
            { "onb_s0_r4_title", "3 Difficulty Cycles (Day, Sunset, Night)" },
            { "onb_s0_r4_desc", "As your score climbs, the atmosphere changes, the game speeds up, and your reflexes are put to the test!" },

            { "onb_s1_sub", "Only Classic is unlocked at first! Reach 2500 score to unlock next:" },
            { "onb_s1_c0_badge", "UNLOCKED BY DEFAULT" },
            { "onb_s1_c0_desc", "Meadow Realm • Permanent +15% bonus score ability!" },
            { "onb_s1_c1_badge", "🔒 Reach 2500 with Classic" },
            { "onb_s1_c1_desc", "Volcano Realm • Fiery fireballs and glowing embers!" },
            { "onb_s1_c2_badge", "🔒 Reach 2500 with Blaze" },
            { "onb_s1_c2_desc", "Glacier Realm • Freezing blizzards and polar nights!" },
            { "onb_s1_c3_badge", "🔒 Reach 2500 with Frost" },
            { "onb_s1_c3_desc", "Toxic Forest • Multiplies combo rewards by x1.25!" },
            { "onb_s1_c4_badge", "🔒 Reach 2500 with Venom" },
            { "onb_s1_c4_desc", "Deep Space • 28% chance to automatically dodge obstacles!" },

            { "onb_s2_sub", "Collect power-ups and upgrade permanent abilities!" },
            { "onb_s2_r1_title", "Protective Shield" },
            { "onb_s2_r1_desc", "Completely absorbs a collision, destroys nearby obstacles, and saves your run." },
            { "onb_s2_r2_title", "Slow-Mo Time Warp" },
            { "onb_s2_r2_desc", "Slows world speed by 40% for 5 seconds, making difficult obstacles effortless to clear." },
            { "onb_s2_r3_title", "x2 Score & Gold Multiplier" },
            { "onb_s2_r3_desc", "Doubles all points scored and coins collected while active!" },
            { "onb_s2_r4_title", "Second Chance Ticket & Upgrades" },
            { "onb_s2_r4_desc", "Buy tickets in Shop to continue after crashing, or upgrade jump height and combo timers permanently." },

            { "onb_s3_sub", "Log in daily, open the chest and style your runner!" },
            { "onb_s3_r1_title", "Daily Reward Chest" },
            { "onb_s3_r1_desc", "Open your chest every day! Keep your daily login streak alive to earn larger gold rewards!" },
            { "onb_s3_r2_title", "Collect Gold & Break Records" },
            { "onb_s3_r2_desc", "Earn gold by dodging obstacles, building combos, and setting new personal bests!" },
            { "onb_s3_r3_title", "Wardrobe & Outfits" },
            { "onb_s3_r3_desc", "Use your gold to equip superhero capes, stylish sunglasses, and party hats for your runners!" }
        };

        #endregion
    }
}
