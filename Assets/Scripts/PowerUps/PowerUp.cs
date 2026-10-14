using UnityEngine;
using ZiplaKac.Core;

namespace ZiplaKac.PowerUps
{
    /// <summary>
    /// Ekrandaki güçlendirme nesnesinin hareketini, yukarı-aşağı salınım (bobbing) animasyonunu
    /// ve oyuncu tarafından toplanmasını (pickup) yöneten sınıf.
    /// </summary>
    public class PowerUp : MonoBehaviour
    {
        public PowerUpType powerUpType = PowerUpType.Shield;
        public float bobbingSpeed = 3.0f;
        public float bobbingAmount = 0.25f;
        public float offscreenX = -4.0f;

        private float startY;
        private float bobbingTimer = 0f;

        private void Awake()
        {
            UpdateOffscreenBoundary();
        }

        private void OnEnable()
        {
            UpdateOffscreenBoundary();
            startY = transform.position.y;
            bobbingTimer = Random.Range(0f, 6.28f); // Rastgele faz ile başla
        }

        private void UpdateOffscreenBoundary()
        {
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            float halfWidth = 5.0f * aspect;
            offscreenX = -halfWidth - 1.5f;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
            {
                return;
            }

            // 1. Sağdan sola hareket
            float speed = GameManager.Instance.GetEffectiveSpeed();
            transform.Translate(Vector3.left * speed * Time.deltaTime, Space.World);

            // 2. Yukarı-Aşağı Salınım (Bobbing Motion)
            bobbingTimer += Time.deltaTime * bobbingSpeed;
            float newY = startY + Mathf.Sin(bobbingTimer) * bobbingAmount;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);

            // 3. Ekran dışına çıkma kontrolü
            if (transform.position.x < offscreenX)
            {
                RecycleToPool();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            if (other.CompareTag("Player"))
            {
                // Güçlendirmeyi aktifleştir
                switch (powerUpType)
                {
                    case PowerUpType.Shield:
                        GameManager.Instance.ActivateShield();
                        break;
                    case PowerUpType.Slow:
                        GameManager.Instance.ActivateSlow(4.0f);
                        break;
                    case PowerUpType.ScoreMultiplier:
                        GameManager.Instance.ActivateDoubleScore(5.0f);
                        break;
                }

                RecycleToPool();
            }
        }

        public void RecycleToPool()
        {
            string poolTag = GetPoolTag();
            if (ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance.ReturnToPool(poolTag, gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public static void ClearAllActivePowerUps()
        {
            PowerUp[] all = FindObjectsOfType<PowerUp>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].gameObject.activeSelf)
                {
                    all[i].RecycleToPool();
                }
            }
        }

        private string GetPoolTag()
        {
            switch (powerUpType)
            {
                case PowerUpType.Shield: return "PowerUp_Shield";
                case PowerUpType.Slow: return "PowerUp_Slow";
                case PowerUpType.ScoreMultiplier: return "PowerUp_Multi";
                default: return "PowerUp_Shield";
            }
        }
    }
}
