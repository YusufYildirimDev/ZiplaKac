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
        public float tileWidth = 10.24f;
        public float resetThresholdX = -10.24f;

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
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTierChanged -= HandleTierChanged;
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

            // Karoların sonsuz döngü kontrolü
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] != null && tiles[i].position.x <= resetThresholdX)
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
            }
        }
    }
}
