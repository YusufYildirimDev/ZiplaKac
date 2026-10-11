using UnityEngine;
using ZiplaKac.Core;

namespace ZiplaKac.PowerUps
{
    /// <summary>
    /// Ekranda aynı anda en fazla 1 adet aktif kalacak şekilde 8–14 saniyede bir
    /// rastgele güçlendirme (Shield, Slow, Multi) oluşturan spawner sınıfı.
    /// </summary>
    public class PowerUpSpawner : MonoBehaviour
    {
        [Header("Zamanlama Ayarları")]
        public float minSpawnTime = 8.0f;
        public float maxSpawnTime = 14.0f;

        [Header("Konumlandırma Ayarları")]
        public float spawnX = 4.0f;
        public float minSpawnY = -2.2f;
        public float maxSpawnY = 0.5f;

        private float spawnTimer = 0f;

        private void Awake()
        {
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            float halfWidth = 5.0f * aspect;
            spawnX = halfWidth + 1.2f;
            minSpawnY = -2.2f;
            maxSpawnY = 0.5f;
        }

        private void Start()
        {
            ResetTimer();
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
            {
                return;
            }

            // Ekranda halihazırda aktif bir power-up varsa bekle (Max 1 kuralı)
            if (IsPowerUpActiveOnScreen())
            {
                return;
            }

            spawnTimer -= Time.deltaTime;

            if (spawnTimer <= 0f)
            {
                SpawnRandomPowerUp();
                ResetTimer();
            }
        }

        private void SpawnRandomPowerUp()
        {
            // Rastgele Güçlendirme Seçimi
            PowerUpType[] types = (PowerUpType[])System.Enum.GetValues(typeof(PowerUpType));
            PowerUpType selectedType = types[Random.Range(0, types.Length)];

            string poolTag = GetPoolTag(selectedType);
            float randomY = Random.Range(minSpawnY, maxSpawnY);
            Vector3 spawnPos = new Vector3(spawnX, randomY, 0f);

            if (ObjectPooler.Instance != null)
            {
                GameObject pObj = ObjectPooler.Instance.SpawnFromPool(poolTag, spawnPos, Quaternion.identity);
                if (pObj != null)
                {
                    PowerUp pComp = pObj.GetComponent<PowerUp>();
                    if (pComp == null) pComp = pObj.AddComponent<PowerUp>();
                    pComp.powerUpType = selectedType;
                }
            }
        }

        private bool IsPowerUpActiveOnScreen()
        {
            PowerUp[] activePowerUps = FindObjectsOfType<PowerUp>();
            foreach (var p in activePowerUps)
            {
                if (p.gameObject.activeInHierarchy)
                {
                    return true;
                }
            }
            return false;
        }

        private void ResetTimer()
        {
            spawnTimer = Random.Range(minSpawnTime, maxSpawnTime);
        }

        private string GetPoolTag(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.Shield: return "PowerUp_Shield";
                case PowerUpType.Slow: return "PowerUp_Slow";
                case PowerUpType.ScoreMultiplier: return "PowerUp_Multi";
                default: return "PowerUp_Shield";
            }
        }
    }
}
