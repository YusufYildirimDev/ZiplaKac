using System;
using UnityEngine;

namespace ZiplaKac.Core
{
    /// <summary>
    /// Google AdMob ve Unity IAP entegrasyonu için ticari altyapı iskeleti (Monetization Manager Stub).
    /// Gerçek SDK'lar projeye eklendiğinde bu sınıftaki stub fonksiyonlar SDK çağrılarıyla değiştirilebilir.
    /// </summary>
    public class MonetizationManager : MonoBehaviour
    {
        public static MonetizationManager Instance { get; private set; }

        [Header("Reklam Yapılandırma Stupları")]
        public bool isAdMobInitialized = false;
        public bool isTestMode = true;

        [Header("IAP Yapılandırma Stupları")]
        public bool isIAPInitialized = false;

        // Eventler
        public event Action OnRewardedAdCompleted;
        public event Action OnRewardedAdFailed;
        public event Action<string> OnPurchaseSuccess;

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

            InitializeMonetization();
        }

        private void Start()
        {
            // Apple App Store kuralı (iOS 14.5+): Açılışta kullanıcıdan takip/kişiselleştirilmiş reklam izni istenir
            Utils.IOSATTBridge.RequestTrackingAuthorization(status =>
            {
                Debug.Log($"[MonetizationManager] iOS ATT İzin Durumu: {status}");
            });
        }

        private void InitializeMonetization()
        {
            // SDK Başlatma taklidi
            Debug.Log("[MonetizationManager] AdMob ve IAP modülleri hazırlandı (Stub Mode).");
            isAdMobInitialized = true;
            isIAPInitialized = true;
        }

        #region Reklam Yönetimi (Ads)
        /// <summary>
        /// Geçiş reklamı gösterimi (Oyun bitti ekranlarında 3 oyunda bir tetiklenebilir).
        /// </summary>
        public void ShowInterstitialAd()
        {
            Debug.Log("[MonetizationManager] ShowInterstitialAd çağrıldı.");
            // Gerçek SDK Entegrasyonu: GoogleMobileAds.Api.InterstitialAd.Show();
        }

        /// <summary>
        /// Ödüllü reklam gösterimi (Oyun bitti ekranında 'Reklam İzle & Devam Et' veya karakter kilidi açma için).
        /// </summary>
        public void ShowRewardedAd(Action onSuccess, Action onFailure)
        {
            Debug.Log("[MonetizationManager] ShowRewardedAd simüle ediliyor...");
            
            // Prototip simülasyonu: Doğrudan başarı döner
            onSuccess?.Invoke();
            OnRewardedAdCompleted?.Invoke();

            /* Gerçek SDK Mantığı:
            if (rewardedAd != null && rewardedAd.CanShowAd())
            {
                rewardedAd.Show((Reward reward) => {
                    onSuccess?.Invoke();
                });
            }
            else
            {
                onFailure?.Invoke();
            }
            */
        }
        #endregion

        [Header("AdMob iOS Yapılandırması (Canlı & Test ID)")]
        public string admobAppId = "ca-app-pub-3940256099942544~145869519";
        public string bannerAdUnitId = "ca-app-pub-3940256099942544/2934735716";
        public string interstitialAdUnitId = "ca-app-pub-3940256099942544/4411468910";
        public string rewardedAdUnitId = "ca-app-pub-3940256099942544/1712485313";

        public bool IsNoAdsActive => PlayerPrefs.GetInt("no_ads", 0) == 1;

        #region Uygulama İçi Satın Alma (IAP)
        /// <summary>
        /// Altın paketi, başlangıç paketi veya reklamsız paket satın alma işlemi.
        /// </summary>
        public void BuyProduct(string productId, Action<bool> onComplete)
        {
            Debug.Log($"[MonetizationManager] BuyProduct çağrıldı: {productId}");

            bool success = true;

            switch (productId)
            {
                case "iap_gold_1000":
                    SaveSystem.AddGold(1000);
                    break;

                case "iap_gold_5000":
                    SaveSystem.AddGold(5000);
                    break;

                case "iap_pack_starter":
                    SaveSystem.AddGold(10000);
                    SaveSystem.AddConsumable("second_chance", 5);
                    break;

                case "iap_no_ads":
                    PlayerPrefs.SetInt("no_ads", 1);
                    PlayerPrefs.Save();
                    SaveSystem.BackupToKeychain();
                    break;

                default:
                    success = false;
                    break;
            }

            onComplete?.Invoke(success);
            if (success)
            {
                OnPurchaseSuccess?.Invoke(productId);
            }
        }

        /// <summary>
        /// Apple App Store Kuralı (Guideline 3.1.1):
        /// Kullanıcının daha önce satın aldığı tek seferlik (non-consumable) ürünleri (ör. iap_no_ads)
        /// Keychain ve Store altyapısı üzerinden geri yükler.
        /// </summary>
        public void RestorePurchases(Action<bool, string> onComplete)
        {
            Debug.Log("[MonetizationManager] RestorePurchases çağrıldı.");

            // 1. Keychain ve yerel verileri tazele
            SaveSystem.InitializePersistence();

            bool restored = false;
            if (PlayerPrefs.GetInt("no_ads", 0) == 1)
            {
                restored = true;
            }
            else
            {
                string kcJson = Utils.IOSKeychainBridge.GetString("hoppzy_persistent_save", "");
                if (!string.IsNullOrEmpty(kcJson))
                {
                    try
                    {
                        var data = JsonUtility.FromJson<PersistentSaveData>(kcJson);
                        if (data != null && data.noAds == 1)
                        {
                            PlayerPrefs.SetInt("no_ads", 1);
                            PlayerPrefs.Save();
                            restored = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[MonetizationManager] Keychain parse hatası: {ex.Message}");
                    }
                }
            }

            if (restored)
            {
                SaveSystem.BackupToKeychain();
                string msg = LocalizationManager.Get("shop_restore_success", "Satın alımlarınız başarıyla geri yüklendi!");
                onComplete?.Invoke(true, msg);
            }
            else
            {
                string msg = LocalizationManager.Get("shop_restore_none", "Geri yüklenecek satın alım bulunamadı.");
                onComplete?.Invoke(false, msg);
            }
        }
        #endregion
    }
}
