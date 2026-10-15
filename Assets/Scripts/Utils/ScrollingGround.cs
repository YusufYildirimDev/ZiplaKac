using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZiplaKac.Core;

namespace ZiplaKac.Utils
{
    /// <summary>
    /// Zıpla Kaç için oyun hızıyla eşzamanlı olarak sağdan sola kayan sonsuz zemin (seamless scrolling ground) sistemi.
    /// 4 zorluk kademesi (Gündüz, Günbatımı, Alacakaranlık, Gece) için özel zemin dokularını yönetir.
    /// </summary>
    public class ScrollingGround : MonoBehaviour
    {
        public static ScrollingGround Instance { get; private set; }

        [Header("Zemin Karoları")]
        public Transform[] tiles;
        public SpriteRenderer[] renderers;

        [Header("Kademe Zemin Sprite'ları (Tier 0-3)")]
        public Sprite[] tierSprites = new Sprite[4];

        [Header("Paralaks & Karo Ölçüleri")]
        public float tileWidth = 6.8266f;

        private int currentTier = 0;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTierChanged += HandleTierChanged;
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTierChanged -= HandleTierChanged;
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                HandleTierChanged(GameManager.Instance.CurrentTierIndex);
            }
            else
            {
                SetTier(0);
            }
            ResetPositions();
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.Playing || state == GameState.MainMenu)
            {
                ResetPositions();
            }
        }

        private void Update()
        {
            float effectiveSpeed = 0f;
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Playing)
            {
                effectiveSpeed = GameManager.Instance.GetEffectiveSpeed();
            }

            if (effectiveSpeed <= 0.001f || tiles == null || tiles.Length < 2) return;

            float move = effectiveSpeed * Time.deltaTime;

            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] != null)
                {
                    tiles[i].position += Vector3.left * move;
                }
            }

            // Kamera sol kenarını dinamik hesapla (tüm en-boy oranlarıyla uyumlu)
            float halfCamWidth = 3.5f;
            if (Camera.main != null && Camera.main.orthographic)
            {
                halfCamWidth = Camera.main.orthographicSize * Camera.main.aspect;
            }
            // Karo tamamen ekranın solundan çıktığında sıfırla
            float resetThreshold = -halfCamWidth - (tileWidth * 0.5f) - 0.5f;

            // Karoların sonsuz döngü kontrolü
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] != null && tiles[i].position.x <= resetThreshold)
                {
                    // En sağdaki karoyu bul
                    float maxX = float.MinValue;
                    for (int j = 0; j < tiles.Length; j++)
                    {
                        if (tiles[j] != null && j != i && tiles[j].position.x > maxX)
                        {
                            maxX = tiles[j].position.x;
                        }
                    }

                    if (maxX > float.MinValue)
                    {
                        tiles[i].position = new Vector3(maxX + tileWidth - 0.005f, tiles[i].position.y, tiles[i].position.z);
                    }
                }
            }
        }

        public void HandleTierChanged(int tierIndex)
        {
            SetTier(tierIndex);
        }

        public void SetTier(int tierIndex)
        {
            currentTier = Mathf.Clamp(tierIndex, 0, 3);
            if (tierSprites != null && currentTier < tierSprites.Length && tierSprites[currentTier] != null)
            {
                Sprite targetSprite = tierSprites[currentTier];
                if (renderers != null)
                {
                    foreach (var r in renderers)
                    {
                        if (r != null) r.sprite = targetSprite;
                    }
                }
                UpdateTileWidth();
            }
        }

        public void UpdateTileWidth()
        {
            if (tierSprites != null && currentTier < tierSprites.Length && tierSprites[currentTier] != null)
            {
                Sprite targetSprite = tierSprites[currentTier];
                if (targetSprite.pixelsPerUnit > 0)
                {
                    tileWidth = targetSprite.rect.width / targetSprite.pixelsPerUnit;
                }
            }
        }

        /// <summary>
        /// Tüm zemin karolarını ekranı başlangıç anında sıfır boşlukla kaplayacak şekilde sırayla konumlandırır.
        /// </summary>
        public void ResetPositions()
        {
            UpdateTileWidth();
            if (tiles == null || tiles.Length == 0) return;

            // Karoları sırayla sol tampondan (-tileWidth) başlayarak sağa doğru diz
            // Tile 0: -tileWidth (sol kenar koruması)
            // Tile 1: 0 (ekran ortası)
            // Tile 2: +tileWidth (ekran sağı)
            // Tile 3: +2*tileWidth (sağ bekleme alanı)
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] != null)
                {
                    float startX = (i - 1) * tileWidth;
                    tiles[i].localPosition = new Vector3(startX, 0f, 0f);
                }
            }
        }
    }
}
