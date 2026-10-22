using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ZiplaKac.Utils
{
    /// <summary>
    /// iOS Keychain (Anahtarlık) köprüsü.
    /// Uygulama silinse dahi cihaz hafızasından silinmeyen güvenli kalıcı depolama sağlar.
    /// </summary>
    public static class IOSKeychainBridge
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void _KeychainSetString(string key, string value);

        [DllImport("__Internal")]
        private static extern string _KeychainGetString(string key);

        [DllImport("__Internal")]
        private static extern void _KeychainDelete(string key);
#endif

        public static void SaveString(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) return;

#if UNITY_IOS && !UNITY_EDITOR
            try
            {
                _KeychainSetString(key, value);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[IOSKeychainBridge] SaveString hatası: {ex.Message}");
            }
#else
            // Editor / Android ortamında yerel simülasyon
            PlayerPrefs.SetString("KC_SIM_" + key, value);
            PlayerPrefs.Save();
#endif
        }

        public static string GetString(string key, string defaultValue = "")
        {
            if (string.IsNullOrEmpty(key)) return defaultValue;

#if UNITY_IOS && !UNITY_EDITOR
            try
            {
                string val = _KeychainGetString(key);
                return string.IsNullOrEmpty(val) ? defaultValue : val;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[IOSKeychainBridge] GetString hatası: {ex.Message}");
                return defaultValue;
            }
#else
            return PlayerPrefs.GetString("KC_SIM_" + key, defaultValue);
#endif
        }

        public static void DeleteKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

#if UNITY_IOS && !UNITY_EDITOR
            try
            {
                _KeychainDelete(key);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[IOSKeychainBridge] DeleteKey hatası: {ex.Message}");
            }
#else
            PlayerPrefs.DeleteKey("KC_SIM_" + key);
#endif
        }
    }
}
