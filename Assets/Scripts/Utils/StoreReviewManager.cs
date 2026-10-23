using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ZiplaKac.Utils
{
    /// <summary>
    /// App Store (SKStoreReviewController) ve Google Play için oyun içi değerlendirme yöneticisi.
    /// </summary>
    public class StoreReviewManager : MonoBehaviour
    {
        public static StoreReviewManager Instance { get; private set; }

        private const string PREF_KEY_LAST_REVIEW_TIME = "hoppzy_last_review_prompt_time";
        private const string PREF_KEY_GAME_COUNT_FOR_REVIEW = "hoppzy_games_played_for_review";
        private const int MIN_DAYS_BETWEEN_PROMPTS = 25; // Apple yılda maks 3 kez izin verir

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void _RequestStoreReview();
#endif

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Game Over veya rekor kırıldığında uygunsa değerlendirme penceresini tetikler.
        /// </summary>
        public void CheckAndPromptReview(int score, bool isNewHighScore)
        {
            int gamesCount = PlayerPrefs.GetInt(PREF_KEY_GAME_COUNT_FOR_REVIEW, 0) + 1;
            PlayerPrefs.SetInt(PREF_KEY_GAME_COUNT_FOR_REVIEW, gamesCount);
            PlayerPrefs.Save();

            // Koşul: Yeni rekor (skor >= 40) veya her 6 oyunda bir
            bool conditionMet = (isNewHighScore && score >= 40) || (gamesCount % 6 == 0);
            if (!conditionMet) return;

            string lastPromptStr = PlayerPrefs.GetString(PREF_KEY_LAST_REVIEW_TIME, string.Empty);
            if (!string.IsNullOrEmpty(lastPromptStr))
            {
                if (DateTime.TryParse(lastPromptStr, out DateTime lastPrompt))
                {
                    if ((DateTime.UtcNow - lastPrompt).TotalDays < MIN_DAYS_BETWEEN_PROMPTS)
                    {
                        return; // Henüz yeterli süre geçmedi
                    }
                }
            }

            RequestReview();
        }

        /// <summary>
        /// Doğrudan mağaza puanlama akışını açar.
        /// </summary>
        public void RequestReview()
        {
            PlayerPrefs.SetString(PREF_KEY_LAST_REVIEW_TIME, DateTime.UtcNow.ToString("O"));
            PlayerPrefs.Save();

            Debug.Log("[StoreReviewManager] Mağaza değerlendirme isteği gönderiliyor...");

#if UNITY_IOS && !UNITY_EDITOR
            try
            {
                _RequestStoreReview();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[StoreReviewManager] iOS Review Hatası: {ex.Message}");
            }
#elif UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                Application.OpenURL("market://details?id=com.yusufyldrm.ziplakac");
            }
            catch
            {
                Application.OpenURL("https://play.google.com/store/apps/details?id=com.yusufyldrm.ziplakac");
            }
#else
            Debug.Log("[StoreReviewManager] Unity Editör: Değerlendirme simüle edildi.");
#endif
        }
    }
}
