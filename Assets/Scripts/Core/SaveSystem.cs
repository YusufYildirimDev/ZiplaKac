using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZiplaKac.Core
{
    [System.Serializable]
    public class ScoreHistoryWrapper
    {
        public List<int> scores = new List<int>();
    }

    /// <summary>
    /// PlayerPrefs tabanlı kalıcı veri saklama sistemi.
    /// En yüksek skor, seçili karakter ID'si ve son 5 oyun skor geçmişi saklanır.
    /// </summary>
    public static class SaveSystem
    {
        private const string KEY_HIGH_SCORE = "ZiplaKac_HighScore";
        private const string KEY_SELECTED_CHARACTER = "ZiplaKac_SelectedCharacter";
        private const string KEY_SCORE_HISTORY = "ZiplaKac_ScoreHistory";

        public static int GetHighScore()
        {
            return PlayerPrefs.GetInt(KEY_HIGH_SCORE, 0);
        }

        /// <summary>
        /// Yeni skoru kaydeder. Eğer yeni rekor kırıldıysa true döner.
        /// Ayrıca son 5 oyun skor listesini günceller.
        /// </summary>
        public static bool SaveScore(int score)
        {
            bool isNewRecord = false;
            int currentHighScore = GetHighScore();

            if (score > currentHighScore)
            {
                PlayerPrefs.SetInt(KEY_HIGH_SCORE, score);
                isNewRecord = true;
            }

            AddScoreToHistory(score);
            PlayerPrefs.Save();

            return isNewRecord;
        }

        public static string GetSelectedCharacterId(string defaultId = "classic")
        {
            return PlayerPrefs.GetString(KEY_SELECTED_CHARACTER, defaultId);
        }

        public static void SaveSelectedCharacterId(string characterId)
        {
            PlayerPrefs.SetString(KEY_SELECTED_CHARACTER, characterId);
            PlayerPrefs.Save();
        }

        private const string KEY_OUTFIT_ENABLED = "ZiplaKac_OutfitEnabled";
        private const string KEY_TOTAL_GOLD = "ZiplaKac_TotalGold";
        private const string KEY_PURCHASED_OUTFITS = "ZiplaKac_PurchasedOutfits";
        private const string KEY_EQUIPPED_OUTFITS = "ZiplaKac_EquippedOutfits";

        public static int GetTotalGold()
        {
            // Başlangıçta oyuncuya hemen 150 altın verilir (ilk eşyayı alabilmesi için)
            return PlayerPrefs.GetInt(KEY_TOTAL_GOLD, 150);
        }

        public static void AddGold(int amount)
        {
            if (amount <= 0) return;
            int total = GetTotalGold() + amount;
            PlayerPrefs.SetInt(KEY_TOTAL_GOLD, total);
            PlayerPrefs.Save();
        }

        public static bool SpendGold(int amount)
        {
            int total = GetTotalGold();
            if (total >= amount)
            {
                PlayerPrefs.SetInt(KEY_TOTAL_GOLD, total - amount);
                PlayerPrefs.Save();
                return true;
            }
            return false;
        }

        public static bool IsOutfitPurchased(string characterId, string outfitType)
        {
            // Klasik pelerin başlangıçta hediye olarak açıktır
            if (characterId == "klasik" && outfitType == "pelerin") return true;

            string key = $"{characterId}_{outfitType}".ToLowerInvariant();
            string purchased = PlayerPrefs.GetString(KEY_PURCHASED_OUTFITS, "klasik_pelerin");
            return purchased.Contains(key);
        }

        public static bool PurchaseOutfit(string characterId, string outfitType, int cost)
        {
            if (IsOutfitPurchased(characterId, outfitType)) return true;

            if (SpendGold(cost))
            {
                string key = $"{characterId}_{outfitType}".ToLowerInvariant();
                string purchased = PlayerPrefs.GetString(KEY_PURCHASED_OUTFITS, "klasik_pelerin");
                purchased += "," + key;
                PlayerPrefs.SetString(KEY_PURCHASED_OUTFITS, purchased);

                // Satın alınınca otomatik olarak giyilsin
                SetOutfitEquipped(characterId, outfitType, true);
                PlayerPrefs.Save();
                return true;
            }
            return false;
        }

        public static bool IsOutfitEquipped(string characterId, string outfitType)
        {
            if (!IsOutfitPurchased(characterId, outfitType)) return false;

            string key = $"{characterId}_{outfitType}".ToLowerInvariant();
            // Varsayılan olarak klasik pelerin giyili
            string equipped = PlayerPrefs.GetString(KEY_EQUIPPED_OUTFITS, "klasik_pelerin");
            return equipped.Contains(key);
        }

        public static void SetOutfitEquipped(string characterId, string outfitType, bool isEquipped)
        {
            string key = $"{characterId}_{outfitType}".ToLowerInvariant();
            string equipped = PlayerPrefs.GetString(KEY_EQUIPPED_OUTFITS, "klasik_pelerin");
            List<string> list = new List<string>(equipped.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries));

            if (isEquipped)
            {
                if (!list.Contains(key)) list.Add(key);
            }
            else
            {
                list.Remove(key);
            }

            PlayerPrefs.SetString(KEY_EQUIPPED_OUTFITS, string.Join(",", list));
            PlayerPrefs.Save();
        }

        public static bool ToggleOutfitEquipped(string characterId, string outfitType)
        {
            bool currentlyEquipped = IsOutfitEquipped(characterId, outfitType);
            SetOutfitEquipped(characterId, outfitType, !currentlyEquipped);
            return !currentlyEquipped;
        }

        public static bool IsOutfitEnabled()
        {
            return PlayerPrefs.GetInt(KEY_OUTFIT_ENABLED, 1) == 1;
        }

        public static void SetOutfitEnabled(bool enabled)
        {
            PlayerPrefs.SetInt(KEY_OUTFIT_ENABLED, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static List<int> GetLastFiveScores()
        {
            string json = PlayerPrefs.GetString(KEY_SCORE_HISTORY, "");
            if (string.IsNullOrEmpty(json))
            {
                return new List<int>();
            }

            try
            {
                ScoreHistoryWrapper wrapper = JsonUtility.FromJson<ScoreHistoryWrapper>(json);
                return wrapper != null ? wrapper.scores : new List<int>();
            }
            catch
            {
                return new List<int>();
            }
        }

        private static void AddScoreToHistory(int newScore)
        {
            List<int> history = GetLastFiveScores();
            // En başa ekle (En yeni skor üstte)
            history.Insert(0, newScore);

            // Maksimum 5 eleman tut
            if (history.Count > 5)
            {
                history = history.GetRange(0, 5);
            }

            ScoreHistoryWrapper wrapper = new ScoreHistoryWrapper { scores = history };
            string json = JsonUtility.ToJson(wrapper);
            PlayerPrefs.SetString(KEY_SCORE_HISTORY, json);
        }

        private const string KEY_LAST_DAILY_REWARD = "ZiplaKac_LastDailyRewardDate";
        private const string KEY_UNLOCKED_CHARACTERS = "ZiplaKac_UnlockedCharacters";
        private const string KEY_CONSUMABLE_PREFIX = "ZiplaKac_Consumable_";
        private const string KEY_UPGRADE_PREFIX = "ZiplaKac_Upgrade_";
        private const string KEY_DAILY_STREAK = "ZiplaKac_DailyStreak";
        private const string KEY_LAST_STREAK_DATE = "ZiplaKac_LastStreakDate";

        public static event System.Action OnConsumablesChanged;

        // Consumables: "shield", "slow_start", "lucky_pu", "second_chance"
        public static int GetConsumableCount(string consumableId)
        {
            return PlayerPrefs.GetInt(KEY_CONSUMABLE_PREFIX + consumableId, 0);
        }

        public static void AddConsumable(string consumableId, int count = 1)
        {
            int current = GetConsumableCount(consumableId);
            PlayerPrefs.SetInt(KEY_CONSUMABLE_PREFIX + consumableId, current + count);
            PlayerPrefs.Save();
            OnConsumablesChanged?.Invoke();
        }

        public static bool UseConsumable(string consumableId)
        {
            int current = GetConsumableCount(consumableId);
            if (current > 0)
            {
                PlayerPrefs.SetInt(KEY_CONSUMABLE_PREFIX + consumableId, current - 1);
                PlayerPrefs.Save();
                OnConsumablesChanged?.Invoke();
                return true;
            }
            return false;
        }

        public static bool PurchaseConsumable(string consumableId, int price)
        {
            if (SpendGold(price))
            {
                AddConsumable(consumableId, 1);
                return true;
            }
            return false;
        }

        // Upgrades: "pu_duration", "double_jump", "combo_memory", "signature"
        public static int GetUpgradeLevel(string characterId, string upgradeId)
        {
            return PlayerPrefs.GetInt($"{KEY_UPGRADE_PREFIX}{characterId}_{upgradeId}", 0);
        }

        public static void SetUpgradeLevel(string characterId, string upgradeId, int level)
        {
            PlayerPrefs.SetInt($"{KEY_UPGRADE_PREFIX}{characterId}_{upgradeId}", level);
            PlayerPrefs.Save();
        }

        public static bool PurchaseUpgrade(string characterId, string upgradeId, int price, int maxLevel = 1)
        {
            int currentLevel = GetUpgradeLevel(characterId, upgradeId);
            if (currentLevel >= maxLevel) return false;

            if (SpendGold(price))
            {
                SetUpgradeLevel(characterId, upgradeId, currentLevel + 1);
                return true;
            }
            return false;
        }

        public static int GetDailyStreak()
        {
            return PlayerPrefs.GetInt(KEY_DAILY_STREAK, 0);
        }

        public static int CalculateDailyLoginBonus()
        {
            int streak = GetDailyStreak() + 1;
            if (streak == 1) return 20;
            if (streak == 2) return 25;
            if (streak == 3) return 30;
            if (streak == 4) return 35;
            return 50;
        }

        public static bool CanClaimDailyReward()
        {
            string today = System.DateTime.UtcNow.ToString("yyyy-MM-dd");
            string lastClaim = PlayerPrefs.GetString(KEY_LAST_DAILY_REWARD, "");
            return lastClaim != today;
        }

        public static int ClaimDailyReward()
        {
            string today = System.DateTime.UtcNow.ToString("yyyy-MM-dd");
            string yesterday = System.DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
            string lastDate = PlayerPrefs.GetString(KEY_LAST_STREAK_DATE, "");

            int currentStreak = PlayerPrefs.GetInt(KEY_DAILY_STREAK, 0);
            if (lastDate == yesterday)
            {
                currentStreak++;
            }
            else if (lastDate != today)
            {
                currentStreak = 1;
            }

            PlayerPrefs.SetInt(KEY_DAILY_STREAK, currentStreak);
            PlayerPrefs.SetString(KEY_LAST_STREAK_DATE, today);
            PlayerPrefs.SetString(KEY_LAST_DAILY_REWARD, today);

            int goldBonus = (currentStreak == 1) ? 20 : (currentStreak == 2) ? 25 : (currentStreak == 3) ? 30 : (currentStreak == 4) ? 35 : 50;
            AddGold(goldBonus);
            PlayerPrefs.Save();
            return goldBonus;
        }

        #region Karakter Kilidi Açma ve Sıralı İlerleme Sistemi (2500 Skor Kuralı)
        public const int AVATAR_UNLOCK_SCORE_THRESHOLD = 2500;
        public static readonly string[] CharacterChain = new string[] { "classic", "fire", "ice", "toxic", "shadow" };
        private const string KEY_AVATAR_CHAIN_INIT = "ZiplaKac_AvatarChain_v1";

        public static string CanonicalizeCharacterId(string id)
        {
            if (string.IsNullOrEmpty(id)) return "classic";
            string lower = id.ToLowerInvariant();
            if (lower.Contains("fire") || lower.Contains("ates")) return "fire";
            if (lower.Contains("ice") || lower.Contains("buz")) return "ice";
            if (lower.Contains("toxic") || lower.Contains("zehir") || lower.Contains("poison")) return "toxic";
            if (lower.Contains("shadow") || lower.Contains("gece") || lower.Contains("night")) return "shadow";
            return "classic";
        }

        public static string GetCharacterDisplayName(string id)
        {
            string canonical = CanonicalizeCharacterId(id);
            switch (canonical)
            {
                case "fire": return "Ateş";
                case "ice": return "Buz";
                case "toxic": return "Zehir";
                case "shadow": return "Gece";
                default: return "Klasik";
            }
        }

        public static string GetPreviousCharacterInChain(string id)
        {
            string canonical = CanonicalizeCharacterId(id);
            for (int i = 1; i < CharacterChain.Length; i++)
            {
                if (CharacterChain[i] == canonical)
                {
                    return CharacterChain[i - 1];
                }
            }
            return null;
        }

        public static string GetNextCharacterInChain(string id)
        {
            string canonical = CanonicalizeCharacterId(id);
            for (int i = 0; i < CharacterChain.Length - 1; i++)
            {
                if (CharacterChain[i] == canonical)
                {
                    return CharacterChain[i + 1];
                }
            }
            return null;
        }

        public static string GetUnlockRequirementText(string id)
        {
            string canonical = CanonicalizeCharacterId(id);
            if (canonical == "classic") return "Açık";

            string prev = GetPreviousCharacterInChain(canonical);
            string prevName = GetCharacterDisplayName(prev);
            return $"{prevName} ile {AVATAR_UNLOCK_SCORE_THRESHOLD} skor yap";
        }

        public static int GetCharacterHighScore(string id)
        {
            string canonical = CanonicalizeCharacterId(id);
            return PlayerPrefs.GetInt($"ZiplaKac_CharHighScore_{canonical}", 0);
        }

        public static bool SaveCharacterScore(string id, int score)
        {
            string canonical = CanonicalizeCharacterId(id);
            int currentHigh = GetCharacterHighScore(canonical);
            if (score > currentHigh)
            {
                PlayerPrefs.SetInt($"ZiplaKac_CharHighScore_{canonical}", score);
                PlayerPrefs.Save();
                return true;
            }
            return false;
        }

        /// <summary>
        /// İlk açılışta 4 avatarın kilitli olmasını garanti eder. Sadece Klasik açıktır.
        /// </summary>
        public static void EnsureAvatarChainInitialized()
        {
            if (PlayerPrefs.GetInt(KEY_AVATAR_CHAIN_INIT, 0) == 0)
            {
                PlayerPrefs.SetInt(KEY_AVATAR_CHAIN_INIT, 1);
                PlayerPrefs.SetInt("ZiplaKac_Unlocked_classic", 1);
                PlayerPrefs.SetInt("ZiplaKac_Unlocked_fire", 0);
                PlayerPrefs.SetInt("ZiplaKac_Unlocked_ice", 0);
                PlayerPrefs.SetInt("ZiplaKac_Unlocked_toxic", 0);
                PlayerPrefs.SetInt("ZiplaKac_Unlocked_shadow", 0);
                PlayerPrefs.SetString(KEY_UNLOCKED_CHARACTERS, "classic");

                // Eğer aktif seçili karakter kilitli birinde kalmışsa hemen klasiğe çek
                string currentSelected = GetSelectedCharacterId("classic");
                if (CanonicalizeCharacterId(currentSelected) != "classic")
                {
                    SaveSelectedCharacterId("classic");
                }
                PlayerPrefs.Save();
            }
        }

        public static bool IsCharacterUnlocked(string id)
        {
            EnsureAvatarChainInitialized();
            string canonical = CanonicalizeCharacterId(id);
            if (canonical == "classic") return true;

            // 1. Zaten kalıcı olarak açılmış mı?
            if (PlayerPrefs.GetInt($"ZiplaKac_Unlocked_{canonical}", 0) == 1)
            {
                return true;
            }

            // 2. Bir önceki karakter ile 2500 veya üstü skor yapılmış mı?
            string prev = GetPreviousCharacterInChain(canonical);
            if (!string.IsNullOrEmpty(prev))
            {
                if (GetCharacterHighScore(prev) >= AVATAR_UNLOCK_SCORE_THRESHOLD)
                {
                    UnlockCharacter(canonical);
                    return true;
                }
            }

            return false;
        }

        public static void UnlockCharacter(string id)
        {
            string canonical = CanonicalizeCharacterId(id);
            PlayerPrefs.SetInt($"ZiplaKac_Unlocked_{canonical}", 1);

            string unlocked = PlayerPrefs.GetString(KEY_UNLOCKED_CHARACTERS, "classic");
            if (!unlocked.Contains(canonical))
            {
                unlocked += "," + canonical;
                PlayerPrefs.SetString(KEY_UNLOCKED_CHARACTERS, unlocked);
            }
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Mevcut oynanan karakterle 2500+ skor yapıldığında sıradaki karakterin kilidini açar.
        /// Eğer yeni bir kilit açıldıysa true döner ve açılan karakterin ID'sini verir.
        /// </summary>
        public static bool CheckAndUnlockNextCharacter(string currentCharacterId, int scoreAchieved, out string unlockedCharacterId)
        {
            unlockedCharacterId = null;
            if (scoreAchieved < AVATAR_UNLOCK_SCORE_THRESHOLD) return false;

            string canonical = CanonicalizeCharacterId(currentCharacterId);
            string next = GetNextCharacterInChain(canonical);
            if (!string.IsNullOrEmpty(next) && !IsCharacterUnlocked(next))
            {
                UnlockCharacter(next);
                unlockedCharacterId = next;
                return true;
            }

            return false;
        }
        #endregion
    }
}
