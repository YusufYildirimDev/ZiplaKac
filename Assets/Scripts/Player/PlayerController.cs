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
        public float jumpForce = 11.5f;

        [Tooltip("Çift zıplama (daha kısa) gücü")]
        public float doubleJumpForce = 9.5f;

        [Tooltip("Yerçekimi ölçeği (daha seri ve tok düşüş için)")]
        public float gravityScale = 2.8f;

        [Header("Zemin Algılama Ayarları")]
        public Transform groundCheckTransform;
        public float groundCheckRadius = 0.2f;
        public LayerMask groundLayerMask;

        [Header("Görsel Bileşenler")]
        public SpriteRenderer bodySpriteRenderer;
        public SpriteRenderer glowSpriteRenderer;
        public GameObject shieldRingObject;
        public SpriteRenderer shieldRingSpriteRenderer;

        // Durum değişkenleri
        private Rigidbody2D rb;
        private bool isGrounded = false;
        private bool canDoubleJump = false;
        private bool isInvulnerable = false;

        private float fixedXPosition;

        public static PlayerController Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = gravityScale;

            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            float halfWidth = 5.0f * aspect;
            fixedXPosition = -halfWidth + (halfWidth * 2f) * 0.22f;
            transform.position = new Vector3(fixedXPosition, -2.5f, 0f);

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
            // Sabit X pozisyonunu daima koru
            transform.position = new Vector3(fixedXPosition, transform.position.y, transform.position.z);

            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
            {
                return;
            }

            // Zemin kontrolü
            CheckGroundedStatus();

            // Squash & Stretch esneme/yaylanma animasyonu (index.html oranı: 0.18f)
            if (squashTimer > 0f)
            {
                squashTimer -= Time.deltaTime * 4.0f;
                float sq = Mathf.Max(0f, squashTimer);
                float sx = 1f + sq * 0.18f;
                float sy = 1f - sq * 0.18f;
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

        private void CheckGroundedStatus()
        {
            Vector2 checkPosition = groundCheckTransform != null ? (Vector2)groundCheckTransform.position : (Vector2)transform.position + Vector2.down * 0.45f;
            bool wasGrounded = isGrounded;
            
            Collider2D[] hits = Physics2D.OverlapCircleAll(checkPosition, groundCheckRadius, groundLayerMask);
            bool foundGround = false;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].gameObject != gameObject && !hits[i].isTrigger)
                {
                    foundGround = true;
                    break;
                }
            }
            isGrounded = foundGround;

            if (isGrounded && !wasGrounded)
            {
                // Yere değince çift zıplama hakkı sıfırlanır, yaylanır ve toz parçacıkları çıkar
                canDoubleJump = true;
                squashTimer = 1.0f;
                SpawnLandingDust(checkPosition);
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

        private void PerformJump()
        {
            if (isGrounded)
            {
                // İlk Zıplama
                rb.linearVelocity = new Vector2(0f, jumpForce);
                canDoubleJump = true;
                squashTimer = 1.0f;
                AudioManager.Instance?.PlayJumpSound();
            }
            else if (canDoubleJump)
            {
                // Çift Zıplama (Havadayken 2. daha kısa zıplama)
                rb.linearVelocity = new Vector2(0f, doubleJumpForce);
                canDoubleJump = false;
                squashTimer = 1.0f;
                AudioManager.Instance?.PlayDoubleJumpSound();
            }
        }

        public void Jump()
        {
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Playing)
            {
                PerformJump();
            }
        }

        public void UpdateCharacterAppearance()
        {
            string characterId = SaveSystem.GetSelectedCharacterId("classic");
            CharacterData data = LoadCharacterData(characterId);

            if (data != null)
            {
                if (bodySpriteRenderer != null)
                {
                    bodySpriteRenderer.sprite = Utils.ProceduralSpriteGenerator.CreatePlayerSprite(data.bodyColor, data.glowColor, 96);
                    bodySpriteRenderer.color = Color.white; // Renk sprite içine çizildi
                }
                if (glowSpriteRenderer != null) glowSpriteRenderer.color = data.glowColor;
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
            if (newState == GameState.Playing)
            {
                // Başlangıç pozisyonuna sıfırla (Zeminin üzerinden başlar)
                float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
                float halfWidth = 5.0f * aspect;
                fixedXPosition = -halfWidth + (halfWidth * 2f) * 0.22f;

                transform.position = new Vector3(fixedXPosition, -2.5f, 0f);
                rb.linearVelocity = Vector2.zero;
                canDoubleJump = true;
                isInvulnerable = false;
                UpdateCharacterAppearance();
                UpdateShieldVisual();
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
