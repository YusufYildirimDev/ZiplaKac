using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZiplaKac.Core;

namespace ZiplaKac.Obstacles
{
    /// <summary>
    /// Zorluk kademelerine (Tier 0-3) göre ağırlıklı rastgele engel üreten (spawn) ve
    /// artan hıza göre spawn sıklığını dinamik olarak sıkan spawner sınıfı.
    /// </summary>
    public class ObstacleSpawner : MonoBehaviour
    {
        [Header("Spawn Konumu Ayarları")]
        public float spawnX = 4.0f;
        public float groundY = -3.5f;

        [Header("Zamanlama Ayarları")]
        [Tooltip("Başlangıç iki engel arası süre (saniye)")]
        public float initialSpawnInterval = 1.40f;

        [Tooltip("Sıkışabileceği minimum spawn aralığı sınırı (Adil zorluk için)")]
        public float minSpawnInterval = 0.75f;

        [Header("Tier Yapılandırmaları")]
        public List<TierData> tiers = new List<TierData>();

        private float spawnTimer = 1.25f;

        private void Awake()
        {
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            float halfWidth = 5.0f * aspect;
            spawnX = halfWidth + 1.2f;
            groundY = -3.20f;
        }

        private void Start()
        {
            InitializeDefaultTiersIfEmpty();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.Playing)
            {
                spawnTimer = 1.25f; // Oyun başlayınca oyuncuya ritmi yakalaması için 1.25s süre ver
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
            {
                return;
            }

            spawnTimer -= Time.deltaTime;

            if (spawnTimer <= 0f)
            {
                SpawnNextObstacle();
                CalculateNextSpawnDelay();
            }
        }

        private void SpawnNextObstacle()
        {
            int currentTierIndex = GameManager.Instance.CurrentTierIndex;
            TierData activeTier = GetTierData(currentTierIndex);

            if (activeTier == null) return;

            WeightedObstacle chosen = activeTier.GetRandomObstacle();
            if (chosen == null) return;

            // Y Yüksekliği Ayarı (Zemin vs Kuşlar)
            float spawnY = groundY;
            if (chosen.obstacleType == ObstacleType.LowBird)
            {
                spawnY = -2.05f; // Alçak uçuş: üzerinden zıplanabilir
            }
            else if (chosen.obstacleType == ObstacleType.HighBird)
            {
                spawnY = -1.40f; // Yüksek uçuş: altından geçilir
            }
            else
            {
                // Cactus, Spike, Rock: tabanları zemin yüzeyi Y = -3.20f üzerinde durur
                spawnY = groundY;
            }

            Vector3 spawnPosition = new Vector3(spawnX, spawnY, 0f);

            if (ObjectPooler.Instance != null)
            {
                GameObject obstacleObj = ObjectPooler.Instance.SpawnFromPool(chosen.poolTag, spawnPosition, Quaternion.identity);
                if (obstacleObj != null)
                {
                    Obstacle obsComponent = obstacleObj.GetComponent<Obstacle>();
                    if (obsComponent == null) obsComponent = obstacleObj.AddComponent<Obstacle>();
                    obsComponent.poolTag = chosen.poolTag;
                }
            }
        }

        private void CalculateNextSpawnDelay()
        {
            // Hız arttıkça iki engel arası süre azalır
            float currentSpeed = GameManager.Instance.CurrentSpeed;
            float speedRatio = currentSpeed / GameManager.Instance.baseSpeed;

            float baseInterval = initialSpawnInterval / speedRatio;
            float jitter = Random.Range(-0.12f, 0.20f);
            spawnTimer = Mathf.Max(baseInterval + jitter, minSpawnInterval);
        }

        private TierData GetTierData(int tierIndex)
        {
            if (tierIndex >= 0 && tierIndex < tiers.Count)
            {
                return tiers[tierIndex];
            }
            return tiers.Count > 0 ? tiers[0] : null;
        }

        private void InitializeDefaultTiersIfEmpty()
        {
            if (tiers != null && tiers.Count > 0) return;

            // KOD İÇİ DİNAMİK TIER OLUŞTURMA (ScriptableObject assetleri atanmamışsa)
            tiers = new List<TierData>();

            // Tier 0
            TierData t0 = ScriptableObject.CreateInstance<TierData>();
            t0.tierIndex = 0; t0.minScore = 0; t0.maxScore = 39;
            t0.backgroundColorTop = new Color(0.4f, 0.75f, 1.0f);
            t0.backgroundColorBottom = new Color(0.8f, 0.95f, 1.0f);
            t0.obstacleWeights = new List<WeightedObstacle>
            {
                new WeightedObstacle { obstacleType = ObstacleType.Cactus, poolTag = "Cactus", weight = 70 },
                new WeightedObstacle { obstacleType = ObstacleType.Rock, poolTag = "Rock", weight = 30 }
            };
            tiers.Add(t0);

            // Tier 1
            TierData t1 = ScriptableObject.CreateInstance<TierData>();
            t1.tierIndex = 1; t1.minScore = 40; t1.maxScore = 99;
            t1.backgroundColorTop = new Color(1.0f, 0.45f, 0.2f);
            t1.backgroundColorBottom = new Color(0.9f, 0.2f, 0.3f);
            t1.obstacleWeights = new List<WeightedObstacle>
            {
                new WeightedObstacle { obstacleType = ObstacleType.Cactus, poolTag = "Cactus", weight = 50 },
                new WeightedObstacle { obstacleType = ObstacleType.Rock, poolTag = "Rock", weight = 25 },
                new WeightedObstacle { obstacleType = ObstacleType.Spike, poolTag = "Spike", weight = 25 }
            };
            tiers.Add(t1);

            // Tier 2
            TierData t2 = ScriptableObject.CreateInstance<TierData>();
            t2.tierIndex = 2; t2.minScore = 100; t2.maxScore = 199;
            t2.backgroundColorTop = new Color(0.4f, 0.2f, 0.6f);
            t2.backgroundColorBottom = new Color(0.15f, 0.1f, 0.35f);
            t2.obstacleWeights = new List<WeightedObstacle>
            {
                new WeightedObstacle { obstacleType = ObstacleType.Cactus, poolTag = "Cactus", weight = 35 },
                new WeightedObstacle { obstacleType = ObstacleType.Rock, poolTag = "Rock", weight = 20 },
                new WeightedObstacle { obstacleType = ObstacleType.Spike, poolTag = "Spike", weight = 25 },
                new WeightedObstacle { obstacleType = ObstacleType.LowBird, poolTag = "LowBird", weight = 20 }
            };
            tiers.Add(t2);

            // Tier 3
            TierData t3 = ScriptableObject.CreateInstance<TierData>();
            t3.tierIndex = 3; t3.minScore = 200; t3.maxScore = 99999;
            t3.backgroundColorTop = new Color(0.05f, 0.05f, 0.15f);
            t3.backgroundColorBottom = new Color(0.02f, 0.02f, 0.08f);
            t3.obstacleWeights = new List<WeightedObstacle>
            {
                new WeightedObstacle { obstacleType = ObstacleType.Cactus, poolTag = "Cactus", weight = 25 },
                new WeightedObstacle { obstacleType = ObstacleType.Rock, poolTag = "Rock", weight = 15 },
                new WeightedObstacle { obstacleType = ObstacleType.Spike, poolTag = "Spike", weight = 20 },
                new WeightedObstacle { obstacleType = ObstacleType.LowBird, poolTag = "LowBird", weight = 20 },
                new WeightedObstacle { obstacleType = ObstacleType.HighBird, poolTag = "HighBird", weight = 20 }
            };
            tiers.Add(t3);
        }
    }
}
