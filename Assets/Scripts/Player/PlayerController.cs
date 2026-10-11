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
        public float jumpForce = 12.0f;

        [Tooltip("Çift zıplama (daha kısa) gücü")]
        public float doubleJumpForce = 9.5f;

        [Tooltip("Yerçekimi ölçeği (daha seri düşüş için)")]
        public float gravityScale = 3.5f;

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

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = gravityScale;
            fixedXPosition = transform.position.x;

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

            // Sabit X pozisyonunu koru
            transform.position = new Vector3(fixedXPosition, transform.position.y, transform.position.z);

            // Zemin kontrolü
            CheckGroundedStatus();

            // Squash & Stretch esneme/yaylanma animasyonu
            if (squashTimer > 0f)
            {
                squashTimer -= Time.deltaTime * 4f;
                float sx = 1f + Mathf.Max(0f, squashTimer) * 0.22f;
                float sy = 1f - Mathf.Max(0f, squashTimer) * 0.22f;
                transform.localScale = new Vector3(sx, sy, 1f);
            }
            else
            {
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, Time.deltaTime * 8f);
            }

            // Dokunma (Mobile) veya Boşluk Tuşu (Keyboard) girişi
            if (IsJumpInputTriggered())
            {
                PerformJump();
            }
        }

        private void CheckGroundedStatus()
        {
            Vector2 checkPosition = groundCheckTransform != null ? (Vector2)groundCheckTransform.position : (Vector2)transform.position + Vector2.down * 0.5f;
            bool wasGrounded = isGrounded;
            isGrounded = Physics2D.OverlapCircle(checkPosition, groundCheckRadius, groundLayerMask);

            if (isGrounded && !wasGrounded)
            {
                // Yere değince çift zıplama hakkı sıfırlanır ve yere inişte esner
                canDoubleJump = true;
                squashTimer = 0.8f;
            }
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

        public void UpdateCharacterAppearance()
        {
            string characterId = SaveSystem.GetSelectedCharacterId("classic");
            CharacterData data = LoadCharacterData(characterId);

            if (data != null)
            {
                if (bodySpriteRenderer != null)
                {
                    bodySpriteRenderer.sprite = Utils.ProceduralSpriteGenerator.CreatePlayerSprite(data.bodyColor, data.glowColor, 64);
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

            if (other.CompareTag("Obstacle"))
            {
                if (GameManager.Instance.isShieldActive)
                {
                    // Kalkan aktif: Engeli yok et, kalkanı harca ve kısa dokunulmazlık ver
                    GameManager.Instance.GameOver(); // GameManager içinde isShieldActive kontrol edilip kaldırılır
                    StartCoroutine(GrantInvulnerability(0.3f));

                    // Çarpan engeli havuza iade et
                    Core.ObjectPooler.Instance?.ReturnToPool(other.gameObject.name.Replace("(Clone)", "").Trim(), other.gameObject);
                }
                else
                {
                    // Çarpışma: Oyun Biter
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
                // Başlangıç pozisyonuna sıfırla
                transform.position = new Vector3(fixedXPosition, 0f, 0f);
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
