using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZiplaKac.Core;

namespace ZiplaKac.Player
{
    /// <summary>
    /// Karakterin hareketini, zıplama ve çift zıplama (double jump) fiziğini,
    /// zemin algılamasını ve kalkan halkası efektini yöneten kontrolcü.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Zıplama Fizik Ayarları")]
        [Tooltip("İlk zıplama gücü")]
        public float jumpForce = 11.2f;

        [Tooltip("Çift zıplama gücü")]
        public float doubleJumpForce = 9.8f;

        [Tooltip("Yerçekimi ölçeği (akıcı ve tok düşüş)")]
        public float gravityScale = 2.7f;

        [Header("Zemin Algılama Ayarları")]
        public Transform groundCheckTransform;
        public float groundCheckRadius = 0.15f;
        public LayerMask groundLayerMask;

        [Header("Görsel Bileşenler")]
        public SpriteRenderer bodySpriteRenderer;
        public SpriteRenderer glowSpriteRenderer;
        public GameObject shieldRingObject;
        public SpriteRenderer shieldRingSpriteRenderer;

        [Header("Kıyafet / Aksesuar Katmanları")]
        public SpriteRenderer capeSpriteRenderer;
        public SpriteRenderer glassesSpriteRenderer;
        public SpriteRenderer accessorySpriteRenderer;
        public SpriteRenderer hatSpriteRenderer;

        // Durum değişkenleri
        private Rigidbody2D rb;
        private bool isGrounded = true;
        private int jumpsUsed = 0;
        private int lastJumpFrame = -1;
        private float lastJumpTime = -1f;
        private bool isInvulnerable = false;

        public const float FIXED_PLAYER_X = -1.35f;
        public const float GROUND_FLOOR_Y = -2.96f;

        public static PlayerController Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = gravityScale;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            transform.position = new Vector3(FIXED_PLAYER_X, GROUND_FLOOR_Y, 0f);
            rb.simulated = false;

            try
            {
                UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
            }
            catch { }
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
                GameManager.Instance.OnShieldStatusChanged += UpdateShieldVisual;
            }

            UpdateCharacterAppearance();
            UpdateShieldVisual();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
                GameManager.Instance.OnShieldStatusChanged -= UpdateShieldVisual;
            }
        }

        private float squashTimer = 0f;

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
            {
                return;
            }

            // Squash & Stretch esneme/yaylanma animasyonu
            if (squashTimer > 0f)
            {
                squashTimer -= Time.deltaTime * 5.0f;
                float sq = Mathf.Max(0f, squashTimer);
                float sx = 1f + sq * 0.15f;
                float sy = 1f - sq * 0.15f;
                transform.localScale = new Vector3(sx, sy, 1f);
            }
            else
            {
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, Time.deltaTime * 10f);
            }

            // Dokunma (Mobile) veya Boşluk Tuşu (Keyboard) girişi
            if (IsJumpInputTriggered())
            {
                PerformJump();
            }
        }

        public void OnLanded()
        {
            if (!isGrounded || jumpsUsed > 0)
            {
                isGrounded = true;
                jumpsUsed = 0;
                squashTimer = 1.0f;
                SpawnLandingDust(transform.position + Vector3.down * 0.22f);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.name.Contains("Ground") || collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                if (Time.time - lastJumpTime > 0.10f)
                {
                    OnLanded();
                }
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (collision.gameObject.name.Contains("Ground") || collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                if (rb.linearVelocity.y <= 0.1f && Time.time - lastJumpTime > 0.15f)
                {
                    isGrounded = true;
                    jumpsUsed = 0;
                }
            }
        }

        private void SpawnLandingDust(Vector2 groundPos)
        {
            for (int i = 0; i < 6; i++)
            {
                GameObject dust = new GameObject("LandingDust");
                dust.transform.position = new Vector3(groundPos.x + Random.Range(-0.15f, 0.15f), groundPos.y + 0.05f, 0f);
                dust.transform.localScale = Vector3.one * Random.Range(0.08f, 0.14f);

                SpriteRenderer sr = dust.AddComponent<SpriteRenderer>();
                sr.sprite = Utils.ProceduralSpriteGenerator.CreateCircleSprite(new Color(0.35f, 0.45f, 0.7f, 0.8f), 32);
                sr.sortingOrder = 4;
                Material safeMat = Utils.GameBootstrap.GetSafeSpriteMaterial();
                if (safeMat != null) sr.material = safeMat;

                Vector2 vel = new Vector2(Random.Range(-1.6f, 1.6f), Random.Range(1.0f, 2.6f));
                StartCoroutine(AnimateDust(dust, vel));
            }
        }

        private IEnumerator AnimateDust(GameObject dust, Vector2 vel)
        {
            float life = 0.35f;
            float elapsed = 0f;
            Vector3 pos = dust.transform.position;
            SpriteRenderer sr = dust.GetComponent<SpriteRenderer>();
            Color initCol = sr.color;

            while (elapsed < life && dust != null)
            {
                elapsed += Time.deltaTime;
                vel.y -= 12f * Time.deltaTime;
                pos += (Vector3)vel * Time.deltaTime;
                dust.transform.position = pos;

                float alpha = Mathf.Max(0f, 1f - (elapsed / life));
                sr.color = new Color(initCol.r, initCol.g, initCol.b, alpha * 0.75f);
                yield return null;
            }

            if (dust != null) Destroy(dust);
        }

        private bool IsJumpInputTriggered()
        {
            // 1. New Input System Kontrolü (iOS Dokunma, Pointer, Klavye)
            try
            {
                if (UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.enabled)
                {
                    var touches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
                    for (int i = 0; i < touches.Count; i++)
                    {
                        if (touches[i].phase == UnityEngine.InputSystem.TouchPhase.Began)
                        {
                            return true;
                        }
                    }
                }

                var ts = UnityEngine.InputSystem.Touchscreen.current;
                if (ts != null && ts.primaryTouch.press.wasPressedThisFrame)
                {
                    return true;
                }

                var ptr = UnityEngine.InputSystem.Pointer.current;
                if (ptr != null && ptr.press.wasPressedThisFrame)
                {
                    return true;
                }

                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame))
                {
                    return true;
                }
            }
            catch { }

            // 2. Legacy Input Kontrolü (Fallback)
            try
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow))
                {
                    return true;
                }

                if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                {
                    return true;
                }

                if (Input.GetMouseButtonDown(0))
                {
                    return true;
                }
            }
            catch { }

            return false;
        }

        public void PerformJump()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
            {
                return;
            }

            // Çift tetiklenme / release event / multi-source koruması (min 120ms aralık)
            if (Time.time - lastJumpTime < 0.12f) return;
            if (Time.frameCount == lastJumpFrame) return;

            if (jumpsUsed < 2)
            {
                lastJumpTime = Time.time;
                lastJumpFrame = Time.frameCount;

                string charKey = WorldThemeManager.Instance != null ? WorldThemeManager.Instance.CurrentCharacterKey : "klasik";
                bool hasDoubleJumpMaster = SaveSystem.GetUpgradeLevel(charKey, "double_jump") > 0;
                float currentDoubleJumpForce = hasDoubleJumpMaster ? doubleJumpForce * 1.15f : doubleJumpForce;

                float force = (jumpsUsed == 0) ? jumpForce : currentDoubleJumpForce;
                rb.linearVelocity = new Vector2(0f, force);
                isGrounded = false;
                jumpsUsed++;
                squashTimer = 1.0f;

                if (jumpsUsed == 1)
                {
                    AudioManager.Instance?.PlayJumpSound();
                }
                else
                {
                    AudioManager.Instance?.PlayDoubleJumpSound();
                }
            }
        }

        public void Jump()
        {
            PerformJump();
        }

        public void UpdateCharacterAppearance()
        {
            string characterId = SaveSystem.GetSelectedCharacterId("classic");
            CharacterData data = LoadCharacterData(characterId);

            if (data != null)
            {
                if (bodySpriteRenderer != null)
                {
                    bodySpriteRenderer.sprite = Utils.ProceduralSpriteGenerator.GetSpriteOrGenerate(
                        $"Sprites/{characterId}_hero",
                        () => Utils.ProceduralSpriteGenerator.CreatePlayerSprite(data.bodyColor, data.glowColor, 128)
                    );
                    bodySpriteRenderer.color = Color.white;
                }
                // Çamurlu kahverengi glow halkasını tamamen kapat
                if (glowSpriteRenderer != null)
                {
                    glowSpriteRenderer.gameObject.SetActive(false);
                }

                // Kıyafet ve Aksesuar Katmanlarını Güncelle
                UpdateOutfitVisuals(characterId);
            }
        }

        private void EnsureOutfitRenderers()
        {
            if (bodySpriteRenderer == null) return;

            int bodyOrder = bodySpriteRenderer.sortingOrder;
            int bodyLayer = bodySpriteRenderer.sortingLayerID;

            if (capeSpriteRenderer == null)
            {
                capeSpriteRenderer = GetOrCreateChildRenderer("Cape", bodyLayer, bodyOrder - 1);
            }
            if (glassesSpriteRenderer == null)
            {
                glassesSpriteRenderer = GetOrCreateChildRenderer("Glasses", bodyLayer, bodyOrder + 1);
            }
            if (accessorySpriteRenderer == null)
            {
                accessorySpriteRenderer = GetOrCreateChildRenderer("Accessory", bodyLayer, bodyOrder + 2);
            }
            if (hatSpriteRenderer == null)
            {
                hatSpriteRenderer = GetOrCreateChildRenderer("Hat", bodyLayer, bodyOrder + 3);
            }
        }

        private SpriteRenderer GetOrCreateChildRenderer(string childName, int sortingLayerID, int sortingOrder)
        {
            Transform child = transform.Find(childName);
            GameObject obj;
            if (child != null)
            {
                obj = child.gameObject;
            }
            else
            {
                obj = new GameObject(childName);
                obj.transform.SetParent(transform, false);
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localScale = Vector3.one;
                obj.transform.localRotation = Quaternion.identity;
            }

            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            if (sr == null) sr = obj.AddComponent<SpriteRenderer>();

            sr.sortingLayerID = sortingLayerID;
            sr.sortingOrder = sortingOrder;
            if (bodySpriteRenderer != null && bodySpriteRenderer.sharedMaterial != null)
            {
                sr.sharedMaterial = bodySpriteRenderer.sharedMaterial;
            }
            return sr;
        }

        private void UpdateOutfitVisuals(string characterId)
        {
            EnsureOutfitRenderers();
            string charKey = WorldThemeManager.NormalizeCharacterKey(characterId);
            bool outfitEnabled = SaveSystem.IsOutfitEnabled();

            // 1. Pelerin (Cape)
            if (capeSpriteRenderer != null)
            {
                Sprite sp = Resources.Load<Sprite>($"Sprites/Kiyafetler/kiyafet_{charKey}_pelerin");
                capeSpriteRenderer.sprite = sp;
                capeSpriteRenderer.color = Color.white;
                bool isEquipped = SaveSystem.IsOutfitEquipped(charKey, "pelerin");
                capeSpriteRenderer.gameObject.SetActive(outfitEnabled && isEquipped && sp != null);
            }

            // 2. Güneş Gözlüğü (Glasses)
            if (glassesSpriteRenderer != null)
            {
                Sprite sp = Resources.Load<Sprite>($"Sprites/Kiyafetler/kiyafet_{charKey}_gunes_gozlugu");
                glassesSpriteRenderer.sprite = sp;
                glassesSpriteRenderer.color = Color.white;
                bool isEquipped = SaveSystem.IsOutfitEquipped(charKey, "gunes_gozlugu");
                glassesSpriteRenderer.gameObject.SetActive(outfitEnabled && isEquipped && sp != null);
            }

            // 3. Özel Aksesuar (Accessory)
            if (accessorySpriteRenderer != null)
            {
                Sprite sp = Resources.Load<Sprite>($"Sprites/Kiyafetler/kiyafet_{charKey}_ozel_aksesuar");
                accessorySpriteRenderer.sprite = sp;
                accessorySpriteRenderer.color = Color.white;
                bool isEquipped = SaveSystem.IsOutfitEquipped(charKey, "ozel_aksesuar");
                accessorySpriteRenderer.gameObject.SetActive(outfitEnabled && isEquipped && sp != null);
            }

            // 4. Parti Şapkası (Hat)
            if (hatSpriteRenderer != null)
            {
                Sprite sp = Resources.Load<Sprite>($"Sprites/Kiyafetler/kiyafet_{charKey}_parti_sapkasi");
                hatSpriteRenderer.sprite = sp;
                hatSpriteRenderer.color = Color.white;
                bool isEquipped = SaveSystem.IsOutfitEquipped(charKey, "parti_sapkasi");
                hatSpriteRenderer.gameObject.SetActive(outfitEnabled && isEquipped && sp != null);
            }
        }

        private CharacterData LoadCharacterData(string id)
        {
            CharacterData data = ScriptableObject.CreateInstance<CharacterData>();
            data.characterId = id;

            switch (id)
            {
                case "fire":
                    data.characterName = "Ateş";
                    data.bodyColor = new Color(1.0f, 0.42f, 0.21f); // #ff6b35
                    data.glowColor = new Color(1.0f, 0.69f, 0.6f, 0.8f);
                    break;
                case "ice":
                    data.characterName = "Buz";
                    data.bodyColor = new Color(0.3f, 0.79f, 0.94f); // #4cc9f0
                    data.glowColor = new Color(0.74f, 0.88f, 0.99f, 0.8f);
                    break;
                case "poison":
                case "toxic":
                    data.characterName = "Zehir";
                    data.bodyColor = new Color(0.49f, 0.95f, 0.61f); // #7cf29c
                    data.glowColor = new Color(0.78f, 1.0f, 0.85f, 0.8f);
                    break;
                case "night":
                case "shadow":
                    data.characterName = "Gece";
                    data.bodyColor = new Color(0.61f, 0.36f, 0.9f); // #9b5de5
                    data.glowColor = new Color(0.85f, 0.71f, 0.99f, 0.8f);
                    break;
                default: // classic
                    data.characterName = "Klasik";
                    data.bodyColor = new Color(1.0f, 0.82f, 0.4f); // #ffd166
                    data.glowColor = new Color(1.0f, 0.91f, 0.66f, 0.8f);
                    break;
            }

            return data;
        }

        public void UpdateShieldVisual()
        {
            bool hasShield = GameManager.Instance != null && GameManager.Instance.isShieldActive;
            if (shieldRingObject != null)
            {
                shieldRingObject.SetActive(hasShield);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isInvulnerable || GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
            {
                return;
            }

            bool isObstacle = other.CompareTag("Obstacle") || other.GetComponent<Obstacles.Obstacle>() != null;
            if (isObstacle)
            {
                if (GameManager.Instance.isShieldActive)
                {
                    // Kalkan aktif: Engeli yok et, kalkanı harca ve kısa dokunulmazlık ver
                    UI.UIManager.Instance?.TriggerCameraShake(0.15f, 0.08f);
                    GameManager.Instance.GameOver(); // GameManager içinde isShieldActive kontrol edilip kaldırılır
                    StartCoroutine(GrantInvulnerability(0.3f));

                    // Çarpan engeli havuza iade et
                    Core.ObjectPooler.Instance?.ReturnToPool(other.gameObject.name.Replace("(Clone)", "").Trim(), other.gameObject);
                }
                else
                {
                    // Çarpışma: Oyun Biter
                    UI.UIManager.Instance?.TriggerCameraShake(0.24f, 0.14f);
                    GameManager.Instance.GameOver();
                }
            }
        }

        private IEnumerator GrantInvulnerability(float duration)
        {
            isInvulnerable = true;
            yield return new WaitForSeconds(duration);
            isInvulnerable = false;
        }

        private void HandleGameStateChanged(GameState newState)
        {
            transform.position = new Vector3(FIXED_PLAYER_X, GROUND_FLOOR_Y, 0f);
            rb.linearVelocity = Vector2.zero;
            jumpsUsed = 0;
            isGrounded = true;
            isInvulnerable = false;
            lastJumpTime = -1f;
            lastJumpFrame = -1;

            if (newState == GameState.Playing)
            {
                rb.simulated = true;
                UpdateCharacterAppearance();
                UpdateShieldVisual();
            }
            else
            {
                rb.simulated = false;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheckTransform != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(groundCheckTransform.position, groundCheckRadius);
            }
        }
    }
}
