using UnityEngine;

namespace ZiplaKac.Player
{
    public enum UnlockMethod
    {
        Free,
        WatchAd,
        IAP,
        HighScoreRequirement
    }

    /// <summary>
    /// Karakter varyantlarını esnek ve genişletilebilir kılan ScriptableObject veri yapısı.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacterData", menuName = "ZiplaKac/Character Data")]
    public class CharacterData : ScriptableObject
    {
        [Header("Kimlik ve İsim")]
        public string characterId = "classic";
        public string characterName = "Klasik";

        [Header("Görsel & Renk Ayarları")]
        public Color bodyColor = new Color(1f, 0.85f, 0.1f); // Sarı
        public Color glowColor = new Color(1f, 0.95f, 0.4f, 0.8f);

        [Header("Ticari & Kilit Ayarları")]
        public bool isLocked = false;
        public UnlockMethod unlockMethod = UnlockMethod.Free;
        public int requiredHighScore = 0;
        public string iapProductId = "";
        
        [TextArea(2, 4)]
        public string description = "Klasik sarı runner karakteri.";
    }
}
