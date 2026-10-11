using UnityEngine;

namespace ZiplaKac.Obstacles
{
    public enum ObstacleType
    {
        Cactus,      // Tier 0: Zemin, dar/uzun
        Rock,        // Tier 0: Zemin, geniş/kısa
        Spike,       // Tier 1: Zemin, yüksek sivri diken
        LowBird,     // Tier 2: Havada alçak uçan kuş
        HighBird     // Tier 3: Havada yüksek uçan kuş
    }

    /// <summary>
    /// Engel tiplerini ve fiziksel parametrelerini tanımlayan ScriptableObject yapısı.
    /// </summary>
    [CreateAssetMenu(fileName = "NewObstacleData", menuName = "ZiplaKac/Obstacle Data")]
    public class ObstacleData : ScriptableObject
    {
        public ObstacleType obstacleType;
        public string obstacleName = "Kaktüs";
        public string poolTag = "Cactus";

        [Header("Görsel & Boyut Ayarları")]
        public Vector2 size = new Vector2(0.8f, 1.6f);
        public Color spriteColor = Color.green;

        [Header("Konumlandırma Ayarları")]
        [Tooltip("Zeminden Y ofseti (Kuşlar için yüksek ofset)")]
        public float yOffset = 0.0f;
    }
}
