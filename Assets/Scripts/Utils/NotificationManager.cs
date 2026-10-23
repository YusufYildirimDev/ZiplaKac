using System;
using System.Runtime.InteropServices;
using UnityEngine;
using ZiplaKac.Core;

namespace ZiplaKac.Utils
{
    /// <summary>
    /// Oyuncuyu 24 saat sonra günlük ödül ve yeni rekor için çağıran yerel bildirim yöneticisi.
    /// </summary>
    public class NotificationManager : MonoBehaviour
    {
        public static NotificationManager Instance { get; private set; }

        private const string NOTIFICATION_ID_DAILY_REWARD = "hoppzy_daily_reward_notif";
        private const int DELAY_24_HOURS_SECONDS = 86400;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void _RequestNotificationPermission();

        [DllImport("__Internal")]
        private static extern void _ScheduleLocalNotification(string title, string message, int delaySeconds, string identifier);

        [DllImport("__Internal")]
        private static extern void _CancelAllNotifications();
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

        private void Start()
        {
            RequestPermission();
            CancelPendingNotifications();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // Uygulama arka plana geçtiğinde 24 saat sonrası için bildirim planla
                ScheduleComebackNotification();
            }
            else
            {
                // Uygulama ön plana geldiğinde bekleyen bildirimleri temizle
                CancelPendingNotifications();
            }
        }

        private void OnApplicationQuit()
        {
            ScheduleComebackNotification();
        }

        public void RequestPermission()
        {
#if UNITY_IOS && !UNITY_EDITOR
            try
            {
                _RequestNotificationPermission();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NotificationManager] Bildirim izni hatası: {ex.Message}");
            }
#endif
        }

        public void CancelPendingNotifications()
        {
#if UNITY_IOS && !UNITY_EDITOR
            try
            {
                _CancelAllNotifications();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NotificationManager] Bildirim iptal hatası: {ex.Message}");
            }
#endif
        }

        public void ScheduleComebackNotification()
        {
            string title = LocalizationManager.Get("notif_reward_title", "🎁 Günlük Ödülün Hazır!");
            string message = LocalizationManager.Get("notif_reward_msg", "Altınların seni bekliyor! Hemen gel ve ödülünü topla!");

#if UNITY_IOS && !UNITY_EDITOR
            try
            {
                _ScheduleLocalNotification(title, message, DELAY_24_HOURS_SECONDS, NOTIFICATION_ID_DAILY_REWARD);
                Debug.Log($"[NotificationManager] 24 saat sonrası için yerel bildirim planlandı: {title}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NotificationManager] Bildirim kurma hatası: {ex.Message}");
            }
#else
            Debug.Log($"[NotificationManager] Editör: 24 saatlik bildirim simüle edildi: '{title}' - '{message}'");
#endif
        }
    }
}
