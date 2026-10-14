using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using ZiplaKac.Core;
using ZiplaKac.Player;
using ZiplaKac.Obstacles;
using ZiplaKac.PowerUps;
using ZiplaKac.UI;

namespace ZiplaKac.Utils
{
    /// <summary>
    /// Boş bir Unity sahnesinde bile oyun nesnelerini, UI Canvas'ı, Oyuncuyu, Engel ve Power-up Spawner'larını
    /// çalışma zamanında (Runtime) tam donanımlı olarak oluşturan sıfır-konfigürasyon Bootstrap sınıfı.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoBootstrap()
        {
            GameObject bootstrapObj = new GameObject("[GameBootstrap]");
            bootstrapObj.AddComponent<GameBootstrap>();
            DontDestroyOnLoad(bootstrapObj);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void SanitizeEventSystem()
        {
            // Eski StandaloneInputModule'ları derhal devre dışı bırak ve yok et
            foreach (var mod in Object.FindObjectsOfType<UnityEngine.EventSystems.StandaloneInputModule>())
            {
                mod.enabled = false;
                Object.DestroyImmediate(mod);
            }

            var es = Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (es != null)
            {
                var inputModule = es.GetComponent<InputSystemUIInputModule>();
                if (inputModule == null)
                {
                    inputModule = es.gameObject.AddComponent<InputSystemUIInputModule>();
                }
                inputModule.AssignDefaultActions();
            }
        }

        private void Awake()
        {
            SetupCamera();
            SetupManagers();
            SetupBackground();
            SetupGround();
            SetupPlayer();
            SetupSpawnersAndPools();
            SetupUICanvas();
        }

        private void SetupCamera()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) mainCam = FindObjectOfType<Camera>();
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<Camera>();
            }

            mainCam.transform.position = new Vector3(0f, 0f, -10f);
            mainCam.orthographic = true;
            mainCam.orthographicSize = 5.0f;
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.05f, 0.07f, 0.13f); // #0d1220
        }

        private void SetupManagers()
        {
            if (GameManager.Instance == null)
            {
                GameObject gm = new GameObject("GameManager");
                gm.AddComponent<GameManager>();
            }

            if (ObjectPooler.Instance == null)
            {
                GameObject op = new GameObject("ObjectPooler");
                op.AddComponent<ObjectPooler>();
            }

            if (AudioManager.Instance == null)
            {
                GameObject am = new GameObject("AudioManager");
                am.AddComponent<AudioManager>();
            }

            if (MonetizationManager.Instance == null)
            {
                GameObject mm = new GameObject("MonetizationManager");
                mm.AddComponent<MonetizationManager>();
            }
        }

        private static Material s_safeSpriteMat;
        public static Material GetSafeSpriteMaterial()
        {
            if (s_safeSpriteMat != null) return s_safeSpriteMat;

            Shader s = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit");
            if (s == null) s = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (s == null) s = Shader.Find("Sprites/Default");
            if (s != null)
            {
                s_safeSpriteMat = new Material(s);
                return s_safeSpriteMat;
            }
            return null;
        }

        private void SetupBackground()
        {
            GameObject bgObj = GameObject.Find("GradientBackground");
            if (bgObj == null)
            {
                bgObj = new GameObject("GradientBackground");
                bgObj.transform.position = new Vector3(0f, 0f, 5f);
                bgObj.transform.localScale = new Vector3(20f, 16f, 1f);

                SpriteRenderer bgSr = bgObj.AddComponent<SpriteRenderer>();
                bgSr.sprite = ProceduralSpriteGenerator.CreateGradientSkySprite(new Color(0.12f, 0.16f, 0.29f), new Color(0.05f, 0.07f, 0.13f));
                bgSr.sortingOrder = -10;
                Material safeMat = GetSafeSpriteMaterial();
                if (safeMat != null) bgSr.material = safeMat;

                if (UIManager.Instance != null) UIManager.Instance.gradientBgRenderer = bgSr;
            }

            // Tier 3 yıldızlı gece için yıldızlar
            GameObject starsObj = GameObject.Find("StarsContainer");
            if (starsObj == null)
            {
                starsObj = new GameObject("StarsContainer");
                starsObj.transform.position = new Vector3(0f, 0f, 4f);

                for (int i = 0; i < 18; i++)
                {
                    GameObject star = new GameObject($"Star_{i}");
                    star.transform.SetParent(starsObj.transform, false);

                    float sx = -3.5f + (i * 2.37f) % 7.0f;
                    float sy = -0.5f + (i * 1.83f) % 4.8f;
                    star.transform.position = new Vector3(sx, sy, 4f);
                    star.transform.localScale = Vector3.one * (0.08f + (i % 3) * 0.04f);

                    SpriteRenderer starSr = star.AddComponent<SpriteRenderer>();
                    float alpha = 0.35f + (i % 3) * 0.25f;
                    starSr.sprite = ProceduralSpriteGenerator.CreateCircleSprite(new Color(1f, 1f, 1f, alpha), 16);
                    starSr.sortingOrder = -9;
                    Material safeMat = GetSafeSpriteMaterial();
                    if (safeMat != null) starSr.material = safeMat;
                }

                starsObj.SetActive(false);
                if (UIManager.Instance != null) UIManager.Instance.starsContainer = starsObj;
            }
        }

        private void SetupGround()
        {
            GameObject ground = GameObject.Find("Ground");
            if (ground == null)
            {
                ground = new GameObject("Ground");
                ground.transform.position = new Vector3(0f, -3.20f, 0f);
                ground.transform.localScale = new Vector3(4f, 1f, 1f);

                SpriteRenderer sr = ground.AddComponent<SpriteRenderer>();
                sr.sprite = ProceduralSpriteGenerator.CreateGroundSprite(new Color(0.17f, 0.23f, 0.39f)); // #2b3a63 (Modern Zemin)
                sr.sortingOrder = 2;
                Material safeMat = GetSafeSpriteMaterial();
                if (safeMat != null) sr.material = safeMat;

                BoxCollider2D col = ground.AddComponent<BoxCollider2D>();
                col.size = new Vector2(40f, 2f);
                col.offset = new Vector2(0f, -1.0f); // Üst yüzey Y = -3.20f

                int groundLayer = LayerMask.NameToLayer("Ground");
                if (groundLayer != -1) ground.layer = groundLayer;

                if (UIManager.Instance != null) UIManager.Instance.groundRenderer = sr;
            }
        }

        private void SetupPlayer()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                player = new GameObject("Player");
                try { player.tag = "Player"; } catch { }

                player.transform.position = new Vector3(PlayerController.FIXED_PLAYER_X, PlayerController.GROUND_FLOOR_Y, 0f);

                Material safeMat = GetSafeSpriteMaterial();

                // Gövde Görseli (Cute rounded square with eyes & catchlight)
                SpriteRenderer bodySr = player.AddComponent<SpriteRenderer>();
                bodySr.sprite = ProceduralSpriteGenerator.CreatePlayerSprite(new Color(1.0f, 0.82f, 0.4f), new Color(1.0f, 0.91f, 0.66f, 0.8f), 128);
                bodySr.sortingOrder = 10;
                if (safeMat != null) bodySr.material = safeMat;

                // Kalkan Halkası (Shield Ring) Alt Nesnesi
                GameObject shieldObj = new GameObject("ShieldRing");
                shieldObj.transform.SetParent(player.transform, false);
                shieldObj.transform.localScale = Vector3.one * 1.35f;
                SpriteRenderer shieldSr = shieldObj.AddComponent<SpriteRenderer>();
                shieldSr.sprite = ProceduralSpriteGenerator.CreateRingSprite(new Color(0.30f, 0.79f, 0.94f), 96, 0.84f);
                shieldSr.sortingOrder = 11;
                if (safeMat != null) shieldSr.material = safeMat;
                shieldObj.SetActive(false);

                // Zemin Kontrol Noktası (GroundCheck)
                GameObject groundCheckObj = new GameObject("GroundCheck");
                groundCheckObj.transform.SetParent(player.transform, false);
                groundCheckObj.transform.localPosition = new Vector3(0f, -0.22f, 0f);

                // Fizik & Kontrolcü (PPU 200 karakter boyutu 0.52x0.52 ile uyumlu BoxCollider2D)
                BoxCollider2D col = player.AddComponent<BoxCollider2D>();
                col.size = new Vector2(0.44f, 0.44f);

                Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
                rb.gravityScale = 2.8f;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                PlayerController pc = player.AddComponent<PlayerController>();
                pc.bodySpriteRenderer = bodySr;
                pc.shieldRingObject = shieldObj;
                pc.shieldRingSpriteRenderer = shieldSr;
                pc.groundCheckTransform = groundCheckObj.transform;

                int groundLayer = LayerMask.NameToLayer("Ground");
                if (groundLayer != -1) pc.groundLayerMask = 1 << groundLayer;
                else pc.groundLayerMask = ~0;
            }
        }

        private void SetupSpawnersAndPools()
        {
            // Obstacle Spawner
            GameObject obsSpawnerObj = GameObject.Find("ObstacleSpawner");
            if (obsSpawnerObj == null)
            {
                obsSpawnerObj = new GameObject("ObstacleSpawner");
                obsSpawnerObj.AddComponent<ObstacleSpawner>();
            }

            // PowerUp Spawner
            GameObject puSpawnerObj = GameObject.Find("PowerUpSpawner");
            if (puSpawnerObj == null)
            {
                puSpawnerObj = new GameObject("PowerUpSpawner");
                puSpawnerObj.AddComponent<PowerUpSpawner>();
            }

            // Prefab Havuzlarını Nesne Havuzuna Kaydet
            RegisterDefaultPrefabPools();
        }

        private void RegisterDefaultPrefabPools()
        {
            if (ObjectPooler.Instance == null) return;

            // 1. Kaktüs (Cactus) - Doğal orantı (Scale 1,1,1)
            GameObject cactusPrefab = CreateObstaclePrefab("Cactus");
            ObjectPooler.Instance.RegisterPool("Cactus", cactusPrefab, 8);

            // 2. Kaya (Rock) - Doğal orantı (Scale 1,1,1)
            GameObject rockPrefab = CreateObstaclePrefab("Rock");
            ObjectPooler.Instance.RegisterPool("Rock", rockPrefab, 8);

            // 3. Diken (Spike) - Doğal orantı (Scale 1,1,1)
            GameObject spikePrefab = CreateObstaclePrefab("Spike");
            ObjectPooler.Instance.RegisterPool("Spike", spikePrefab, 8);

            // 4. Alçak Kuş (LowBird) - Doğal orantı (Scale 1,1,1)
            GameObject lowBirdPrefab = CreateObstaclePrefab("LowBird");
            ObjectPooler.Instance.RegisterPool("LowBird", lowBirdPrefab, 6);

            // 5. Yüksek Kuş (HighBird) - Doğal orantı (Scale 1,1,1)
            GameObject highBirdPrefab = CreateObstaclePrefab("HighBird");
            ObjectPooler.Instance.RegisterPool("HighBird", highBirdPrefab, 6);

            // Power-up Prefabları (index.html renkleri: #4cc9f0, #90e0ef, #ffd60a)
            GameObject pShield = CreatePowerUpPrefab("PowerUp_Shield", PowerUpType.Shield, new Color(0.30f, 0.79f, 0.94f), "S");
            ObjectPooler.Instance.RegisterPool("PowerUp_Shield", pShield, 3);

            GameObject pSlow = CreatePowerUpPrefab("PowerUp_Slow", PowerUpType.Slow, new Color(0.56f, 0.88f, 0.94f), "T");
            ObjectPooler.Instance.RegisterPool("PowerUp_Slow", pSlow, 3);

            GameObject pMulti = CreatePowerUpPrefab("PowerUp_Multi", PowerUpType.ScoreMultiplier, new Color(1.0f, 0.84f, 0.04f), "2X");
            ObjectPooler.Instance.RegisterPool("PowerUp_Multi", pMulti, 3);

            // Floating Text Prefabı
            GameObject ftPrefab = CreateFloatingTextPrefab();
            ObjectPooler.Instance.RegisterPool("FloatingText", ftPrefab, 5);
        }

        private GameObject CreateObstaclePrefab(string name)
        {
            GameObject obj = new GameObject(name);
            try { obj.tag = "Obstacle"; } catch { }

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;
            Material safeMat = GetSafeSpriteMaterial();
            if (safeMat != null) sr.material = safeMat;

            Color obsColor = new Color(0.94f, 0.28f, 0.44f); // #ef476f (HTML Obstacle Coral)
            Color airColor = new Color(0.35f, 0.28f, 0.5f);  // #30264a (HTML Air Bird Violet)

            BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            if (name == "Cactus")
            {
                sr.sprite = ProceduralSpriteGenerator.CreateCactusSprite(obsColor);
                col.size = new Vector2(0.34f, 0.54f);
                col.offset = new Vector2(0f, 0.27f);
            }
            else if (name == "Rock")
            {
                sr.sprite = ProceduralSpriteGenerator.CreateRockSprite(obsColor);
                col.size = new Vector2(0.48f, 0.30f);
                col.offset = new Vector2(0f, 0.15f);
            }
            else if (name == "Spike")
            {
                sr.sprite = ProceduralSpriteGenerator.CreateSpikeSprite(obsColor);
                col.size = new Vector2(0.28f, 0.72f);
                col.offset = new Vector2(0f, 0.36f);
            }
            else if (name.Contains("Bird"))
            {
                sr.sprite = ProceduralSpriteGenerator.CreateBirdSprite(airColor);
                col.size = new Vector2(0.40f, 0.26f);
                col.offset = Vector2.zero;
            }

            obj.transform.localScale = Vector3.one;

            Obstacle obs = obj.AddComponent<Obstacle>();
            obs.poolTag = name;

            obj.SetActive(false);
            return obj;
        }

        private GameObject CreatePowerUpPrefab(string name, PowerUpType type, Color color, string label)
        {
            GameObject obj = new GameObject(name);
            try { obj.tag = "PowerUp"; } catch { }

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreatePowerUpOrb(color, label);
            sr.sortingOrder = 7;
            Material safeMat = GetSafeSpriteMaterial();
            if (safeMat != null) sr.material = safeMat;

            CircleCollider2D col = obj.AddComponent<CircleCollider2D>();
            col.radius = 0.22f;
            col.isTrigger = true;

            obj.transform.localScale = Vector3.one;

            PowerUp p = obj.AddComponent<PowerUp>();
            p.powerUpType = type;

            obj.SetActive(false);
            return obj;
        }

        private static Font GetSafeDefaultFont()
        {
            Font font = null;
            try
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch { }

            if (font == null)
            {
                try
                {
                    font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                catch { }
            }

            if (font == null)
            {
                Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
                if (fonts != null && fonts.Length > 0) font = fonts[0];
            }

            return font;
        }

        private GameObject CreateFloatingTextPrefab()
        {
            GameObject canvasObj = new GameObject("FloatingTextCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.transform.localScale = Vector3.one * 0.02f;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(canvasObj.transform, false);

            Text txt = textObj.AddComponent<Text>();
            txt.font = GetSafeDefaultFont();
            txt.fontSize = 36;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.yellow;

            FloatingText ft = canvasObj.AddComponent<FloatingText>();
            ft.textComponent = txt;

            canvasObj.SetActive(false);
            return canvasObj;
        }

        private void SetupUICanvas()
        {
            GameObject canvasObj = GameObject.Find("UICanvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("UICanvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);

                canvasObj.AddComponent<GraphicRaycaster>();
                canvasObj.AddComponent<UIManager>();

                // 1. Ana Menü Paneli
                CreateMainMenuPanel(canvasObj.transform);

                // 2. HUD Paneli
                CreateHUDPanel(canvasObj.transform);

                // 3. Oyun Bitti Paneli
                CreateGameOverPanel(canvasObj.transform);
            }

            // EventSystem Kontrolü (iOS & New Input System tam uyumluluk)
            foreach (var mod in FindObjectsOfType<UnityEngine.EventSystems.StandaloneInputModule>())
            {
                mod.enabled = false;
                DestroyImmediate(mod);
            }

            UnityEngine.EventSystems.EventSystem es = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                es = esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            }

            var uiMod = es.GetComponent<InputSystemUIInputModule>();
            if (uiMod == null)
            {
                uiMod = es.gameObject.AddComponent<InputSystemUIInputModule>();
            }
            uiMod.AssignDefaultActions();
        }

        private static GameObject CreateCardContainer(Transform parent, string name, Vector2 size)
        {
            GameObject card = new GameObject(name);
            card.transform.SetParent(parent, false);
            RectTransform rt = card.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;

            Image img = card.AddComponent<Image>();
            // #141a30 (%88 alpha) + subtle border (%14 alpha)
            img.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(
                new Color(0.08f, 0.10f, 0.19f, 0.88f),
                new Color(1f, 1f, 1f, 0.14f),
                2f, 24, 128, 128);
            img.type = Image.Type.Sliced;
            img.raycastTarget = true;

            return card;
        }

        private static GameObject CreatePillButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Color bgColor, Color textColor, int fontSize = 32)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(bgColor, Color.clear, 0f, 24, 128, 64);
            img.type = Image.Type.Sliced;
            img.raycastTarget = true;

            Button btn = obj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            btn.colors = colors;

            Text labelTxt = CreateText(obj.transform, "Label", label, fontSize, Vector2.zero, textColor, false, FontStyle.Bold);
            RectTransform labelRt = labelTxt.GetComponent<RectTransform>();
            labelRt.sizeDelta = size;

            return obj;
        }

        private void CreateMainMenuPanel(Transform parent)
        {
            GameObject panel = new GameObject("MainMenuPanel");
            panel.transform.SetParent(parent, false);
            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.45f);

            // Centered Glassmorphic Card (760x760, Y: +130 so player is clearly visible below)
            GameObject card = CreateCardContainer(panel.transform, "MenuCard", new Vector2(760f, 760f));
            card.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 130f);

            // Başlık "ZIPLA KAÇ"
            CreateText(card.transform, "TitleText", "ZIPLA KAÇ", 54, new Vector2(0f, 275f), new Color(1.0f, 0.82f, 0.4f), false, FontStyle.Bold); // #ffd166

            // Alt Başlık "tek dokunuş, sonsuz kaçış"
            CreateText(card.transform, "SubtitleText", "tek dokunuş, sonsuz kaçış", 24, new Vector2(0f, 220f), new Color(0.6f, 0.7f, 0.85f), false);

            // "KARAKTER SEÇ" etiketi
            CreateText(card.transform, "SelectPrompt", "KARAKTER SEÇ", 20, new Vector2(0f, 125f), new Color(0.75f, 0.82f, 0.95f), false, FontStyle.Bold);

            // Karakter Galerisi UI (Swatches)
            GameObject galleryObj = new GameObject("CharacterGallery");
            galleryObj.transform.SetParent(card.transform, false);
            RectTransform galleryRt = galleryObj.AddComponent<RectTransform>();
            galleryRt.anchoredPosition = new Vector2(0f, 20f);
            galleryRt.sizeDelta = new Vector2(700f, 130f);

            galleryObj.AddComponent<CharacterSelectorUI>();

            // Neon Mint "BAŞLA" Butonu (#06d6a0)
            Color mintColor = new Color(0.024f, 0.839f, 0.627f);
            Color darkNavy = new Color(0.05f, 0.07f, 0.13f);
            GameObject btnObj = CreatePillButton(card.transform, "StartButton", "BAŞLA", new Vector2(0f, -165f), new Vector2(400f, 84f), mintColor, darkNavy, 32);
            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => {
                if (GameManager.Instance != null) GameManager.Instance.StartGame();
            });

            // Alt İpucu
            CreateText(card.transform, "ControlHint", "Zıplamak için dokun / Boşluk", 20, new Vector2(0f, -285f), new Color(0.55f, 0.65f, 0.8f), false);

            if (UIManager.Instance != null) UIManager.Instance.mainMenuPanel = panel;
        }

        private void CreateHUDPanel(Transform parent)
        {
            GameObject panel = new GameObject("HUDPanel");
            panel.transform.SetParent(parent, false);
            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            // 1. Fullscreen Jump Trigger (Ekrana herhangi bir yere dokunulduğunda anında zıplama)
            GameObject jumpTrigger = new GameObject("FullscreenJumpTrigger");
            jumpTrigger.transform.SetParent(panel.transform, false);
            RectTransform jtRt = jumpTrigger.AddComponent<RectTransform>();
            jtRt.anchorMin = Vector2.zero; jtRt.anchorMax = Vector2.one;
            jtRt.offsetMin = Vector2.zero; jtRt.offsetMax = Vector2.zero;
            Image jtImg = jumpTrigger.AddComponent<Image>();
            jtImg.color = Color.clear;
            jtImg.raycastTarget = true;
            Button jtBtn = jumpTrigger.AddComponent<Button>();
            jtBtn.transition = Selectable.Transition.None;
            jtBtn.onClick.AddListener(() => {
                if (PlayerController.Instance != null) PlayerController.Instance.Jump();
            });

            HUDController hud = panel.AddComponent<HUDController>();

            // Top Header Bar
            // Anlık Skor (Sol Üst Pill Rozeti)
            GameObject scoreCard = CreateCardContainer(panel.transform, "ScorePill", new Vector2(260f, 74f));
            scoreCard.GetComponent<RectTransform>().anchoredPosition = new Vector2(-360f, 820f);
            hud.currentScoreText = CreateText(scoreCard.transform, "CurrentScoreText", "Skor: 0", 28, Vector2.zero, Color.white, false, FontStyle.Bold);

            // En İyi Skor (Sağ Üst Pill Rozeti)
            GameObject highCard = CreateCardContainer(panel.transform, "HighScorePill", new Vector2(260f, 74f));
            highCard.GetComponent<RectTransform>().anchoredPosition = new Vector2(360f, 820f);
            hud.highScoreText = CreateText(highCard.transform, "HighScoreText", "En iyi: 0", 28, Vector2.zero, new Color(1.0f, 0.82f, 0.4f), false, FontStyle.Bold);

            // Active PowerUp Badges Bar (Üst Orta)
            GameObject badgeBar = new GameObject("PowerUpBadgeBar");
            badgeBar.transform.SetParent(panel.transform, false);
            RectTransform barRt = badgeBar.AddComponent<RectTransform>();
            barRt.anchoredPosition = new Vector2(0f, 820f);
            barRt.sizeDelta = new Vector2(600f, 80f);

            hud.shieldBadgeText = CreateText(badgeBar.transform, "ShieldBadge", "Kalkan", 26, new Vector2(-190f, 0f), new Color(0.30f, 0.79f, 0.94f), false, FontStyle.Bold);
            hud.shieldBadgeObject = hud.shieldBadgeText.gameObject;
            hud.shieldBadgeObject.SetActive(false);

            hud.slowBadgeText = CreateText(badgeBar.transform, "SlowBadge", "Yavaş", 26, new Vector2(0f, 0f), new Color(0.56f, 0.88f, 0.94f), false, FontStyle.Bold);
            hud.slowBadgeObject = hud.slowBadgeText.gameObject;
            hud.slowBadgeObject.SetActive(false);

            hud.multiBadgeText = CreateText(badgeBar.transform, "MultiBadge", "x2 Puan", 26, new Vector2(190f, 0f), new Color(1.0f, 0.84f, 0.04f), false, FontStyle.Bold);
            hud.multiBadgeObject = hud.multiBadgeText.gameObject;
            hud.multiBadgeObject.SetActive(false);

            // Alt Kontrol İpucu
            hud.controlTipText = CreateText(panel.transform, "ControlTipText", "Zıpla: Dokun / Space", 24, new Vector2(0f, -840f), new Color(0.6f, 0.7f, 0.85f, 0.7f), false);

            panel.SetActive(false);
            if (UIManager.Instance != null) UIManager.Instance.hudPanel = panel;
        }

        private void CreateGameOverPanel(Transform parent)
        {
            GameObject panel = new GameObject("GameOverPanel");
            panel.transform.SetParent(parent, false);
            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.65f);

            GameOverUI goUI = panel.AddComponent<GameOverUI>();

            // Centered Glassmorphic Card (760x760, Y: +100)
            GameObject card = CreateCardContainer(panel.transform, "GameOverCard", new Vector2(760f, 760f));
            card.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 100f);

            // Başlık "OYUN BİTTİ" (Coral #ef476f)
            CreateText(card.transform, "GameOverTitle", "OYUN BİTTİ", 52, new Vector2(0f, 280f), new Color(0.94f, 0.28f, 0.44f), false, FontStyle.Bold);

            // Alt Bilgi "Bir engele çarptın!"
            CreateText(card.transform, "GameOverSub", "Bir engele çarptın!", 24, new Vector2(0f, 225f), new Color(0.65f, 0.72f, 0.85f), false);

            // Skor Büyük Rakam
            goUI.finalScoreText = CreateText(card.transform, "FinalScoreText", "0", 80, new Vector2(0f, 135f), Color.white, false, FontStyle.Bold);

            // En İyi Skor
            goUI.highScoreText = CreateText(card.transform, "HighScoreText", "En iyi skor: 0", 28, new Vector2(0f, 55f), new Color(1.0f, 0.82f, 0.4f), false, FontStyle.Bold);

            // Yeni Rekor Rozeti
            Text newRecText = CreateText(card.transform, "NewRecordBadge", "★ YENİ REKOR! ★", 28, new Vector2(0f, 0f), new Color(0.024f, 0.839f, 0.627f), false, FontStyle.Bold);
            goUI.newRecordBadge = newRecText.gameObject;
            goUI.newRecordBadge.SetActive(false);

            // Son 5 Skor Listesi (Kompakt Tek Satır)
            goUI.scoreHistoryListText = CreateText(card.transform, "HistoryListText", "", 22, new Vector2(0f, -55f), new Color(0.7f, 0.78f, 0.9f), false);

            // Tekrar Oyna Butonu (Neon Mint #06d6a0)
            Color mintColor = new Color(0.024f, 0.839f, 0.627f);
            Color darkNavy = new Color(0.05f, 0.07f, 0.13f);
            GameObject restartBtn = CreatePillButton(card.transform, "RestartButton", "TEKRAR OYNA", new Vector2(0f, -160f), new Vector2(400f, 84f), mintColor, darkNavy, 32);
            goUI.restartButton = restartBtn.GetComponent<Button>();

            // Ana Menü Butonu (Sleek ikincil buton)
            GameObject menuBtn = CreatePillButton(card.transform, "MainMenuButton", "ANA MENÜ", new Vector2(0f, -265f), new Vector2(320f, 72f), new Color(0.12f, 0.16f, 0.29f, 0.9f), new Color(0.9f, 0.95f, 1f), 24);
            goUI.mainMenuButton = menuBtn.GetComponent<Button>();

            panel.SetActive(false);
            if (UIManager.Instance != null) UIManager.Instance.gameOverPanel = panel;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, Vector2 position, Color color, bool raycastTarget = false, FontStyle fontStyle = FontStyle.Normal)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(800f, 120f);

            Text txt = obj.AddComponent<Text>();
            txt.font = GetSafeDefaultFont();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = color;
            txt.fontStyle = fontStyle;
            txt.raycastTarget = raycastTarget;

            return txt;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
        {
            Color mintColor = new Color(0.024f, 0.839f, 0.627f);
            Color darkNavy = new Color(0.05f, 0.07f, 0.13f);
            return CreatePillButton(parent, name, label, position, size, mintColor, darkNavy, 32);
        }
    }
}
