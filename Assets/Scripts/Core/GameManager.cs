using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZiplaKac.Core
{
    public enum GameState
    {
        MainMenu,
        Playing,
        GameOver
    }

    /// <summary>
    /// Zıpla Kaç oyununun tüm akışını, skorunu, zorluk kademelerini (Tier 0-3) ve kombo sistemini yöneten ana Singleton sınıfı.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Temel Hız ve Zorluk Ayarları")]
        [Tooltip("Oyunun başlangıç engel/zemin kayma hızı")]
        public float baseSpeed = 4.2f;
        
        [Tooltip("Saniye başına hız artış miktarı")]
        public float speedAcceleration = 0.05f;
        
        [Tooltip("Maksimum hız sınırı")]
        public float maxSpeed = 8.5f;

        [Header("Skor Ayarları")]
        [Tooltip("Saniye başına verilen taban puan (Yaklaşık 22 puan/sn)")]
        public float basePointsPerSecond = 22.0f;

        [Header("Aktif Oyun Durumu (Read-Only Inspection)")]
        public GameState State { get; private set; } = GameState.MainMenu;
        public float CurrentSpeed { get; private set; }
        public float CurrentScore { get; private set; }
        public int HighScore { get; private set; }
        public int CurrentTierIndex { get; private set; } = 0;
        public int CurrentRunGold { get; private set; } = 0;
        public int RunObstacleGold { get; private set; } = 0;
        public int RunComboGold { get; private set; } = 0;
        public int RunScoreGold { get; private set; } = 0;
        public int RunRecordGold { get; private set; } = 0;
        public static string NewlyUnlockedCharacterId { get; set; } = null;

        [Header("Güçlendirme Durumları")]
        public bool isShieldActive = false;
        public bool isSlowActive = false;
        public bool isDoubleScoreActive = false;
        public bool hasSecondChance = false;
        public bool hasUsedLifeThisRun = false;
        public bool hasFireReviveUsed = false;
        public bool isTemporaryInvulnerable = false;
        private float invulnerabilityTimer = 0f;

        public float slowTimer = 0f;
        public float doubleScoreTimer = 0f;

        [Header("Kombo Sistemi")]
        public int comboCount = 0;
        public int consecutivePassedObstacles = 0;
        public int maxComboInRun = 0;

        // Duraklatma (Pause) Sistemi
        public bool IsPaused { get; private set; } = false;
        public event Action<bool> OnGamePaused;

        // Event'ler
        public event Action<GameState> OnGameStateChanged;
        public event Action<int> OnTierChanged;
        public event Action<int, Vector3> OnComboAchieved; // comboCount, position
        public event Action OnShieldStatusChanged;
        public event Action OnSecondChanceUsed;

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
            }
        }

        private void Start()
        {
            HighScore = SaveSystem.GetHighScore();
            SetState(GameState.MainMenu);
        }

        private void Update()
        {
            if (State != GameState.Playing) return;

            if (isTemporaryInvulnerable)
            {
                invulnerabilityTimer -= Time.deltaTime;
                if (invulnerabilityTimer <= 0f)
                {
                    isTemporaryInvulnerable = false;
                }
            }

            // 1. Hız artışı
            CurrentSpeed += speedAcceleration * Time.deltaTime;
            CurrentSpeed = Mathf.Min(CurrentSpeed, maxSpeed);

            // 2. Skor hesabı (Klasik karakterin imza yeteneği varsa +%15 bonus)
            string charKey = WorldThemeManager.Instance != null ? WorldThemeManager.Instance.CurrentCharacterKey : "klasik";
            bool hasSignature = SaveSystem.GetUpgradeLevel(charKey, "signature") > 0;
            float signatureScoreBonus = (charKey == "klasik" && hasSignature) ? 1.15f : 1.0f;

            float scoreMultiplier = isDoubleScoreActive ? 2.0f : 1.0f;
            CurrentScore += basePointsPerSecond * scoreMultiplier * signatureScoreBonus * Time.deltaTime;

            // 3. Güçlendirme süre takibi
            UpdatePowerUpTimers();

            // 4. Zorluk Kademesi (Tier) kontrolü
            CheckTierProgress();
        }

        public void SetState(GameState newState)
        {
            if (IsPaused)
            {
                IsPaused = false;
                OnGamePaused?.Invoke(false);
            }

            State = newState;

            switch (newState)
            {
                case GameState.MainMenu:
                    Time.timeScale = 1.0f;
                    ResetPowerUps();
                    Obstacles.Obstacle.ClearAllActiveObstacles();
                    PowerUps.PowerUp.ClearAllActivePowerUps();
                    break;

                case GameState.Playing:
                    Time.timeScale = 1.0f;
                    CurrentScore = 0f;
                    CurrentSpeed = baseSpeed;
                    CurrentTierIndex = 0;
                    CurrentRunGold = 0;
                    RunObstacleGold = 0;
                    RunComboGold = 0;
                    RunScoreGold = 0;
                    RunRecordGold = 0;
                    isTemporaryInvulnerable = false;
                    invulnerabilityTimer = 0f;
                    NewlyUnlockedCharacterId = null;

                    if (WorldThemeManager.Instance != null)
                    {
                        WorldThemeManager.Instance.SyncFromSavedCharacter();
                        WorldThemeManager.Instance.SetStage(0);
                    }
                    comboCount = 0;
                    consecutivePassedObstacles = 0;
                    maxComboInRun = 0;
                    ResetPowerUps();
                    Obstacles.Obstacle.ClearAllActiveObstacles();
                    PowerUps.PowerUp.ClearAllActivePowerUps();
                    hasUsedLifeThisRun = false;
                    hasFireReviveUsed = false;

                    // --- Tek Kullanımlık (Consumable) Güçlendirmeleri Uygula ---
                    ApplyConsumablesAtStart();
                    break;

                case GameState.GameOver:
                    Time.timeScale = 1.0f;
                    int finalScoreInt = Mathf.FloorToInt(CurrentScore);
                    bool isNewRecord = SaveSystem.SaveScore(finalScoreInt);
                    if (isNewRecord)
                    {
                        HighScore = finalScoreInt;
                        RunRecordGold = 50; // Yeni rekor bonusu
                    }

                    // Aktif oynanan karakter için skoru kaydet ve sıralı kilit açılımını kontrol et
                    string activeCharId = SaveSystem.GetSelectedCharacterId("classic");
                    SaveSystem.SaveCharacterScore(activeCharId, finalScoreInt);

                    NewlyUnlockedCharacterId = null;
                    if (SaveSystem.CheckAndUnlockNextCharacter(activeCharId, finalScoreInt, out string unlockedNextChar))
                    {
                        NewlyUnlockedCharacterId = unlockedNextChar;
                        AudioManager.Instance?.PlayPowerUpSound();
                    }

                    // Skorun %10'u kadar altın
                    RunScoreGold = Mathf.FloorToInt(CurrentScore * 0.10f);
                    CurrentRunGold += RunScoreGold + RunRecordGold;

                    if (CurrentRunGold > 0)
                    {
                        SaveSystem.AddGold(CurrentRunGold);
                    }

                    // Apple Game Center Skor ve Başarım Bildirimi
                    if (GameCenterManager.Instance != null)
                    {
                        GameCenterManager.Instance.ReportScore(finalScoreInt);
                        if (finalScoreInt >= 1000) GameCenterManager.Instance.ReportAchievement("ach_score_1000");
                        if (finalScoreInt >= 2500) GameCenterManager.Instance.ReportAchievement("ach_score_2500");
                        if (maxComboInRun >= 20) GameCenterManager.Instance.ReportAchievement("ach_combo_20");
                    }
                    break;
            }

            OnGameStateChanged?.Invoke(State);
        }

        private void ApplyConsumablesAtStart()
        {
            string charKey = WorldThemeManager.Instance != null ? WorldThemeManager.Instance.CurrentCharacterKey : "klasik";

            // 1. Başlangıç Kalkanı (Varsa tüket ve aç)
            if (SaveSystem.UseConsumable("shield"))
            {
                ActivateShield();
            }
            else if (SaveSystem.IsOutfitEnabled() && SaveSystem.IsOutfitEquipped(charKey, "ozel_aksesuar") && UnityEngine.Random.value < 0.35f)
            {
                // Özel Aksesuar (Taç) Gücü: %35 şansla oyuna ücretsiz Kalkan ile başla
                ActivateShield();
            }

            // 2. Yavaş Başlangıç (İlk 5 saniye yavaşlatma)
            if (SaveSystem.UseConsumable("slow_start"))
            {
                ActivateSlow(5.0f);
            }

            // 3. Şanslı Güçlendirme (İlk güçlendirme 3. saniyede çıkar)
            if (SaveSystem.UseConsumable("lucky_pu"))
            {
                PowerUps.PowerUpSpawner spawner = UnityEngine.Object.FindFirstObjectByType<PowerUps.PowerUpSpawner>();
                if (spawner != null) spawner.SetNextSpawnTime(3.0f);
            }

            // 4. İkinci Şans Bileti
            hasSecondChance = SaveSystem.GetConsumableCount("second_chance") > 0;
        }

        public void StartGame()
        {
            if (IsPaused)
            {
                ResumeGame();
            }
            SetState(GameState.Playing);
        }

        public void PauseGame()
        {
            if (State != GameState.Playing || IsPaused) return;

            IsPaused = true;
            Time.timeScale = 0f;
            OnGamePaused?.Invoke(true);
        }

        public void ResumeGame()
        {
            if (!IsPaused) return;

            IsPaused = false;
            Time.timeScale = 1.0f;
            OnGamePaused?.Invoke(false);
        }

        public void TogglePause()
        {
            if (IsPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && State == GameState.Playing && !IsPaused)
            {
                PauseGame();
            }
        }

        public void TriggerTemporaryInvulnerability(float duration = 1.5f)
        {
            isTemporaryInvulnerable = true;
            invulnerabilityTimer = duration;
        }

        public bool RevivePlayer(int goldCost = 500)
        {
            if (State != GameState.GameOver) return false;

            // 1. Envanterde ekstra can varsa doğrudan kullan ve diril
            if (SaveSystem.GetConsumableCount("second_chance") > 0)
            {
                if (SaveSystem.UseConsumable("second_chance"))
                {
                    ExecuteRevive();
                    return true;
                }
            }

            // 2. Ekstra can yoksa altın ile can alıp diril
            if (SaveSystem.SpendGold(goldCost))
            {
                ExecuteRevive();
                return true;
            }

            return false;
        }

        public bool ReviveWithGold(int cost = 500)
        {
            return RevivePlayer(cost);
        }

        private void ExecuteRevive()
        {
            // Önceki GameOver aşamasında eklenen tur içi altını sıfırla ki sonraki ölümde tekrar toplanmasın
            CurrentRunGold = 0;
            RunObstacleGold = 0;
            RunComboGold = 0;
            RunScoreGold = 0;
            RunRecordGold = 0;

            // Sahadaki engelleri temizle
            Obstacles.Obstacle.ClearAllActiveObstacles();

            // 2.5 saniye geçici dokunulmazlık sağla
            TriggerTemporaryInvulnerability(2.5f);

            // Karakteri zemine al ve fizik simülasyonunu aç
            if (Player.PlayerController.Instance != null)
            {
                Player.PlayerController.Instance.ResetToGround();
            }

            // Oynanış durumuna devam et (skor korunur)
            State = GameState.Playing;
            Time.timeScale = 1.0f;
            OnGameStateChanged?.Invoke(State);

            AudioManager.Instance?.PlayPowerUpSound();
            AudioManager.Instance?.TriggerHapticFeedback();
        }

        public void GameOver()
        {
            if (isTemporaryInvulnerable) return;

            string charKey = WorldThemeManager.Instance != null ? WorldThemeManager.Instance.CurrentCharacterKey : "klasik";
            bool hasComboMemory = SaveSystem.GetUpgradeLevel(charKey, "combo_memory") > 0;
            bool hasSignature = SaveSystem.GetUpgradeLevel(charKey, "signature") > 0;

            if (isShieldActive)
            {
                // Kalkan darbeyi emer
                isShieldActive = false;
                if (hasComboMemory)
                {
                    comboCount = Mathf.FloorToInt(comboCount / 2f);
                    consecutivePassedObstacles = comboCount * 5;
                }
                else
                {
                    comboCount = 0;
                    consecutivePassedObstacles = 0;
                }
                OnShieldStatusChanged?.Invoke();
                AudioManager.Instance?.PlayShieldHitSound();
                AudioManager.Instance?.TriggerHapticFeedback();
                TriggerTemporaryInvulnerability(0.8f);
                return;
            }

            // İkinci Şans Bileti Kontrolü (Her oyunda SADECE BİR KERE can kullanılabilir)
            if (!hasUsedLifeThisRun && hasSecondChance && SaveSystem.UseConsumable("second_chance"))
            {
                hasUsedLifeThisRun = true;
                hasSecondChance = false;
                TriggerTemporaryInvulnerability(1.6f);
                Obstacles.Obstacle.ClearAllActiveObstacles();
                if (hasComboMemory)
                {
                    comboCount = Mathf.FloorToInt(comboCount / 2f);
                    consecutivePassedObstacles = comboCount * 5;
                }
                else
                {
                    comboCount = 0;
                    consecutivePassedObstacles = 0;
                }
                AudioManager.Instance?.PlayPowerUpSound();
                AudioManager.Instance?.TriggerHapticFeedback();
                OnSecondChanceUsed?.Invoke();
                return;
            }

            // Ateş Karakteri İmza Yeteneği: Alev Patlaması (Turda 1 kez ilk çarpmada engeli yakıp yok eder)
            if (charKey == "ates" && hasSignature && !hasFireReviveUsed)
            {
                hasFireReviveUsed = true;
                TriggerTemporaryInvulnerability(1.8f);
                Obstacles.Obstacle.ClearAllActiveObstacles();
                if (hasComboMemory)
                {
                    comboCount = Mathf.FloorToInt(comboCount / 2f);
                    consecutivePassedObstacles = comboCount * 5;
                }
                else
                {
                    comboCount = 0;
                    consecutivePassedObstacles = 0;
                }
                AudioManager.Instance?.PlayPowerUpSound();
                AudioManager.Instance?.TriggerHapticFeedback();
                UI.UIManager.Instance?.TriggerCameraShake(0.25f, 0.15f);
                return;
            }

            // Gece Karakteri İmza Yeteneği: %28 Otomatik Sıyrılma Şansı
            if (charKey == "gece" && hasSignature && UnityEngine.Random.value < 0.28f)
            {
                TriggerTemporaryInvulnerability(1.6f);
                Obstacles.Obstacle.ClearAllActiveObstacles();
                if (hasComboMemory)
                {
                    comboCount = Mathf.FloorToInt(comboCount / 2f);
                    consecutivePassedObstacles = comboCount * 5;
                }
                else
                {
                    comboCount = 0;
                    consecutivePassedObstacles = 0;
                }
                AudioManager.Instance?.PlayPowerUpSound();
                AudioManager.Instance?.TriggerHapticFeedback();
                return;
            }

            AudioManager.Instance?.PlayGameOverSound();
            AudioManager.Instance?.TriggerHapticFeedback();
            SetState(GameState.GameOver);
        }

        public void OnObstacleCleared(Vector3 obstaclePosition)
        {
            if (State != GameState.Playing) return;

            string charKey = WorldThemeManager.Instance != null ? WorldThemeManager.Instance.CurrentCharacterKey : "klasik";

            // Her geçilen engel için altın kazanılır (x2 puan varsa +2 altın)
            int goldEarned = isDoubleScoreActive ? 2 : 1;

            // Güneş Gözlüğü Gücü: Altın Gözü (Kazanılan altınlara +%25~50 bonus şansı)
            bool hasGlasses = SaveSystem.IsOutfitEnabled() && SaveSystem.IsOutfitEquipped(charKey, "gunes_gozlugu");
            if (hasGlasses && (consecutivePassedObstacles % 2 == 0))
            {
                goldEarned += 1;
            }

            RunObstacleGold += goldEarned;
            CurrentRunGold += goldEarned;
            AudioManager.Instance?.PlayCoinSound();

            consecutivePassedObstacles++;

            // Kombo Serisi Altın Bonusu (5 / 10 / 20 / 40 eşikleri)
            int comboGoldBonus = 0;
            if (consecutivePassedObstacles == 5) comboGoldBonus = 2;
            else if (consecutivePassedObstacles == 10) comboGoldBonus = 5;
            else if (consecutivePassedObstacles == 20) comboGoldBonus = 12;
            else if (consecutivePassedObstacles == 40) comboGoldBonus = 30;
            else if (consecutivePassedObstacles > 40 && consecutivePassedObstacles % 10 == 0) comboGoldBonus = 10;

            // Zehir Karakteri İmza Yeteneği: Kombo ödülleri x1.25 katına çıkar
            if (charKey == "zehir" && SaveSystem.GetUpgradeLevel(charKey, "signature") > 0 && comboGoldBonus > 0)
            {
                comboGoldBonus = Mathf.RoundToInt(comboGoldBonus * 1.25f);
            }

            if (comboGoldBonus > 0)
            {
                RunComboGold += comboGoldBonus;
                CurrentRunGold += comboGoldBonus;
            }

            if (consecutivePassedObstacles % 5 == 0)
            {
                comboCount++;
                if (comboCount > maxComboInRun) maxComboInRun = comboCount;

                // Parti Şapkası Gücü: Parti Coşkusu (Kombolarda +15 ekstra kombo puanı: 10 -> 25)
                bool hasPartyHat = SaveSystem.IsOutfitEnabled() && SaveSystem.IsOutfitEquipped(charKey, "parti_sapkasi");
                int bonusPoints = hasPartyHat ? 25 : 10;
                CurrentScore += bonusPoints;
                OnComboAchieved?.Invoke(comboCount, obstaclePosition);
                AudioManager.Instance?.PlayComboSound();
            }
        }

        public void AddBonusScore(float amount)
        {
            CurrentScore += amount;
            if (Mathf.FloorToInt(CurrentScore) > HighScore)
            {
                HighScore = Mathf.FloorToInt(CurrentScore);
            }
        }

        public void ActivateShield()
        {
            isShieldActive = true;
            OnShieldStatusChanged?.Invoke();
            AudioManager.Instance?.PlayPowerUpSound();
        }

        public void ActivateSlow(float duration = 4.0f)
        {
            string charKey = WorldThemeManager.Instance != null ? WorldThemeManager.Instance.CurrentCharacterKey : "klasik";
            int puLevel = SaveSystem.GetUpgradeLevel(charKey, "pu_duration");
            bool hasCrown = SaveSystem.IsOutfitEnabled() && SaveSystem.IsOutfitEquipped(charKey, "ozel_aksesuar");
            float crownMultiplier = hasCrown ? 1.25f : 1.0f; // Özel Aksesuar: +%25 süre
            float finalDuration = duration * (1f + 0.20f * puLevel) * crownMultiplier;

            isSlowActive = true;
            slowTimer = finalDuration;
            AudioManager.Instance?.PlayPowerUpSound();
        }

        public void ActivateDoubleScore(float duration = 5.0f)
        {
            string charKey = WorldThemeManager.Instance != null ? WorldThemeManager.Instance.CurrentCharacterKey : "klasik";
            int puLevel = SaveSystem.GetUpgradeLevel(charKey, "pu_duration");
            bool hasCrown = SaveSystem.IsOutfitEnabled() && SaveSystem.IsOutfitEquipped(charKey, "ozel_aksesuar");
            float crownMultiplier = hasCrown ? 1.25f : 1.0f; // Özel Aksesuar: +%25 süre
            float finalDuration = duration * (1f + 0.20f * puLevel) * crownMultiplier;

            isDoubleScoreActive = true;
            doubleScoreTimer = finalDuration;
            AudioManager.Instance?.PlayPowerUpSound();
        }

        private void UpdatePowerUpTimers()
        {
            if (isSlowActive)
            {
                slowTimer -= Time.deltaTime;
                if (slowTimer <= 0f)
                {
                    isSlowActive = false;
                    slowTimer = 0f;
                }
            }

            if (isDoubleScoreActive)
            {
                doubleScoreTimer -= Time.deltaTime;
                if (doubleScoreTimer <= 0f)
                {
                    isDoubleScoreActive = false;
                    doubleScoreTimer = 0f;
                }
            }
        }

        private void ResetPowerUps()
        {
            isShieldActive = false;
            isSlowActive = false;
            isDoubleScoreActive = false;
            slowTimer = 0f;
            doubleScoreTimer = 0f;
            OnShieldStatusChanged?.Invoke();
        }

        private void CheckTierProgress()
        {
            int calculatedTier = 0;
            int currentScoreInt = Mathf.FloorToInt(CurrentScore);

            // 3 Aşama: 0-749 (Aşama 1), 750-1499 (Aşama 2), 1500+ (Aşama 3)
            if (currentScoreInt >= 1500)
            {
                calculatedTier = 2;
            }
            else if (currentScoreInt >= 750)
            {
                calculatedTier = 1;
            }
            else
            {
                calculatedTier = 0;
            }

            if (calculatedTier != CurrentTierIndex)
            {
                CurrentTierIndex = calculatedTier;
                if (WorldThemeManager.Instance != null)
                {
                    WorldThemeManager.Instance.SetStage(CurrentTierIndex);
                }
                OnTierChanged?.Invoke(CurrentTierIndex);
            }
        }

        public float GetEffectiveSpeed()
        {
            float speed = CurrentSpeed;
            string charKey = WorldThemeManager.Instance != null ? WorldThemeManager.Instance.CurrentCharacterKey : "klasik";
            bool hasSignature = SaveSystem.GetUpgradeLevel(charKey, "signature") > 0;
            if (charKey == "buz" && hasSignature)
            {
                speed *= 0.90f; // Buz Karakteri İmza Yeteneği: Buzul Serinliği (%10 daha kontrollü akış hızı)
            }
            return isSlowActive ? speed * 0.5f : speed;
        }
    }
}
