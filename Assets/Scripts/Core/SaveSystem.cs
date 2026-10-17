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
        }

        public static bool UseConsumable(string consumableId)
        {
            int current = GetConsumableCount(consumableId);
            if (current > 0)
            {
                PlayerPrefs.SetInt(KEY_CONSUMABLE_PREFIX + consumableId, current - 1);
                PlayerPrefs.Save();
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

        public static bool IsCharacterUnlocked(string id)
        {
            if (id == "classic") return true;
            string unlocked = PlayerPrefs.GetString(KEY_UNLOCKED_CHARACTERS, "classic");
            return unlocked.Contains(id);
        }

        public static void UnlockCharacter(string id)
        {
            string unlocked = PlayerPrefs.GetString(KEY_UNLOCKED_CHARACTERS, "classic");
            if (!unlocked.Contains(id))
            {
                unlocked += "," + id;
                PlayerPrefs.SetString(KEY_UNLOCKED_CHARACTERS, unlocked);
                PlayerPrefs.Save();
            }
        }
    }
}
