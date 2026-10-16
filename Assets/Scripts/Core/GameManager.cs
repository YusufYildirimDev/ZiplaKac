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

        [Header("Güçlendirme Durumları")]
        public bool isShieldActive = false;
        public bool isSlowActive = false;
        public bool isDoubleScoreActive = false;

        public float slowTimer = 0f;
        public float doubleScoreTimer = 0f;

        [Header("Kombo Sistemi")]
        public int comboCount = 0;
        public int consecutivePassedObstacles = 0;

        // Event'ler
        public event Action<GameState> OnGameStateChanged;
        public event Action<int> OnTierChanged;
        public event Action<int, Vector3> OnComboAchieved; // comboCount, position
        public event Action OnShieldStatusChanged;

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

            // 1. Hız artışı
            CurrentSpeed += speedAcceleration * Time.deltaTime;
            CurrentSpeed = Mathf.Min(CurrentSpeed, maxSpeed);

            // 2. Skor hesabı (Slow aktif olsa dahi puan kazanma hızı etkilenmez)
            float scoreMultiplier = isDoubleScoreActive ? 2.0f : 1.0f;
            CurrentScore += basePointsPerSecond * scoreMultiplier * Time.deltaTime;

            // 3. Güçlendirme süre takibi
            UpdatePowerUpTimers();

            // 4. Zorluk Kademesi (Tier) kontrolü
            CheckTierProgress();
        }

        public void SetState(GameState newState)
        {
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
                    if (WorldThemeManager.Instance != null)
                    {
                        WorldThemeManager.Instance.SyncFromSavedCharacter();
                        WorldThemeManager.Instance.SetStage(0);
                    }
                    comboCount = 0;
                    consecutivePassedObstacles = 0;
                    ResetPowerUps();
                    Obstacles.Obstacle.ClearAllActiveObstacles();
                    PowerUps.PowerUp.ClearAllActivePowerUps();
                    break;

                case GameState.GameOver:
                    Time.timeScale = 1.0f; // Varsa son patlama animasyonları için
                    int finalScoreInt = Mathf.FloorToInt(CurrentScore);
                    bool isNewRecord = SaveSystem.SaveScore(finalScoreInt);
                    if (isNewRecord)
                    {
                        HighScore = finalScoreInt;
                    }
                    break;
            }

            OnGameStateChanged?.Invoke(State);
        }

        public void StartGame()
        {
            SetState(GameState.Playing);
        }

        public void GameOver()
        {
            if (isShieldActive)
            {
                // Kalkan darbeyi emer
                isShieldActive = false;
                comboCount = 0; // Kalkan kullanıldığında kombo sıfırlanır
                consecutivePassedObstacles = 0;
                OnShieldStatusChanged?.Invoke();
                AudioManager.Instance?.PlayShieldHitSound();
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

            consecutivePassedObstacles++;
            if (consecutivePassedObstacles % 5 == 0)
            {
                comboCount++;
                int bonusPoints = 10;
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
            isSlowActive = true;
            slowTimer = duration;
            AudioManager.Instance?.PlayPowerUpSound();
        }

        public void ActivateDoubleScore(float duration = 5.0f)
        {
            isDoubleScoreActive = true;
            doubleScoreTimer = duration;
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

            // 3 Aşama: 0-79 (Aşama 1), 80-199 (Aşama 2), 200+ (Aşama 3)
            if (currentScoreInt >= 200)
            {
                calculatedTier = 2;
            }
            else if (currentScoreInt >= 80)
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
            return isSlowActive ? CurrentSpeed * 0.5f : CurrentSpeed;
        }
    }
}
