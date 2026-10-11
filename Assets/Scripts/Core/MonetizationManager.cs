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

        #region Uygulama İçi Satın Alma (IAP)
        /// <summary>
        /// Karakter kilidi açma veya reklamsız paket satın alma işlemi.
        /// </summary>
        public void BuyProduct(string productId, Action<bool> onComplete)
        {
            Debug.Log($"[MonetizationManager] BuyProduct çağrıldı: {productId}");

            // Prototip simülasyonu: Satın almayı başarılı kabul eder
            onComplete?.Invoke(true);
            OnPurchaseSuccess?.Invoke(productId);

            /* Gerçek SDK Mantığı:
            storeController.InitiatePurchase(productId);
            */
        }
        #endregion
    }
}
