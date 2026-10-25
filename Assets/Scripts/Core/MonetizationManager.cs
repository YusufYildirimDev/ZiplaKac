using System;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;

namespace ZiplaKac.Core
{
    /// <summary>
    /// Google AdMob ve Unity In-App Purchasing (IAP - StoreKit & Google Play Billing)
    /// resmi mağaza entegrasyonlarını yöneten merkezi sınıf.
    /// </summary>
    public class MonetizationManager : MonoBehaviour, IDetailedStoreListener
    {
        public static MonetizationManager Instance { get; private set; }

        [Header("AdMob Kimlikleri (Test & Canlı)")]
        [Tooltip("Test modunda Google'ın resmi test birim kimlikleri kullanılır. Canlı yayında false yapın.")]
        public bool isTestMode = true;

#if UNITY_ANDROID
        public string appId = "ca-app-pub-3940256099942544~3347511713";
        public string interstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";
        public string rewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";
#elif UNITY_IOS
        public string appId = "ca-app-pub-3940256099942544~1458002511";
        public string interstitialAdUnitId = "ca-app-pub-3940256099942544/4411468910";
        public string rewardedAdUnitId = "ca-app-pub-3940256099942544/1712485313";
#else
        public string appId = "ca-app-pub-3940256099942544~3347511713";
        public string interstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";
        public string rewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";
#endif

        [Header("Geçiş Reklamı Sıklığı")]
        [Tooltip("Kaç oyun bittiğinde (GameOver) bir geçiş reklamı gösterilsin.")]
        public int interstitialIntervalGames = 5;
        private const string PREF_KEY_GAME_OVER_COUNT = "hoppzy_game_over_ad_count";

        [Header("IAP Ürün Kimlikleri")]
        public const string PRODUCT_GOLD_1000 = "iap_gold_1000";
        public const string PRODUCT_GOLD_5000 = "iap_gold_5000";
        public const string PRODUCT_PACK_STARTER = "iap_pack_starter";
        public const string PRODUCT_NO_ADS = "iap_no_ads";

        // Unity IAP Dahili Değişkenleri
        private IStoreController m_StoreController;
        private IExtensionProvider m_StoreExtensionProvider;
        private IAppleExtensions m_AppleExtensions;
        private Action<bool> m_CurrentPurchaseCallback;

        // AdMob Dahili Değişkenleri
        private InterstitialAd m_InterstitialAd;
        private RewardedAd m_RewardedAd;
        private bool isAdMobInitialized = false;

        // Eventler
        public event Action OnRewardedAdCompleted;
        public event Action OnRewardedAdFailed;
        public event Action<string> OnPurchaseSuccess;

        public bool IsIAPInitialized => m_StoreController != null && m_StoreExtensionProvider != null;
        public bool IsNoAdsActive => PlayerPrefs.GetInt("no_ads", 0) == 1;

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

            InitializeIAP();
            InitializeAdMob();
        }

        private void Start()
        {
            // iOS 14.5+ ATT (App Tracking Transparency) İzni
            Utils.IOSATTBridge.RequestTrackingAuthorization(status =>
            {
                Debug.Log($"[MonetizationManager] iOS ATT Durumu: {status}");
                RequestConsentAndLoadAds();
            });
        }

        #region Unity In-App Purchasing (IAP) Entegrasyonu
        private void InitializeIAP()
        {
            if (IsIAPInitialized) return;

            Debug.Log("[MonetizationManager] Unity IAP başlatılıyor...");

            try
            {
                var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

                // Ürün Kataloğu
                builder.AddProduct(PRODUCT_GOLD_1000, ProductType.Consumable);
                builder.AddProduct(PRODUCT_GOLD_5000, ProductType.Consumable);
                builder.AddProduct(PRODUCT_PACK_STARTER, ProductType.Consumable);
                builder.AddProduct(PRODUCT_NO_ADS, ProductType.NonConsumable);

                UnityPurchasing.Initialize(this, builder);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MonetizationManager] IAP başlatılırken hata: {ex.Message}");
            }
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            Debug.Log("[MonetizationManager] Unity IAP başarıyla başlatıldı!");
            m_StoreController = controller;
            m_StoreExtensionProvider = extensions;
            m_AppleExtensions = extensions.GetExtension<IAppleExtensions>();

            // Önceden satın alınmış Non-Consumable (Reklamları Kaldır) kontrolü
            Product noAdsProduct = m_StoreController.products.WithID(PRODUCT_NO_ADS);
            if (noAdsProduct != null && noAdsProduct.hasReceipt)
            {
                Debug.Log("[MonetizationManager] Daha önce 'Reklamları Kaldır' satın alınmış, aktif ediliyor.");
                PlayerPrefs.SetInt("no_ads", 1);
                PlayerPrefs.Save();
                SaveSystem.BackupToKeychain();
            }
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogError($"[MonetizationManager] IAP Başlatılamadı: {error}");
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogError($"[MonetizationManager] IAP Başlatılamadı: {error} - {message}");
        }

        /// <summary>
        /// Mağazadan ürün satın alımını başlatır (Apple StoreKit / Google Play Billing).
        /// </summary>
        public void BuyProduct(string productId, Action<bool> onComplete)
        {
            Debug.Log($"[MonetizationManager] BuyProduct çağrıldı: {productId}");
            m_CurrentPurchaseCallback = onComplete;

            if (IsIAPInitialized)
            {
                Product product = m_StoreController.products.WithID(productId);
                if (product != null && product.availableToPurchase)
                {
                    Debug.Log($"[MonetizationManager] Mağaza ödeme ekranı açılıyor: {product.definition.id}");
                    m_StoreController.InitiatePurchase(product);
                    return;
                }
                else
                {
                    Debug.LogWarning($"[MonetizationManager] Ürün bulunamadı veya satın almaya uygun değil: {productId}");
                }
            }

            // Mağaza bağlantısı henüz kurulamadıysa (Editör veya test amaçlı)
#if UNITY_EDITOR
            Debug.Log("[MonetizationManager] Editör modunda IAP simülasyonu yapılıyor...");
            GrantProductReward(productId);
            onComplete?.Invoke(true);
            OnPurchaseSuccess?.Invoke(productId);
#else
            onComplete?.Invoke(false);
#endif
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            string purchasedId = args.purchasedProduct.definition.id;
            Debug.Log($"[MonetizationManager] Satın alma başarılı: {purchasedId}");

            GrantProductReward(purchasedId);

            if (m_CurrentPurchaseCallback != null)
            {
                m_CurrentPurchaseCallback.Invoke(true);
                m_CurrentPurchaseCallback = null;
            }

            OnPurchaseSuccess?.Invoke(purchasedId);

            return PurchaseProcessingResult.Complete;
        }

        private void GrantProductReward(string productId)
        {
            switch (productId)
            {
                case PRODUCT_GOLD_1000:
                    SaveSystem.AddGold(1000);
                    break;

                case PRODUCT_GOLD_5000:
                    SaveSystem.AddGold(5000);
                    break;

                case PRODUCT_PACK_STARTER:
                    SaveSystem.AddGold(10000);
                    SaveSystem.AddConsumable("second_chance", 5);
                    break;

                case PRODUCT_NO_ADS:
                    PlayerPrefs.SetInt("no_ads", 1);
                    PlayerPrefs.Save();
                    SaveSystem.BackupToKeychain();
                    break;
            }
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Debug.LogWarning($"[MonetizationManager] Satın alma başarısız: {product.definition.id}, Neden: {failureReason}");
            if (m_CurrentPurchaseCallback != null)
            {
                m_CurrentPurchaseCallback.Invoke(false);
                m_CurrentPurchaseCallback = null;
            }
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            Debug.LogWarning($"[MonetizationManager] Satın alma detaylı hata: {product.definition.id}, Detay: {failureDescription.message}");
            if (m_CurrentPurchaseCallback != null)
            {
                m_CurrentPurchaseCallback.Invoke(false);
                m_CurrentPurchaseCallback = null;
            }
        }

        /// <summary>
        /// Apple App Store Kuralı (Guideline 3.1.1):
        /// Kullanıcının satın aldığı non-consumable ürünleri StoreKit / Play Billing üzerinden geri yükler.
        /// </summary>
        public void RestorePurchases(Action<bool, string> onComplete)
        {
            Debug.Log("[MonetizationManager] RestorePurchases çağrıldı.");

            // 1. Önce yerel Keychain verilerini tazele
            SaveSystem.InitializePersistence();

#if UNITY_IOS
            if (m_AppleExtensions != null)
            {
                m_AppleExtensions.RestoreTransactions((result, error) =>
                {
                    Debug.Log($"[MonetizationManager] Apple RestoreTransactions sonucu: {result}, Hata: {error}");
                    bool restored = IsNoAdsActive;
                    if (restored)
                    {
                        SaveSystem.BackupToKeychain();
                        onComplete?.Invoke(true, LocalizationManager.Get("shop_restore_success", "Satın alımlarınız başarıyla geri yüklendi!"));
                    }
                    else
                    {
                        onComplete?.Invoke(false, LocalizationManager.Get("shop_restore_none", "Geri yüklenecek geçerli bir satın alım bulunamadı."));
                    }
                });
                return;
            }
#endif

            // Android veya AppleExtensions hazır değilse StoreController makbuzlarını tara
            bool foundOwned = false;
            if (m_StoreController != null)
            {
                Product noAds = m_StoreController.products.WithID(PRODUCT_NO_ADS);
                if (noAds != null && noAds.hasReceipt)
                {
                    PlayerPrefs.SetInt("no_ads", 1);
                    PlayerPrefs.Save();
                    SaveSystem.BackupToKeychain();
                    foundOwned = true;
                }
            }

            if (foundOwned || IsNoAdsActive)
            {
                onComplete?.Invoke(true, LocalizationManager.Get("shop_restore_success", "Satın alımlarınız başarıyla geri yüklendi!"));
            }
            else
            {
                onComplete?.Invoke(false, LocalizationManager.Get("shop_restore_none", "Geri yüklenecek geçerli bir satın alım bulunamadı."));
            }
        }
        #endregion

        #region Google AdMob & UMP (GDPR) Entegrasyonu
        private void InitializeAdMob()
        {
            Debug.Log("[MonetizationManager] Google Mobile Ads (AdMob) başlatılıyor...");

            try
            {
                MobileAds.Initialize(initStatus =>
                {
                    Debug.Log("[MonetizationManager] Google Mobile Ads başlatıldı.");
                    isAdMobInitialized = true;
                    LoadInterstitialAd();
                    LoadRewardedAd();
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MonetizationManager] AdMob başlatılırken istisna: {ex.Message}");
            }
        }

        private void RequestConsentAndLoadAds()
        {
            try
            {
                var request = new ConsentRequestParameters();
                ConsentInformation.Update(request, consentError =>
                {
                    if (consentError != null)
                    {
                        Debug.LogWarning($"[MonetizationManager] UMP Consent hatası: {consentError.Message}");
                        return;
                    }

                    ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                    {
                        if (formError != null)
                        {
                            Debug.LogWarning($"[MonetizationManager] UMP Form gösterim hatası: {formError.Message}");
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MonetizationManager] UMP Consent kontrolü istisnası: {ex.Message}");
            }
        }

        private void LoadInterstitialAd()
        {
            if (IsNoAdsActive) return;

            // Önceki reklamı temizle
            if (m_InterstitialAd != null)
            {
                m_InterstitialAd.Destroy();
                m_InterstitialAd = null;
            }

            var adRequest = new AdRequest();
            InterstitialAd.Load(interstitialAdUnitId, adRequest, (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogWarning($"[MonetizationManager] Geçiş reklamı yüklenemedi: {error}");
                    return;
                }

                m_InterstitialAd = ad;
                Debug.Log("[MonetizationManager] Geçiş reklamı başarıyla yüklendi.");
                RegisterInterstitialEvents(m_InterstitialAd);
            });
        }

        private void RegisterInterstitialEvents(InterstitialAd ad)
        {
            ad.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("[MonetizationManager] Geçiş reklamı kapatıldı, yenisi yükleniyor.");
                LoadInterstitialAd();
            };

            ad.OnAdFullScreenContentFailed += (err) =>
            {
                Debug.LogWarning($"[MonetizationManager] Geçiş reklamı gösterim hatası: {err}");
                LoadInterstitialAd();
            };
        }

        /// <summary>
        /// GameOver olduğunda çağrılır; her N oyunda bir geçiş reklamı gösterir (Reklamsız paket yoksa).
        /// </summary>
        public void CheckAndShowInterstitialOnGameOver()
        {
            if (IsNoAdsActive) return;

            int currentCount = PlayerPrefs.GetInt(PREF_KEY_GAME_OVER_COUNT, 0) + 1;
            Debug.Log($"[MonetizationManager] GameOver Sayacı: {currentCount}/{interstitialIntervalGames}");

            if (currentCount >= interstitialIntervalGames)
            {
                PlayerPrefs.SetInt(PREF_KEY_GAME_OVER_COUNT, 0);
                PlayerPrefs.Save();
                ShowInterstitialAd();
            }
            else
            {
                PlayerPrefs.SetInt(PREF_KEY_GAME_OVER_COUNT, currentCount);
                PlayerPrefs.Save();
            }
        }

        public void ShowInterstitialAd()
        {
            if (IsNoAdsActive)
            {
                Debug.Log("[MonetizationManager] Reklamsız mod aktif, geçiş reklamı gösterilmedi.");
                return;
            }

            if (m_InterstitialAd != null && m_InterstitialAd.CanShowAd())
            {
                Debug.Log("[MonetizationManager] Geçiş reklamı gösteriliyor...");
                m_InterstitialAd.Show();
            }
            else
            {
                Debug.Log("[MonetizationManager] Geçiş reklamı hazır değil, yeniden yükleniyor.");
                LoadInterstitialAd();
#if UNITY_EDITOR
                Debug.Log("[MonetizationManager] [Editor Simülasyonu] 5 oyunda bir otomatik geçiş reklamı (Interstitial Ad) tetiklendi!");
#endif
            }
        }

        private void LoadRewardedAd()
        {
            if (m_RewardedAd != null)
            {
                m_RewardedAd.Destroy();
                m_RewardedAd = null;
            }

            var adRequest = new AdRequest();
            RewardedAd.Load(rewardedAdUnitId, adRequest, (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogWarning($"[MonetizationManager] Ödüllü reklam yüklenemedi: {error}");
                    return;
                }

                m_RewardedAd = ad;
                Debug.Log("[MonetizationManager] Ödüllü reklam başarıyla yüklendi.");
                RegisterRewardedEvents(m_RewardedAd);
            });
        }

        private void RegisterRewardedEvents(RewardedAd ad)
        {
            ad.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("[MonetizationManager] Ödüllü reklam kapatıldı, yenisi yükleniyor.");
                LoadRewardedAd();
            };

            ad.OnAdFullScreenContentFailed += (err) =>
            {
                Debug.LogWarning($"[MonetizationManager] Ödüllü reklam gösterim hatası: {err}");
                LoadRewardedAd();
            };
        }

        /// <summary>
        /// Ödüllü reklam gösterimi (Oyun bitti ekranında can veya ana menüde ödül için).
        /// </summary>
        public void ShowRewardedAd(Action onSuccess, Action onFailure)
        {
            if (m_RewardedAd != null && m_RewardedAd.CanShowAd())
            {
                Debug.Log("[MonetizationManager] Ödüllü reklam gösteriliyor...");
                m_RewardedAd.Show(reward =>
                {
                    Debug.Log($"[MonetizationManager] Kullanıcı ödülü kazandı: {reward.Type} - {reward.Amount}");
                    onSuccess?.Invoke();
                    OnRewardedAdCompleted?.Invoke();
                });
            }
            else
            {
                Debug.LogWarning("[MonetizationManager] Ödüllü reklam hazır değil!");
#if UNITY_EDITOR
                Debug.Log("[MonetizationManager] Editör modunda ödüllü reklam simülasyonu çalıştırılıyor...");
                onSuccess?.Invoke();
                OnRewardedAdCompleted?.Invoke();
#else
                onFailure?.Invoke();
                OnRewardedAdFailed?.Invoke();
                LoadRewardedAd();
#endif
            }
        }
        #endregion
    }
}
