using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ZiplaKac.Core;
using ZiplaKac.Utils;

namespace ZiplaKac.UI
{
    /// <summary>
    /// Spesifikasyonda belirtilen Ödül Sandığı (Treasure Chest) ve Günlük Giriş Ödülü modalı.
    /// Sandık görselini görüntüler, açılma animasyonu oynatır ve bonus puan kazandırır.
    /// </summary>
    public class DailyRewardModalUI : MonoBehaviour
    {
        public static DailyRewardModalUI Instance { get; private set; }

        public Image chestImage;
        public Text titleText;
        public Text descriptionText;
        public Text statusText;
        public Button claimButton;
        public Text claimButtonText;
        public Button closeButton;

        private bool hasClaimed = false;

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            RefreshUI();
            StartCoroutine(AnimateChestBreathing());
        }

        public void RefreshUI()
        {
            bool canClaim = SaveSystem.CanClaimDailyReward();
            hasClaimed = !canClaim;

            if (canClaim)
            {
                if (statusText != null) statusText.text = "🎁 Bugünün sandığı açılmaya hazır!";
                if (claimButtonText != null) claimButtonText.text = "SANDIĞI AÇ";
                if (claimButton != null) claimButton.interactable = true;
            }
            else
            {
                if (statusText != null) statusText.text = "✅ Bugünkü hediyeni aldın! Yarın tekrar gel.";
                if (claimButtonText != null) claimButtonText.text = "ALINDI";
                if (claimButton != null) claimButton.interactable = false;
            }
        }

        public void OnClaimButtonClicked()
        {
            if (hasClaimed)
            {
                CloseModal();
                return;
            }

            hasClaimed = true;
            SaveSystem.ClaimDailyReward();

            // 100 Bonus Skor Hediye Et
            int currentHigh = SaveSystem.GetHighScore();
            SaveSystem.SaveScore(currentHigh + 100);

            AudioManager.Instance?.PlayNewRecordSound();
            AudioManager.Instance?.TriggerHapticFeedback();

            // Sandık zıplama efekti
            StartCoroutine(AnimateChestOpening());

            if (statusText != null) statusText.text = "🎉 TEBRİKLER! +100 SKOR PUANI KAZANDIN!";
            if (claimButtonText != null) claimButtonText.text = "HARİKA!";
        }

        public void CloseModal()
        {
            AudioManager.Instance?.PlayJumpSound();
            gameObject.SetActive(false);
        }

        private IEnumerator AnimateChestBreathing()
        {
            if (chestImage == null) yield break;
            Transform tr = chestImage.transform;

            while (gameObject.activeInHierarchy && !hasClaimed)
            {
                float t = Mathf.Sin(Time.time * 3f) * 0.06f;
                tr.localScale = new Vector3(1f + t, 1f - t, 1f);
                yield return null;
            }
            tr.localScale = Vector3.one;
        }

        private IEnumerator AnimateChestOpening()
        {
            if (chestImage == null) yield break;
            Transform tr = chestImage.transform;

            float elapsed = 0f;
            while (elapsed < 0.4f)
            {
                elapsed += Time.deltaTime;
                float s = 1f + Mathf.Sin(elapsed * Mathf.PI / 0.4f) * 0.28f;
                tr.localScale = Vector3.one * s;
                yield return null;
            }
            tr.localScale = Vector3.one;
        }
    }
}
