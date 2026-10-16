using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZiplaKac.Core
{
    [System.Serializable]
    public struct WorldThemePalette
    {
        public string worldName;
        public Color groundObstacleColor;
        public Color airObstacleColor;
        public Color accentColor;
    }

    /// <summary>
    /// 5 Karakter × 3 Aşama Dünya ve Tematik Engel Yöneticisi.
    /// Her karakterin dünyasını, aşama arka planlarını, zemin dokularını ve tematik engel renklerini koordine eder.
    /// </summary>
    public class WorldThemeManager : MonoBehaviour
    {
        public static WorldThemeManager Instance { get; private set; }

        public event Action<string, int> OnWorldThemeChanged;

        public string CurrentCharacterKey { get; private set; } = "klasik";
        public int CurrentStageIndex { get; private set; } = 0; // 0 = Aşama 1, 1 = Aşama 2, 2 = Aşama 3

        private static readonly Dictionary<string, string[]> StageNames = new Dictionary<string, string[]>
        {
            { "klasik", new[] { "01_gunduz_cayiri", "02_altin_sonbahar", "03_yildizli_cayir_gecesi" } },
            { "ates",   new[] { "01_col_gunbatimi", "02_lav_vadisi", "03_kizil_gece" } },
            { "buz",    new[] { "01_karli_daglar", "02_buzul_alacakaranligi", "03_kutup_gecesi" } },
            { "zehir",  new[] { "01_yemyesil_orman", "02_toksik_sis", "03_radyoaktif_gece" } },
            { "gece",   new[] { "01_alacakaranlik_orman", "02_mor_nebula", "03_derin_uzay" } }
        };

        private static readonly Dictionary<string, WorldThemePalette> Palettes = new Dictionary<string, WorldThemePalette>
        {
            {
                "klasik", new WorldThemePalette
                {
                    worldName = "Çayır Diyarı",
                    groundObstacleColor = new Color(0.94f, 0.28f, 0.44f), // #ef476f Canlı Güneş Mercanı
                    airObstacleColor = new Color(0.94f, 0.28f, 0.44f),
                    accentColor = new Color(1.0f, 0.82f, 0.40f)         // #ffd166 Altın
                }
            },
            {
                "ates", new WorldThemePalette
                {
                    worldName = "Volkan Diyarı",
                    groundObstacleColor = new Color(1.0f, 0.22f, 0.0f),  // #ff3800 Kor Kırmızı Magma
                    airObstacleColor = new Color(1.0f, 0.35f, 0.0f),     // #ff5900 Alev Kuşu
                    accentColor = new Color(1.0f, 0.65f, 0.0f)          // #ffa600 Lav Sarısı
                }
            },
            {
                "buz", new WorldThemePalette
                {
                    worldName = "Buz Diyarı",
                    groundObstacleColor = new Color(0.0f, 0.94f, 1.0f),  // #00f0ff Parlayan Buzul Cyan
                    airObstacleColor = new Color(0.28f, 0.79f, 0.94f),   // #48cae4 Kutup Kuşu
                    accentColor = new Color(0.88f, 0.97f, 1.0f)         // #e0f7fa Kar Beyazı
                }
            },
            {
                "zehir", new WorldThemePalette
                {
                    worldName = "Zehirli Bataklık",
                    groundObstacleColor = new Color(0.22f, 1.0f, 0.08f), // #39ff14 Toksik Neon Asit
                    airObstacleColor = new Color(0.0f, 0.90f, 0.46f),    // #00e676 Asit Kuşu
                    accentColor = new Color(0.65f, 1.0f, 0.20f)         // #a6ff33 Fosfor
                }
            },
            {
                "gece", new WorldThemePalette
                {
                    worldName = "Gölge Diyarı",
                    groundObstacleColor = new Color(0.85f, 0.27f, 0.94f),// #d946ef Kozmik Radyant Mor
                    airObstacleColor = new Color(0.96f, 0.25f, 0.37f),   // #f43f5e Nebula Kuşu
                    accentColor = new Color(0.66f, 0.33f, 0.97f)        // #a855f7 Derin Mor
                }
            }
        };

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                SyncFromSavedCharacter();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public static string NormalizeCharacterKey(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return "klasik";
            string lower = characterId.ToLowerInvariant();

            if (lower.Contains("fire") || lower.Contains("ates")) return "ates";
            if (lower.Contains("ice") || lower.Contains("buz")) return "buz";
            if (lower.Contains("toxic") || lower.Contains("poison") || lower.Contains("zehir")) return "zehir";
            if (lower.Contains("night") || lower.Contains("shadow") || lower.Contains("gece")) return "gece";
            return "klasik";
        }

        public void SyncFromSavedCharacter()
        {
            string savedId = SaveSystem.GetSelectedCharacterId("classic");
            CurrentCharacterKey = NormalizeCharacterKey(savedId);
        }

        public void SetCharacter(string characterId)
        {
            string normalized = NormalizeCharacterKey(characterId);
            if (normalized != CurrentCharacterKey)
            {
                CurrentCharacterKey = normalized;
                OnWorldThemeChanged?.Invoke(CurrentCharacterKey, CurrentStageIndex);
            }
        }

        public void SetStage(int stageIndex)
        {
            int clamped = Mathf.Clamp(stageIndex, 0, 2);
            if (clamped != CurrentStageIndex)
            {
                CurrentStageIndex = clamped;
                OnWorldThemeChanged?.Invoke(CurrentCharacterKey, CurrentStageIndex);
            }
        }

        public WorldThemePalette GetCurrentPalette()
        {
            if (Palettes.TryGetValue(CurrentCharacterKey, out var palette))
            {
                return palette;
            }
            return Palettes["klasik"];
        }

        public Sprite GetBackgroundSprite(string characterKey, int stageIndex)
        {
            string key = NormalizeCharacterKey(characterKey);
            int stage = Mathf.Clamp(stageIndex, 0, 2);

            if (StageNames.TryGetValue(key, out var names) && stage < names.Length)
            {
                string path = $"Sprites/Worlds/bg_{key}_{names[stage]}";
                Sprite sp = Resources.Load<Sprite>(path);
                if (sp != null) return sp;
            }

            // Fallback
            return Resources.Load<Sprite>($"Sprites/bg_tier{stage}");
        }

        public Sprite GetGroundSprite(string characterKey, int stageIndex)
        {
            string key = NormalizeCharacterKey(characterKey);
            int stage = Mathf.Clamp(stageIndex, 0, 2);

            if (StageNames.TryGetValue(key, out var names) && stage < names.Length)
            {
                string path = $"Sprites/Worlds/zemin_{key}_{names[stage]}";
                Sprite sp = Resources.Load<Sprite>(path);
                if (sp != null) return sp;
            }

            // Fallback
            return Resources.Load<Sprite>($"Sprites/ground_tier{stage}");
        }
    }
}
