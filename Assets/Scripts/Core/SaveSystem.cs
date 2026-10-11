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
    }
}
