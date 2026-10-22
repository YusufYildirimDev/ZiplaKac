using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ZiplaKac.Utils
{
    public enum ATTStatus
    {
        NotDetermined = 0,
        Restricted = 1,
        Denied = 2,
        Authorized = 3
    }

    /// <summary>
    /// iOS 14.5+ App Tracking Transparency (ATT) köprüsü.
    /// Kullanıcıdan reklam takibi izni ister ve sonucu döndürür.
    /// </summary>
    public static class IOSATTBridge
    {
        private static Action<ATTStatus> pendingCallback;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int _GetTrackingAuthorizationStatus();

        private delegate void InternalATTCallback(int status);

        [DllImport("__Internal")]
        private static extern void _RequestTrackingAuthorization(InternalATTCallback callback);

        [AOT.MonoPInvokeCallback(typeof(InternalATTCallback))]
        private static void OnATTStatusReceived(int status)
        {
            ATTStatus attStatus = (ATTStatus)status;
            Debug.Log($"[IOSATTBridge] ATT Durumu Geldi: {attStatus}");
            pendingCallback?.Invoke(attStatus);
            pendingCallback = null;
        }
#endif

        public static ATTStatus GetCurrentStatus()
        {
#if UNITY_IOS && !UNITY_EDITOR
            try
            {
                return (ATTStatus)_GetTrackingAuthorizationStatus();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[IOSATTBridge] Status okunamadı: {ex.Message}");
                return ATTStatus.Authorized;
            }
#else
            return ATTStatus.Authorized;
#endif
        }

        public static void RequestTrackingAuthorization(Action<ATTStatus> callback = null)
        {
            pendingCallback = callback;

#if UNITY_IOS && !UNITY_EDITOR
            try
            {
                ATTStatus current = GetCurrentStatus();
                if (current == ATTStatus.NotDetermined)
                {
                    Debug.Log("[IOSATTBridge] ATT izin dialogu çağrılıyor...");
                    _RequestTrackingAuthorization(OnATTStatusReceived);
                }
                else
                {
                    Debug.Log($"[IOSATTBridge] ATT zaten belirlenmiş: {current}");
                    callback?.Invoke(current);
                    pendingCallback = null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IOSATTBridge] ATT çağrı hatası: {ex.Message}");
                callback?.Invoke(ATTStatus.Authorized);
                pendingCallback = null;
            }
#else
            Debug.Log("[IOSATTBridge] iOS dışı veya Editor ortamı: ATT varsayılan Authorized kabul edildi.");
            callback?.Invoke(ATTStatus.Authorized);
            pendingCallback = null;
#endif
        }
    }
}
