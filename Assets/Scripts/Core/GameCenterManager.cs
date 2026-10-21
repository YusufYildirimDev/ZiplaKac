using System;
using UnityEngine;
using UnityEngine.SocialPlatforms;
#if UNITY_IOS
using UnityEngine.SocialPlatforms.GameCenter;
#endif

namespace ZiplaKac.Core
{
    /// <summary>
    /// Apple Game Center entegrasyon yöneticisi.
    /// Yerel iOS GameKit / Social API kullanarak oyuncu doğrulaması,
    /// küresel lider tablosu (Leaderboard) ve başarımlar (Achievements) sağlar.
    /// </summary>
    public class GameCenterManager : MonoBehaviour
    {
        public static GameCenterManager Instance { get; private set; }

        public const string LEADERBOARD_ID_DEFAULT = "com.yusufyldrm.ziplakac.leaderboard";

        [Header("Yapılandırma")]
        public string leaderboardId = LEADERBOARD_ID_DEFAULT;
        public bool isAuthenticated { get; private set; } = false;

        public event Action<bool> OnAuthenticationChanged;

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
                return;
            }
        }

        private void Start()
        {
            AuthenticateLocalUser();
        }

        /// <summary>
        /// Apple Game Center yerel oyuncusunu sessizce doğrula.
        /// </summary>
        public void AuthenticateLocalUser(Action<bool> onComplete = null)
        {
            try
            {
                Debug.Log("[GameCenter] Oyuncu doğrulanıyor...");
                Social.localUser.Authenticate((bool success) => {
                    isAuthenticated = success;
                    if (success)
                    {
                        Debug.Log($"[GameCenter] Başarıyla bağlandı: {Social.localUser.userName} (ID: {Social.localUser.id})");
                    }
                    else
                    {
                        Debug.LogWarning("[GameCenter] Kimlik doğrulaması başarısız veya kullanıcı iptal etti.");
                    }
                    OnAuthenticationChanged?.Invoke(success);
                    onComplete?.Invoke(success);
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GameCenter] Doğrulama hatası: {ex.Message}");
                isAuthenticated = false;
                onComplete?.Invoke(false);
            }
        }

        /// <summary>
        /// Yüksek skoru Game Center lider tablosuna bildir.
        /// </summary>
        public void ReportScore(long score, string customLeaderboardId = null)
        {
            string targetId = string.IsNullOrEmpty(customLeaderboardId) ? leaderboardId : customLeaderboardId;

            if (!isAuthenticated)
            {
                AuthenticateLocalUser((success) => {
                    if (success)
                    {
                        SendScore(score, targetId);
                    }
                });
                return;
            }

            SendScore(score, targetId);
        }

        private void SendScore(long score, string targetLeaderboardId)
        {
            try
            {
                Social.ReportScore(score, targetLeaderboardId, (bool success) => {
                    if (success)
                    {
                        Debug.Log($"[GameCenter] Skor başarıyla gönderildi: {score} -> {targetLeaderboardId}");
                    }
                    else
                    {
                        Debug.LogWarning($"[GameCenter] Skor gönderilemedi: {score}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GameCenter] ReportScore istisnası: {ex.Message}");
            }
        }

        /// <summary>
        /// iOS yerel Game Center Lider Tablosu arayüzünü açar.
        /// </summary>
        public void ShowLeaderboardUI(Action onNotAvailable = null)
        {
            if (!isAuthenticated)
            {
                AuthenticateLocalUser((success) => {
                    if (success)
                    {
                        Social.ShowLeaderboardUI();
                    }
                    else
                    {
                        onNotAvailable?.Invoke();
                    }
                });
                return;
            }

            try
            {
                Social.ShowLeaderboardUI();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GameCenter] ShowLeaderboardUI istisnası: {ex.Message}");
                onNotAvailable?.Invoke();
            }
        }

        /// <summary>
        /// Başarım ilerlemesini bildir (0 - 100 arası).
        /// </summary>
        public void ReportAchievement(string achievementId, double progress = 100.0)
        {
            if (string.IsNullOrEmpty(achievementId)) return;

            if (!isAuthenticated)
            {
                AuthenticateLocalUser((success) => {
                    if (success) SendAchievement(achievementId, progress);
                });
                return;
            }

            SendAchievement(achievementId, progress);
        }

        private void SendAchievement(string achievementId, double progress)
        {
            try
            {
                Social.ReportProgress(achievementId, progress, (bool success) => {
                    if (success)
                    {
                        Debug.Log($"[GameCenter] Başarım bildirildi: {achievementId} (%{progress:F0})");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GameCenter] Başarım bildirimi hatası: {ex.Message}");
            }
        }
    }
}
