using UnityEngine;
using ZiplaKac.Core;

namespace ZiplaKac.Obstacles
{
    /// <summary>
    /// Ekrandaki tekil engel nesnelerinin sağdan sola hareketini,
    /// yavaşlatma (Slow) gücünü ve oyuncu tarafından geçildiğinde kombo tetiklenmesini yöneten sınıf.
    /// </summary>
    public class Obstacle : MonoBehaviour
    {
        public string poolTag = "Cactus";
        public float offscreenX = -4.0f;

        private bool hasBeenPassed = false;
        private Transform playerTransform;
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            UpdateOffscreenBoundary();
        }

        private void OnEnable()
        {
            UpdateOffscreenBoundary();
            hasBeenPassed = false;
            ApplyThemeColor();

            if (playerTransform == null && GameManager.Instance != null)
            {
                Player.PlayerController player = FindObjectOfType<Player.PlayerController>();
                if (player != null) playerTransform = player.transform;
            }

            if (WorldThemeManager.Instance != null)
            {
                WorldThemeManager.Instance.OnWorldThemeChanged += HandleWorldThemeChanged;
            }
        }

        private void OnDisable()
        {
            if (WorldThemeManager.Instance != null)
            {
                WorldThemeManager.Instance.OnWorldThemeChanged -= HandleWorldThemeChanged;
            }
        }

        private void HandleWorldThemeChanged(string characterKey, int stageIndex)
        {
            ApplyThemeColor();
        }

        public void ApplyThemeColor()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) return;

            string charKey = "klasik";
            if (WorldThemeManager.Instance != null)
            {
                charKey = WorldThemeManager.Instance.CurrentCharacterKey;
            }

            // poolTag eşlemesi:
            // Cactus -> bitki
            // Rock -> kaya
            // Spike -> diken
            // LowBird / HighBird -> ucan
            string kind = "kaya";
            if (poolTag.Contains("Cactus")) kind = "bitki";
            else if (poolTag.Contains("Rock")) kind = "kaya";
            else if (poolTag.Contains("Spike")) kind = "diken";
            else if (poolTag.Contains("Bird")) kind = "ucan";

            string spritePath = $"Sprites/Obstacles/engel_{charKey}_{kind}";
            Sprite themedSprite = Resources.Load<Sprite>(spritePath);

            if (themedSprite != null)
            {
                spriteRenderer.sprite = themedSprite;
                spriteRenderer.color = Color.white;
            }
            else
            {
                if (WorldThemeManager.Instance != null)
                {
                    var palette = WorldThemeManager.Instance.GetCurrentPalette();
                    bool isAir = poolTag.Contains("Bird");
                    spriteRenderer.color = isAir ? palette.airObstacleColor : palette.groundObstacleColor;
                }
            }
        }

        private void UpdateOffscreenBoundary()
        {
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            float halfWidth = 5.0f * aspect;
            offscreenX = -halfWidth - 1.5f;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
            {
                return;
            }

            // GameManager'dan Slow etki çarpanı uyarlanmış etkin hızı al
            float speed = GameManager.Instance.GetEffectiveSpeed();
            transform.Translate(Vector3.left * speed * Time.deltaTime);

            // Oyuncu engeli başarıyla geçti mi kontrolü (Kombo hesabı)
            if (!hasBeenPassed && playerTransform != null && transform.position.x < playerTransform.position.x)
            {
                hasBeenPassed = true;
                GameManager.Instance.OnObstacleCleared(transform.position);
            }

            // Ekranın solundan çıkınca nesne havuzuna iade et
            if (transform.position.x < offscreenX)
            {
                RecycleToPool();
            }
        }

        public void RecycleToPool()
        {
            if (ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance.ReturnToPool(poolTag, gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public static void ClearAllActiveObstacles()
        {
            Obstacle[] all = FindObjectsOfType<Obstacle>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].gameObject.activeSelf)
                {
                    all[i].RecycleToPool();
                }
            }
        }
    }
}
