using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZiplaKac.Obstacles
{
    [System.Serializable]
    public class WeightedObstacle
    {
        public ObstacleType obstacleType;
        public string poolTag;
        [Range(1, 100)]
        public int weight = 50;
    }

    /// <summary>
    /// Skor kademelerini (Tier 0..3), arka plan tema renklerini ve ağırlıklı engel seçim ihtimallerini yöneten ScriptableObject.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTierData", menuName = "ZiplaKac/Tier Data")]
    public class TierData : ScriptableObject
    {
        public int tierIndex = 0;
        public string tierName = "Tier 0 (Gündüz)";
        public int minScore = 0;
        public int maxScore = 39;

        [Header("Arka Plan Teması")]
        public Color backgroundColorTop = new Color(0.4f, 0.75f, 1.0f); // Açık Mavi Gökyüzü
        public Color backgroundColorBottom = new Color(0.8f, 0.95f, 1.0f);

        [Header("Ağırlıklı Engel İhtimalleri (Weighted Random Pool)")]
        public List<WeightedObstacle> obstacleWeights = new List<WeightedObstacle>();

        /// <summary>
        /// Ağırlıklı rastgele seçim algoritması (Weighted Random Selection)
        /// </summary>
        public WeightedObstacle GetRandomObstacle()
        {
            if (obstacleWeights == null || obstacleWeights.Count == 0) return null;

            int totalWeight = 0;
            foreach (var item in obstacleWeights)
            {
                totalWeight += item.weight;
            }

            int randomValue = Random.Range(0, totalWeight);
            int currentSum = 0;

            foreach (var item in obstacleWeights)
            {
                currentSum += item.weight;
                if (randomValue < currentSum)
                {
                    return item;
                }
            }

            return obstacleWeights[0];
        }
    }
}
