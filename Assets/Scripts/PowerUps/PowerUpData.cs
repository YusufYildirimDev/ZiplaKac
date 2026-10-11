using UnityEngine;

namespace ZiplaKac.PowerUps
{
    public enum PowerUpType
    {
        Shield,          // S - Kalkan (Bir sonraki darbeyi absorbe eder)
        Slow,            // T - Yavaşlatma (4sn %50 hız azaltma)
        ScoreMultiplier  // 2X - x2 Skor (5sn 2 kat puan)
    }

    /// <summary>
    /// Güçlendirme (Power-up) parametrelerini tanımlayan ScriptableObject yapısı.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPowerUpData", menuName = "ZiplaKac/PowerUp Data")]
    public class PowerUpData : ScriptableObject
    {
        public PowerUpType powerUpType;
        public string powerUpName = "Kalkan";
        public string iconText = "S";
        public Color circleColor = Color.cyan;
        public float duration = 4.0f;
    }
}
