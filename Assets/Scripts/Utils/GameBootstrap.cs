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

        public static GameBootstrap Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            LocalizationManager.InitializeLanguage();
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.AutoRotation;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = true;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
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
            mainCam.backgroundColor = new Color(0.06f, 0.08f, 0.15f); // #0f1426

            // 60/120 FPS akıcı yenileme hızı (Piksel titremesi ve hareket kırılmalarını önler)
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            // URP Kamera verisi & Anti-Aliasing (kenar pikselleşmesini ve titreşimi önler)
            var camData = mainCam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (camData == null)
            {
                camData = mainCam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            }
            if (camData != null)
            {
                camData.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.FastApproximateAntialiasing;
            }
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

            if (WorldThemeManager.Instance == null)
            {
                GameObject wtm = new GameObject("WorldThemeManager");
                wtm.AddComponent<WorldThemeManager>();
            }

            if (GameCenterManager.Instance == null)
            {
                GameObject gcm = new GameObject("GameCenterManager");
                gcm.AddComponent<GameCenterManager>();
            }

            if (StoreReviewManager.Instance == null)
            {
                GameObject srm = new GameObject("StoreReviewManager");
                srm.AddComponent<StoreReviewManager>();
            }

            if (NotificationManager.Instance == null)
            {
                GameObject nm = new GameObject("NotificationManager");
                nm.AddComponent<NotificationManager>();
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
            // BackgroundTierManager - 4 Kademe Atmosferik Arka Plan (1024x1920)
            GameObject bgManagerObj = GameObject.Find("BackgroundTierManager");
            if (bgManagerObj == null)
            {
                bgManagerObj = new GameObject("BackgroundTierManager");
                bgManagerObj.transform.position = new Vector3(0f, 0f, 6.0f);

                BackgroundTierManager btm = bgManagerObj.AddComponent<BackgroundTierManager>();

                btm.tierSprites[0] = Resources.Load<Sprite>("Sprites/bg_tier0_gunduz");
                btm.tierSprites[1] = Resources.Load<Sprite>("Sprites/bg_tier1_gunbatimi");
                btm.tierSprites[2] = Resources.Load<Sprite>("Sprites/bg_tier2_alacakaranlik");
                btm.tierSprites[3] = Resources.Load<Sprite>("Sprites/bg_tier3_gece");

                Material safeMat = GetSafeSpriteMaterial();

                // 1. Birincil Arka Plan Katmanı (sortingOrder = -20)
                GameObject primaryObj = new GameObject("PrimaryBackground");
                primaryObj.transform.SetParent(bgManagerObj.transform, false);
                primaryObj.transform.localPosition = Vector3.zero;
                SpriteRenderer primarySr = primaryObj.AddComponent<SpriteRenderer>();
                primarySr.sprite = btm.tierSprites[0];
                if (WorldThemeManager.Instance != null)
                {
                    Sprite initBg = WorldThemeManager.Instance.GetBackgroundSprite(WorldThemeManager.Instance.CurrentCharacterKey, 0);
                    if (initBg != null) primarySr.sprite = initBg;
                }
                primarySr.sortingOrder = -20;
                if (safeMat != null) primarySr.material = safeMat;
                btm.primaryRenderer = primarySr;

                // 2. Geçiş (Fade) Arka Plan Katmanı (sortingOrder = -19)
                GameObject fadeObj = new GameObject("FadeBackground");
                fadeObj.transform.SetParent(bgManagerObj.transform, false);
                fadeObj.transform.localPosition = Vector3.zero;
                SpriteRenderer fadeSr = fadeObj.AddComponent<SpriteRenderer>();
                fadeSr.sortingOrder = -19;
                fadeSr.color = new Color(1f, 1f, 1f, 0f);
                if (safeMat != null) fadeSr.material = safeMat;
                btm.fadeRenderer = fadeSr;

                btm.SetInitialTier(0);
            }
        }

        private void SetupGround()
        {
            GameObject ground = GameObject.Find("Ground");
            if (ground == null)
            {
                ground = new GameObject("Ground");
                ground.transform.position = new Vector3(0f, -3.20f, 0f);

                ScrollingGround sg = ground.AddComponent<ScrollingGround>();
                sg.tierSprites[0] = Resources.Load<Sprite>("Sprites/ground_tier0_gunduz");
                sg.tierSprites[1] = Resources.Load<Sprite>("Sprites/ground_tier1_gunbatimi");
                sg.tierSprites[2] = Resources.Load<Sprite>("Sprites/ground_tier2_alacakaranlik");
                sg.tierSprites[3] = Resources.Load<Sprite>("Sprites/ground_tier3_gece");

                float tileW = 6.8266f;
                if (WorldThemeManager.Instance != null)
                {
                    Sprite initGround = WorldThemeManager.Instance.GetGroundSprite(WorldThemeManager.Instance.CurrentCharacterKey, 0);
                    if (initGround != null && initGround.pixelsPerUnit > 0)
                    {
                        tileW = initGround.rect.width / initGround.pixelsPerUnit;
                    }
                }
                else if (sg.tierSprites[0] != null && sg.tierSprites[0].pixelsPerUnit > 0)
                {
                    tileW = sg.tierSprites[0].rect.width / sg.tierSprites[0].pixelsPerUnit;
                }
                sg.tileWidth = tileW;

                int tileCount = 5;
                sg.tiles = new Transform[tileCount];
                sg.renderers = new SpriteRenderer[tileCount];
                Material safeMat = GetSafeSpriteMaterial();

                Sprite defaultGroundSprite = sg.tierSprites[0];
                if (WorldThemeManager.Instance != null)
                {
                    Sprite initGround = WorldThemeManager.Instance.GetGroundSprite(WorldThemeManager.Instance.CurrentCharacterKey, 0);
                    if (initGround != null) defaultGroundSprite = initGround;
                }

                for (int i = 0; i < tileCount; i++)
                {
                    GameObject tileObj = new GameObject($"GroundTile_{i}");
                    tileObj.transform.SetParent(ground.transform, false);
                    float startX = (i - 1) * tileW;
                    tileObj.transform.localPosition = new Vector3(startX, 0f, 0f);

                    SpriteRenderer sr = tileObj.AddComponent<SpriteRenderer>();
                    sr.sprite = defaultGroundSprite;
                    sr.sortingOrder = 2;
                    if (safeMat != null) sr.material = safeMat;

                    sg.tiles[i] = tileObj.transform;
                    sg.renderers[i] = sr;
                }

                // Sabit Zemin Çarpışma Kutusu (Karakter fiziği için)
                BoxCollider2D col = ground.AddComponent<BoxCollider2D>();
                col.size = new Vector2(60f, 4f);
                col.offset = new Vector2(0f, -2.0f); // Üst yüzey Y = -3.20f

                int groundLayer = LayerMask.NameToLayer("Ground");
                if (groundLayer != -1) ground.layer = groundLayer;

                sg.SetTier(0);
                sg.ResetPositions();
            }
        }

        private void SetupPlayer()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                player = new GameObject("Player");
                try { player.tag = "Player"; } catch { }

                player.transform.position = new Vector3(PlayerController.GetResponsivePlayerX(), PlayerController.GROUND_FLOOR_Y, 0f);

                Material safeMat = GetSafeSpriteMaterial();

                // Gövde Görseli (Cute rounded square with eyes & catchlight)
                SpriteRenderer bodySr = player.AddComponent<SpriteRenderer>();
                bodySr.sprite = ProceduralSpriteGenerator.GetSpriteOrGenerate("Sprites/classic_hero", () => ProceduralSpriteGenerator.CreatePlayerSprite(new Color(1.0f, 0.82f, 0.4f), new Color(1.0f, 0.91f, 0.66f, 0.8f), 128));
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

            Color obsColor = Color.white; // Temel beyaz: SpriteRenderer.color dünya paletine göre renklendirir
            Color airColor = Color.white;

            BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            if (name == "Cactus")
            {
                sr.sprite = ProceduralSpriteGenerator.GetSpriteOrGenerate("Sprites/cactus", () => ProceduralSpriteGenerator.CreateCactusSprite(obsColor));
                col.size = new Vector2(0.34f, 0.54f);
                col.offset = new Vector2(0f, 0.27f);
            }
            else if (name == "Rock")
            {
                sr.sprite = ProceduralSpriteGenerator.GetSpriteOrGenerate("Sprites/rock", () => ProceduralSpriteGenerator.CreateRockSprite(obsColor));
                col.size = new Vector2(0.48f, 0.30f);
                col.offset = new Vector2(0f, 0.15f);
            }
            else if (name == "Spike")
            {
                sr.sprite = ProceduralSpriteGenerator.GetSpriteOrGenerate("Sprites/spike", () => ProceduralSpriteGenerator.CreateSpikeSprite(obsColor));
                col.size = new Vector2(0.28f, 0.72f);
                col.offset = new Vector2(0f, 0.36f);
            }
            else if (name.Contains("Bird"))
            {
                sr.sprite = ProceduralSpriteGenerator.GetSpriteOrGenerate("Sprites/bird", () => ProceduralSpriteGenerator.CreateBirdSprite(airColor));
                col.size = new Vector2(0.64f, 0.38f);
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

            string powerUpResource = "Sprites/shield_powerup";
            if (type == PowerUpType.Slow) powerUpResource = "Sprites/slow_powerup";
            else if (type == PowerUpType.ScoreMultiplier) powerUpResource = "Sprites/multi_powerup";

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.GetSpriteOrGenerate(powerUpResource, () => ProceduralSpriteGenerator.CreatePowerUpOrb(color, label));
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
            canvasObj.transform.localScale = Vector3.one * 0.012f;

            RectTransform canvasRt = canvasObj.GetComponent<RectTransform>();
            canvasRt.sizeDelta = new Vector2(600f, 160f);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(canvasObj.transform, false);

            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.sizeDelta = new Vector2(600f, 160f);
            textRt.anchoredPosition = Vector2.zero;

            Text txt = textObj.AddComponent<Text>();
            txt.font = GetSafeDefaultFont();
            txt.fontSize = 44;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(1.0f, 0.84f, 0.04f); // #ffd60a
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;

            Shadow shadow = textObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            shadow.effectDistance = new Vector2(2f, -2f);

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
                canvasObj.AddComponent<UI.ResponsiveCanvasScaler>();

                canvasObj.AddComponent<GraphicRaycaster>();
                canvasObj.AddComponent<UIManager>();

                // 1. Ana Menü Paneli
                CreateMainMenuPanel(canvasObj.transform);

                // 2. HUD Paneli
                CreateHUDPanel(canvasObj.transform);

                // 3. Oyun Bitti Paneli
                CreateGameOverPanel(canvasObj.transform);

                // 4. Günlük Ödül Sandığı Modalı
                CreateDailyRewardModal(canvasObj.transform);

                // 5. Gardırop / Kıyafetler Modalı
                CreateWardrobeModal(canvasObj.transform);

                // 6. Başlangıç & Kurallar Onboarding Modalı
                CreateOnboardingModal(canvasObj.transform);

                bool shouldShowOnboarding = PlayerPrefs.GetInt(UI.OnboardingModalUI.KEY_ONBOARDING_SEEN, 0) == 0;
                if (shouldShowOnboarding && UIManager.Instance != null && UIManager.Instance.mainMenuPanel != null)
                {
                    UIManager.Instance.mainMenuPanel.SetActive(false);
                }

                // 7. Hoppzy Açılış & Splash Screen Katmanı (Animasyonlu Kararma & Dokunarak Geçme)
                CreateSplashScreenOverlay(canvasObj.transform);
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
            // Dark Cinematic #101F38 Koyu Lacivert + İnce Mavi Kenarlık + 48px Yumuşak Köşe Kavisleri
            img.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(
                new Color(0.063f, 0.122f, 0.220f, 0.98f),
                new Color(0.153f, 0.353f, 0.647f, 0.35f),
                2.0f, 48, 256, 256);
            img.type = Image.Type.Sliced;
            img.raycastTarget = true;

            return card;
        }

        private static GameObject CreatePillButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Color bgColor, Color textColor, int fontSize = 32, Color? borderColor = null, float borderWidth = 0f, int cornerRadius = 24)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            Color border = borderColor ?? Color.clear;
            img.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(bgColor, border, borderWidth, cornerRadius, 128, 64);
            img.type = Image.Type.Sliced;
            img.raycastTarget = true;

            Button btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            btn.colors = colors;

            Text labelTxt = CreateText(obj.transform, "Label", label, fontSize, Vector2.zero, textColor, false, FontStyle.Bold);
            RectTransform labelRt = labelTxt.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(16f, 0f);
            labelRt.offsetMax = new Vector2(-16f, 0f);
            labelTxt.resizeTextForBestFit = true;
            labelTxt.resizeTextMinSize = 18;
            labelTxt.resizeTextMaxSize = fontSize;
            labelTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            labelTxt.verticalOverflow = VerticalWrapMode.Truncate;

            return obj;
        }

        private static GameObject CreateHUDBadge(Transform parent, string name, Vector2 size, Vector2 position, Color bgColor, Color borderColor, float borderWidth, Sprite iconSprite, string initialText, Color textColor, int fontSize, out Text outText, Vector2? anchor = null, Vector2? pivot = null)
        {
            GameObject card = new GameObject(name);
            card.transform.SetParent(parent, false);
            RectTransform rt = card.AddComponent<RectTransform>();
            Vector2 anc = anchor ?? new Vector2(0.5f, 0.5f);
            Vector2 piv = pivot ?? new Vector2(0.5f, 0.5f);
            rt.anchorMin = anc;
            rt.anchorMax = anc;
            rt.pivot = piv;
            rt.sizeDelta = size;
            rt.anchoredPosition = position;

            Image img = card.AddComponent<Image>();
            img.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(bgColor, borderColor, borderWidth, 28, 128, 128);
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;

            // Sol İkon
            if (iconSprite != null)
            {
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(card.transform, false);
                RectTransform iconRt = iconObj.AddComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0f, 0.5f);
                iconRt.anchorMax = new Vector2(0f, 0.5f);
                iconRt.pivot = new Vector2(0f, 0.5f);
                iconRt.anchoredPosition = new Vector2(16f, 0f);
                iconRt.sizeDelta = new Vector2(56f, 56f);

                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = iconSprite;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
            }

            // Sağ Metin Alanı
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(card.transform, false);
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(iconSprite != null ? 82f : 16f, 4f);
            textRt.offsetMax = new Vector2(-14f, -4f);

            Text txt = textObj.AddComponent<Text>();
            txt.font = GetSafeDefaultFont();
            txt.text = initialText;
            txt.fontSize = fontSize;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.color = textColor;
            txt.fontStyle = FontStyle.Bold;
            txt.raycastTarget = false;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;

            Shadow shadow = textObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(2f, -2f);

            outText = txt;
            return card;
        }

        private static void CreateSectionDivider(Transform parent, string title, float yPos)
        {
            GameObject dividerObj = new GameObject("SectionDivider");
            dividerObj.transform.SetParent(parent, false);
            RectTransform rt = dividerObj.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0f, yPos);
            rt.sizeDelta = new Vector2(780f, 30f);

            // Sol Çizgi
            GameObject leftLine = new GameObject("LeftLine");
            leftLine.transform.SetParent(dividerObj.transform, false);
            RectTransform leftRt = leftLine.AddComponent<RectTransform>();
            leftRt.anchoredPosition = new Vector2(-160f, 0f);
            leftRt.sizeDelta = new Vector2(110f, 2f);
            Image leftImg = leftLine.AddComponent<Image>();
            leftImg.color = new Color(0.24f, 0.35f, 0.52f, 0.65f);
            leftImg.raycastTarget = false;

            // Başlık
            GameObject textObj = new GameObject("Title");
            textObj.transform.SetParent(dividerObj.transform, false);
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchoredPosition = Vector2.zero;
            textRt.sizeDelta = new Vector2(210f, 30f);
            Text txt = textObj.AddComponent<Text>();
            txt.font = GetSafeDefaultFont();
            txt.text = title;
            txt.fontSize = 22;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.42f, 0.56f, 0.74f);
            txt.raycastTarget = false;

            // Sağ Çizgi
            GameObject rightLine = new GameObject("RightLine");
            rightLine.transform.SetParent(dividerObj.transform, false);
            RectTransform rightRt = rightLine.AddComponent<RectTransform>();
            rightRt.anchoredPosition = new Vector2(160f, 0f);
            rightRt.sizeDelta = new Vector2(110f, 2f);
            Image rightImg = rightLine.AddComponent<Image>();
            rightImg.color = new Color(0.24f, 0.35f, 0.52f, 0.65f);
            rightImg.raycastTarget = false;
        }

        private static GameObject CreateMenuGridButton(Transform parent, string name, string iconEmoji, string line1, string line2, Vector2 position, Vector2 size, Color bgColor, Color highlightColor)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(bgColor, highlightColor, 1.5f, 26, 128, 128);
            img.type = Image.Type.Sliced;
            img.raycastTarget = true;

            Button btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            btn.colors = colors;

            // Sol İkon (Emoji / Simge)
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(obj.transform, false);
            RectTransform iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = new Vector2(48f, 0f);
            iconRt.sizeDelta = new Vector2(56f, 56f);
            Text iconTxt = iconObj.AddComponent<Text>();
            iconTxt.font = GetSafeDefaultFont();
            iconTxt.text = iconEmoji;
            iconTxt.fontSize = 42;
            iconTxt.alignment = TextAnchor.MiddleCenter;
            iconTxt.color = Color.white;
            iconTxt.raycastTarget = false;

            // Metin Alanı (2 satır kalın beyaz yazı)
            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(obj.transform, false);
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(88f, 4f);
            textRt.offsetMax = new Vector2(-46f, -4f);
            Text labelComp = textObj.AddComponent<Text>();
            labelComp.font = GetSafeDefaultFont();
            labelComp.text = $"{line1}\n{line2}";
            labelComp.fontSize = 21;
            labelComp.fontStyle = FontStyle.Bold;
            labelComp.alignment = TextAnchor.MiddleLeft;
            labelComp.color = Color.white;
            labelComp.lineSpacing = 1.05f;
            labelComp.resizeTextForBestFit = true;
            labelComp.resizeTextMinSize = 15;
            labelComp.resizeTextMaxSize = 22;
            labelComp.horizontalOverflow = HorizontalWrapMode.Wrap;
            labelComp.verticalOverflow = VerticalWrapMode.Truncate;
            labelComp.raycastTarget = false;

            Shadow shadow = textObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);

            // Sağ Chevron Ok (>)
            GameObject chevObj = new GameObject("Chevron");
            chevObj.transform.SetParent(obj.transform, false);
            RectTransform chevRt = chevObj.AddComponent<RectTransform>();
            chevRt.anchorMin = new Vector2(1f, 0.5f);
            chevRt.anchorMax = new Vector2(1f, 0.5f);
            chevRt.pivot = new Vector2(1f, 0.5f);
            chevRt.anchoredPosition = new Vector2(-20f, 0f);
            chevRt.sizeDelta = new Vector2(30f, 40f);
            Text chevTxt = chevObj.AddComponent<Text>();
            chevTxt.font = GetSafeDefaultFont();
            chevTxt.text = ">";
            chevTxt.fontSize = 32;
            chevTxt.fontStyle = FontStyle.Bold;
            chevTxt.alignment = TextAnchor.MiddleCenter;
            chevTxt.color = new Color(1f, 1f, 1f, 0.85f);
            chevTxt.raycastTarget = false;

            return obj;
        }

        public void ShowRewardToast(Transform parent, string message)
        {
            StartCoroutine(ShowToastCoroutine(parent, message));
        }

        private IEnumerator ShowToastCoroutine(Transform parent, string message)
        {
            GameObject toastObj = new GameObject("RewardToast");
            toastObj.transform.SetParent(parent, false);
            RectTransform rt = toastObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(580f, 76f);
            rt.anchoredPosition = new Vector2(0f, 80f);

            Image bg = toastObj.AddComponent<Image>();
            bg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(new Color(0.08f, 0.12f, 0.22f, 0.95f), new Color(1.0f, 0.82f, 0.25f, 0.95f), 2.5f, 20, 128, 64);
            bg.type = Image.Type.Sliced;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(toastObj.transform, false);
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(16f, 4f);
            textRt.offsetMax = new Vector2(-16f, -4f);

            Text txt = textObj.AddComponent<Text>();
            txt.font = GetSafeDefaultFont();
            txt.text = message;
            txt.fontSize = 26;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(1.0f, 0.92f, 0.45f);

            CanvasGroup cg = toastObj.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            float t = 0f;
            while (t < 0.25f)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(0f, 1f, t / 0.25f);
                yield return null;
            }
            cg.alpha = 1f;

            yield return new WaitForSecondsRealtime(2.0f);

            t = 0f;
            while (t < 0.35f)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, t / 0.35f);
                yield return null;
            }

            Destroy(toastObj);
        }

        public static Sprite LoadMainMenuSprite(string spriteName)
        {
            if (LocalizationManager.CurrentLanguage == LocalizationManager.LANG_EN)
            {
                Sprite enSp = Resources.Load<Sprite>("Sprites/UI_MainMenu/" + spriteName + "_en");
                if (enSp != null) return enSp;
                Texture2D enTex = Resources.Load<Texture2D>("Sprites/UI_MainMenu/" + spriteName + "_en");
                if (enTex != null)
                {
                    return Sprite.Create(enTex, new Rect(0, 0, enTex.width, enTex.height), new Vector2(0.5f, 0.5f));
                }
            }

            Sprite sp = Resources.Load<Sprite>("Sprites/UI_MainMenu/" + spriteName);
            if (sp == null)
            {
                Texture2D tex = Resources.Load<Texture2D>("Sprites/UI_MainMenu/" + spriteName);
                if (tex != null)
                {
                    sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }
            return sp;
        }

        private static GameObject CreateSpriteImage(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return obj;
        }

        private static GameObject CreateSpriteButton(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = true;

            Button btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f);
            cb.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            btn.colors = cb;

            if (onClick != null)
            {
                btn.onClick.AddListener(onClick);
            }

            return obj;
        }

        private void CreateMainMenuPanel(Transform parent)
        {
            GameObject panel = new GameObject("MainMenuPanel");
            panel.transform.SetParent(parent, false);
            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.40f);

            // Dark Cinematic + Playful 3D Ana Panel (#101F38 Koyu Lacivert, 900x1020)
            GameObject card = CreateCardContainer(panel.transform, "MenuCard", new Vector2(900f, 1020f));
            card.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 110f);
            var menuFitter = card.AddComponent<UI.ResponsiveCardFitter>();
            menuFitter.adjustVerticalPosition = true;
            menuFitter.verticalOffset = 110f;

            // 1. "= HOPPZY =" Logo Başlığı (Altın-Turuncu 3D Tipografi - Üstte Bağımsız ve Geniş)
            Sprite titleSp = LoadMainMenuSprite("title_hoppzy");
            if (titleSp != null)
            {
                CreateSpriteImage(card.transform, "TitleImage", titleSp, new Vector2(0f, 432f), new Vector2(500f, 119f));
            }
            else
            {
                CreateText(card.transform, "TitleText", "= HOPPZY =", 84, new Vector2(0f, 432f), new Color(1.0f, 0.74f, 0.08f), false, FontStyle.Bold);
            }

            // 1.5. Ses Aç/Kapat Butonu (Sağ Üst - 3D Beveled Squircle Buton, Birebir Kullanıcı Referansı)
            GameObject soundBtnObj = new GameObject("MainMenuSoundButton");
            soundBtnObj.transform.SetParent(card.transform, false);
            RectTransform soundRt = soundBtnObj.AddComponent<RectTransform>();
            soundRt.sizeDelta = new Vector2(80f, 80f);
            soundRt.anchoredPosition = new Vector2(365f, 432f);

            Image soundImg = soundBtnObj.AddComponent<Image>();
            soundImg.preserveAspect = true;
            soundImg.raycastTarget = true;

            System.Action updateSoundIcon = () => {
                bool isMuted = AudioManager.Instance != null && AudioManager.Instance.IsMuted;
                string path = isMuted ? "Sprites/UI/btn_sound_off" : "Sprites/UI/btn_sound_on";
                Sprite sp = Resources.Load<Sprite>(path);
                if (sp == null) sp = ProceduralSpriteGenerator.CreateSpeakerIconSprite(isMuted, 128);
                soundImg.sprite = sp;
            };
            updateSoundIcon();

            Button soundBtn = soundBtnObj.AddComponent<Button>();
            ColorBlock scb = soundBtn.colors;
            scb.normalColor = Color.white;
            scb.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            scb.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            soundBtn.colors = scb;

            soundBtn.onClick.AddListener(() => {
                if (AudioManager.Instance != null)
                {
                    bool nowMuted = AudioManager.Instance.ToggleMute();
                    updateSoundIcon();
                    AudioManager.Instance.TriggerHapticFeedback();
                    string toastMsg = LocalizationManager.Get(nowMuted ? "menu_sound_off" : "menu_sound_on", nowMuted ? "Ses: Kapalı" : "Ses: Açık");
                    if (Instance != null)
                    {
                        Instance.ShowRewardToast(card.transform, toastMsg);
                    }
                }
            });

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.OnMuteStateChanged += (muted) => updateSoundIcon();
            }

            // 2. Alt Başlık "tek dokunuş, sonsuz kaçış" (#9DBCE0)
            Sprite subSp = LoadMainMenuSprite("subtitle_hoppzy");
            Image subImg = null;
            Text subTxt = null;
            if (subSp != null)
            {
                GameObject subObj = CreateSpriteImage(card.transform, "SubtitleImage", subSp, new Vector2(0f, 365f), new Vector2(330f, 30f));
                subImg = subObj.GetComponent<Image>();
            }
            else
            {
                subTxt = CreateText(card.transform, "SubtitleText", LocalizationManager.Get("app_subtitle"), 26, new Vector2(0f, 365f), new Color(0.61f, 0.73f, 0.88f), false);
            }

            // 2.5. Rozetler Satırı (Skor & Can - Yazının yanından alınıp aşağıda yan yana estetik rozet barı yapıldı)
            // Sol Rozet: 🏆 SKORLAR (Apple Game Center)
            GameObject lbBadge = CreateCardContainer(card.transform, "MainMenuLeaderboardBadge", new Vector2(230f, 62f));
            RectTransform lbRt = lbBadge.GetComponent<RectTransform>();
            lbRt.anchoredPosition = new Vector2(-135f, 308f);

            Image lbBg = lbBadge.GetComponent<Image>();
            lbBg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(
                new Color(0.18f, 0.14f, 0.05f, 0.95f),
                new Color(1.0f, 0.78f, 0.20f, 0.95f),
                2.5f, 22, 230, 62);

            // İçerik Taşıyıcı (Kupa + Metin Birlikte Tam Ortalanır)
            GameObject lbContent = new GameObject("Content");
            lbContent.transform.SetParent(lbBadge.transform, false);
            RectTransform lbContentRt = lbContent.AddComponent<RectTransform>();
            lbContentRt.anchorMin = new Vector2(0.5f, 0.5f);
            lbContentRt.anchorMax = new Vector2(0.5f, 0.5f);
            lbContentRt.pivot = new Vector2(0.5f, 0.5f);
            lbContentRt.anchoredPosition = Vector2.zero;

            HorizontalLayoutGroup lbHlg = lbContent.AddComponent<HorizontalLayoutGroup>();
            lbHlg.childAlignment = TextAnchor.MiddleCenter;
            lbHlg.spacing = 8f;
            lbHlg.childControlWidth = false;
            lbHlg.childControlHeight = false;
            lbHlg.childForceExpandWidth = false;
            lbHlg.childForceExpandHeight = false;

            ContentSizeFitter lbCsf = lbContent.AddComponent<ContentSizeFitter>();
            lbCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            lbCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Kupa İkonu (Vektörel prosedürel sprite - 30x30)
            GameObject lbIconObj = new GameObject("TrophyIcon");
            lbIconObj.transform.SetParent(lbContent.transform, false);
            RectTransform lbIconRt = lbIconObj.AddComponent<RectTransform>();
            lbIconRt.sizeDelta = new Vector2(30f, 30f);
            Image lbIconImg = lbIconObj.AddComponent<Image>();
            lbIconImg.sprite = ProceduralSpriteGenerator.CreateTrophyIconSprite(64);
            lbIconImg.preserveAspect = true;
            lbIconImg.raycastTarget = false;

            // Metin (SKORLAR / LEADERBOARD)
            GameObject lbTextObj = new GameObject("LeaderboardText");
            lbTextObj.transform.SetParent(lbContent.transform, false);
            RectTransform lbTxtRt = lbTextObj.AddComponent<RectTransform>();
            Text lbText = lbTextObj.AddComponent<Text>();
            lbText.font = GetSafeDefaultFont();
            lbText.fontSize = 22;
            lbText.fontStyle = FontStyle.Bold;
            lbText.alignment = TextAnchor.MiddleCenter;
            lbText.color = new Color(1.0f, 0.92f, 0.65f);
            lbText.text = LocalizationManager.Get("menu_leaderboard_label", "SKORLAR");
            lbText.raycastTarget = false;
            lbText.horizontalOverflow = HorizontalWrapMode.Overflow;
            lbText.verticalOverflow = VerticalWrapMode.Overflow;

            ContentSizeFitter lbTxtCsf = lbTextObj.AddComponent<ContentSizeFitter>();
            lbTxtCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            lbTxtCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Button lbBtn = lbBadge.AddComponent<Button>();
            lbBtn.onClick.AddListener(() => {
                AudioManager.Instance?.PlayJumpSound();
                if (GameCenterManager.Instance != null)
                {
                    GameCenterManager.Instance.ShowLeaderboardUI(() => {
                        if (Instance != null)
                        {
                            Instance.ShowRewardToast(card.transform, LocalizationManager.Get("menu_leaderboard_toast_not_auth", "⚠️ Apple Game Center'a bağlanılamadı."));
                        }
                    });
                }
            });

            LocalizationManager.OnLanguageChanged += () => {
                if (lbText != null) lbText.text = LocalizationManager.Get("menu_leaderboard_label", "SKORLAR");
            };

            // Sağ Rozet: ❤️ CAN ROZETİ (Extra Life Pill)
            GameObject lifeBadge = CreateCardContainer(card.transform, "MainMenuLifeBadge", new Vector2(230f, 62f));
            RectTransform lifeRt = lifeBadge.GetComponent<RectTransform>();
            lifeRt.anchoredPosition = new Vector2(135f, 308f);

            Image lifeBg = lifeBadge.GetComponent<Image>();
            lifeBg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(
                new Color(0.14f, 0.06f, 0.10f, 0.95f),
                new Color(1.0f, 0.35f, 0.45f, 0.95f),
                2.5f, 22, 230, 62);

            // İçerik Taşıyıcı (Kalp + Metin Birlikte Tam Ortalanır)
            GameObject lifeContent = new GameObject("Content");
            lifeContent.transform.SetParent(lifeBadge.transform, false);
            RectTransform lifeContentRt = lifeContent.AddComponent<RectTransform>();
            lifeContentRt.anchorMin = new Vector2(0.5f, 0.5f);
            lifeContentRt.anchorMax = new Vector2(0.5f, 0.5f);
            lifeContentRt.pivot = new Vector2(0.5f, 0.5f);
            lifeContentRt.anchoredPosition = Vector2.zero;

            HorizontalLayoutGroup lifeHlg = lifeContent.AddComponent<HorizontalLayoutGroup>();
            lifeHlg.childAlignment = TextAnchor.MiddleCenter;
            lifeHlg.spacing = 8f;
            lifeHlg.childControlWidth = false;
            lifeHlg.childControlHeight = false;
            lifeHlg.childForceExpandWidth = false;
            lifeHlg.childForceExpandHeight = false;

            ContentSizeFitter lifeCsf = lifeContent.AddComponent<ContentSizeFitter>();
            lifeCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            lifeCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Kalp İkonu (30x30)
            GameObject lifeIconObj = new GameObject("HeartIcon");
            lifeIconObj.transform.SetParent(lifeContent.transform, false);
            RectTransform lifeIconRt = lifeIconObj.AddComponent<RectTransform>();
            lifeIconRt.sizeDelta = new Vector2(30f, 30f);
            Image lifeIconImg = lifeIconObj.AddComponent<Image>();
            lifeIconImg.sprite = ProceduralSpriteGenerator.CreateHeartIconSprite(64);
            lifeIconImg.preserveAspect = true;
            lifeIconImg.raycastTarget = false;

            // Metin (CAN / LIVES + Sayı)
            GameObject lifeTextObj = new GameObject("LifeText");
            lifeTextObj.transform.SetParent(lifeContent.transform, false);
            RectTransform lifeTxtRt = lifeTextObj.AddComponent<RectTransform>();
            Text menuLifeText = lifeTextObj.AddComponent<Text>();
            menuLifeText.font = GetSafeDefaultFont();
            menuLifeText.fontSize = 24;
            menuLifeText.fontStyle = FontStyle.Bold;
            menuLifeText.alignment = TextAnchor.MiddleCenter;
            menuLifeText.color = Color.white;
            menuLifeText.raycastTarget = false;
            menuLifeText.horizontalOverflow = HorizontalWrapMode.Overflow;
            menuLifeText.verticalOverflow = VerticalWrapMode.Overflow;

            ContentSizeFitter lifeTxtCsf = lifeTextObj.AddComponent<ContentSizeFitter>();
            lifeTxtCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            lifeTxtCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CanvasGroup lifeCg = lifeBadge.AddComponent<CanvasGroup>();

            System.Action updateMenuLifeBadge = () => {
                if (menuLifeText == null) return;
                int lives = SaveSystem.GetConsumableCount("second_chance");
                string lbl = LocalizationManager.Get("menu_life_label", "CAN");
                menuLifeText.text = $"<size=16><color=#ff7d90>{lbl}</color></size>  <size=28><color=#ffffff><b>{lives}</b></color></size>";
                if (lifeCg != null)
                {
                    lifeCg.alpha = lives > 0 ? 1f : 0.55f;
                }
            };

            Button lifeBtn = lifeBadge.AddComponent<Button>();
            lifeBtn.onClick.AddListener(() => {
                AudioManager.Instance?.PlayJumpSound();
                int currentLives = SaveSystem.GetConsumableCount("second_chance");
                string info = LocalizationManager.GetFormat("menu_life_toast_info", currentLives);
                if (Instance != null)
                {
                    Instance.ShowRewardToast(card.transform, info);
                }
            });

            SaveSystem.OnConsumablesChanged += updateMenuLifeBadge;
            LocalizationManager.OnLanguageChanged += updateMenuLifeBadge;
            updateMenuLifeBadge();

            // 3. Bölücü Çizgi "─── KARAKTER SEÇ ───" (#9DBCE0)
            Sprite divSp = LoadMainMenuSprite("divider_karakter_sec");
            Image divImg = null;
            if (divSp != null)
            {
                GameObject divObj = CreateSpriteImage(card.transform, "DividerImage", divSp, new Vector2(0f, 252f), new Vector2(430f, 28f));
                divImg = divObj.GetComponent<Image>();
            }
            else
            {
                CreateSectionDivider(card.transform, LocalizationManager.Get("menu_select_character"), 252f);
            }

            // 4. Karakter Galerisi UI (5 Karakter Kartı - Seçili olan #27C7FF Neon Mavi Çerçeveli)
            GameObject galleryObj = new GameObject("CharacterGallery");
            galleryObj.transform.SetParent(card.transform, false);
            RectTransform galleryRt = galleryObj.AddComponent<RectTransform>();
            galleryRt.anchoredPosition = new Vector2(0f, 150f);
            galleryRt.sizeDelta = new Vector2(840f, 170f);

            galleryObj.AddComponent<CharacterSelectorUI>();

            // 5. "▶ OYNA" Butonu (Master Referans: 800x110, Birebir Kapsül Tasarım)
            Sprite oynaSp = LoadMainMenuSprite("btn_oyna");
            Image playImg = null;
            Text playTxt = null;
            if (oynaSp != null)
            {
                GameObject playBtnObj = CreateSpriteButton(card.transform, "PlayButton", oynaSp, new Vector2(0f, -5f), new Vector2(800f, 110f), () => {
                    AudioManager.Instance?.PlayJumpSound();
                    if (GameManager.Instance != null) GameManager.Instance.StartGame();
                });
                playImg = playBtnObj.GetComponent<Image>();
            }
            else
            {
                Color primaryBlue = new Color(0.086f, 0.471f, 1.0f); // #1678FF
                Color blueBorder = new Color(0.40f, 0.75f, 1.0f, 0.50f);
                GameObject btnObj = CreatePillButton(card.transform, "PlayButton", LocalizationManager.Get("menu_play"), new Vector2(0f, -5f), new Vector2(800f, 110f), primaryBlue, Color.white, 44, blueBorder, 2.5f, 58);
                playTxt = btnObj.GetComponentInChildren<Text>();
                Button btn = btnObj.GetComponent<Button>();
                btn.onClick.AddListener(() => {
                    AudioManager.Instance?.PlayJumpSound();
                    if (GameManager.Instance != null) GameManager.Instance.StartGame();
                });
            }

            // 6. 2x2 Grid Özellik Kartları (Master Referans, Boyut: 390x130, 924x318 HD Varlıklar):
            // Row 1 (y = -155f): [MAĞAZA & GARDIROP (Mor)]   [GÜNLÜK ÖDÜL (Turuncu)]
            // Row 2 (y = -305f): [NASIL OYNANIR? (Camgöbeği)] [REKLAM İZLE / CAN AL (Yeşil)]
            Vector2 gridSize = new Vector2(390f, 130f);
            float row1Y = -155f;
            float row2Y = -305f;
            float colLeftX = -205f;
            float colRightX = 205f;

            // Buton 1 (Sol Üst): 🛍️ Mağaza & Gardırop (Mor İllüstrasyonlu Buton)
            Sprite wardrobeSp = LoadMainMenuSprite("btn_wardrobe");
            Image wardrobeImg = null;
            if (wardrobeSp != null)
            {
                GameObject wardObj = CreateSpriteButton(card.transform, "WardrobeGridButton", wardrobeSp, new Vector2(colLeftX, row1Y), gridSize, () => {
                    AudioManager.Instance?.PlayJumpSound();
                    if (UI.WardrobeModalUI.Instance != null)
                    {
                        UI.WardrobeModalUI.Instance.gameObject.SetActive(true);
                    }
                });
                wardrobeImg = wardObj.GetComponent<Image>();
            }
            else
            {
                Color purpleBg = new Color(0.45f, 0.22f, 0.88f);
                Color purpleHighlight = new Color(0.70f, 0.45f, 1.0f, 0.35f);
                GameObject wardrobeBtnObj = CreateMenuGridButton(card.transform, "WardrobeGridButton", "🛍️", LocalizationManager.Get("menu_shop_line1"), LocalizationManager.Get("menu_shop_line2"), new Vector2(colLeftX, row1Y), gridSize, purpleBg, purpleHighlight);
                wardrobeBtnObj.GetComponent<Button>().onClick.AddListener(() => {
                    AudioManager.Instance?.PlayJumpSound();
                    if (UI.WardrobeModalUI.Instance != null) UI.WardrobeModalUI.Instance.gameObject.SetActive(true);
                });
            }

            // Buton 2 (Sağ Üst): 🎁 Günlük Ödül (Kehribar Takvim İllüstrasyonlu Buton)
            Sprite rewardSp = LoadMainMenuSprite("btn_reward");
            Image rewardImg = null;
            if (rewardSp != null)
            {
                GameObject rewObj = CreateSpriteButton(card.transform, "DailyRewardGridButton", rewardSp, new Vector2(colRightX, row1Y), gridSize, () => {
                    AudioManager.Instance?.PlayJumpSound();
                    if (DailyRewardModalUI.Instance != null)
                    {
                        DailyRewardModalUI.Instance.gameObject.SetActive(true);
                    }
                });
                rewardImg = rewObj.GetComponent<Image>();
            }
            else
            {
                Color goldBg = new Color(0.92f, 0.56f, 0.10f);
                Color goldHighlight = new Color(1.0f, 0.80f, 0.40f, 0.35f);
                GameObject rewardBtnObj = CreateMenuGridButton(card.transform, "DailyRewardGridButton", "🎁", LocalizationManager.Get("menu_reward_line1"), LocalizationManager.Get("menu_reward_line2"), new Vector2(colRightX, row1Y), gridSize, goldBg, goldHighlight);
                rewardBtnObj.GetComponent<Button>().onClick.AddListener(() => {
                    AudioManager.Instance?.PlayJumpSound();
                    if (DailyRewardModalUI.Instance != null) DailyRewardModalUI.Instance.gameObject.SetActive(true);
                });
            }

            // Buton 3 (Sol Alt): 📖 Nasıl Oynanır? (Camgöbeği Kitap İllüstrasyonlu Buton)
            Sprite rulesSp = LoadMainMenuSprite("btn_rules");
            Image rulesImg = null;
            if (rulesSp != null)
            {
                GameObject rulesObj = CreateSpriteButton(card.transform, "RulesGridButton", rulesSp, new Vector2(colLeftX, row2Y), gridSize, () => {
                    AudioManager.Instance?.PlayJumpSound();
                    if (UI.OnboardingModalUI.Instance != null)
                    {
                        UI.OnboardingModalUI.Instance.OpenModal();
                    }
                    else
                    {
                        var found = Object.FindObjectOfType<UI.OnboardingModalUI>(true);
                        if (found != null) found.OpenModal();
                    }
                });
                rulesImg = rulesObj.GetComponent<Image>();
            }
            else
            {
                Color tealBg = new Color(0.08f, 0.62f, 0.82f);
                Color tealHighlight = new Color(0.40f, 0.85f, 1.0f, 0.35f);
                GameObject rulesBtnObj = CreateMenuGridButton(card.transform, "RulesGridButton", "📖", LocalizationManager.Get("menu_rules_line1"), LocalizationManager.Get("menu_rules_line2"), new Vector2(colLeftX, row2Y), gridSize, tealBg, tealHighlight);
                rulesBtnObj.GetComponent<Button>().onClick.AddListener(() => {
                    AudioManager.Instance?.PlayJumpSound();
                    if (UI.OnboardingModalUI.Instance != null) UI.OnboardingModalUI.Instance.OpenModal();
                    else { var found = Object.FindObjectOfType<UI.OnboardingModalUI>(true); if (found != null) found.OpenModal(); }
                });
            }

            // Buton 4 (Sağ Alt): 🎬 Reklam İzle Can Al ❤️ (Yeşil Klaket İllüstrasyonlu Buton)
            UnityEngine.Events.UnityAction adRewardAction = () => {
                AudioManager.Instance?.PlayJumpSound();
                if (MonetizationManager.Instance != null)
                {
                    MonetizationManager.Instance.ShowRewardedAd(
                        () => {
                            SaveSystem.AddConsumable("second_chance", 1);
                            SaveSystem.AddGold(25);
                            AudioManager.Instance?.PlayNewRecordSound();
                            AudioManager.Instance?.TriggerHapticFeedback();
                            if (Instance != null)
                            {
                                Instance.ShowRewardToast(card.transform, LocalizationManager.Get("menu_toast_reward"));
                            }
                            if (UI.WardrobeModalUI.Instance != null && UI.WardrobeModalUI.Instance.gameObject.activeInHierarchy)
                            {
                                UI.WardrobeModalUI.Instance.RefreshUI();
                            }
                        },
                        () => {
                            if (Instance != null)
                            {
                                Instance.ShowRewardToast(card.transform, LocalizationManager.Get("menu_toast_ad_not_ready"));
                            }
                        }
                    );
                }
                else
                {
                    SaveSystem.AddConsumable("second_chance", 1);
                    SaveSystem.AddGold(25);
                    AudioManager.Instance?.PlayNewRecordSound();
                    if (Instance != null)
                    {
                        Instance.ShowRewardToast(card.transform, LocalizationManager.Get("menu_toast_reward"));
                    }
                }
            };

            Sprite adSp = LoadMainMenuSprite("btn_ad");
            Image adImg = null;
            if (adSp != null)
            {
                GameObject adObj = CreateSpriteButton(card.transform, "AdGridButton", adSp, new Vector2(colRightX, row2Y), gridSize, adRewardAction);
                adImg = adObj.GetComponent<Image>();
            }
            else
            {
                Color greenBg = new Color(0.08f, 0.72f, 0.45f);
                Color greenHighlight = new Color(0.45f, 0.95f, 0.65f, 0.35f);
                GameObject adBtnObj = CreateMenuGridButton(card.transform, "AdGridButton", "🎬", LocalizationManager.Get("menu_ad_line1"), LocalizationManager.Get("menu_ad_line2"), new Vector2(colRightX, row2Y), gridSize, greenBg, greenHighlight);
                adBtnObj.GetComponent<Button>().onClick.AddListener(adRewardAction);
            }

            // 7. Gizlilik Politikası / Privacy Policy Butonu (App Store & Google Play Store Kuralı)
            GameObject privacyBtnObj = new GameObject("PrivacyPolicyButton");
            privacyBtnObj.transform.SetParent(card.transform, false);
            RectTransform privRt = privacyBtnObj.AddComponent<RectTransform>();
            privRt.sizeDelta = new Vector2(400f, 44f);
            privRt.anchoredPosition = new Vector2(0f, -425f);

            Text privTxt = privacyBtnObj.AddComponent<Text>();
            privTxt.font = GetSafeDefaultFont();
            privTxt.fontSize = 20;
            privTxt.fontStyle = FontStyle.Normal;
            privTxt.alignment = TextAnchor.MiddleCenter;
            privTxt.color = new Color(0.55f, 0.70f, 0.88f, 0.85f);
            privTxt.raycastTarget = true;
            privTxt.text = $"🔒  {LocalizationManager.Get("menu_privacy_policy", "Gizlilik Politikası")}";

            Button privBtn = privacyBtnObj.AddComponent<Button>();
            privBtn.targetGraphic = privTxt;
            ColorBlock pcb = privBtn.colors;
            pcb.normalColor = Color.white;
            pcb.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
            pcb.pressedColor = new Color(0.7f, 0.8f, 1.0f);
            privBtn.colors = pcb;
            privBtn.onClick.AddListener(() => {
                AudioManager.Instance?.PlayJumpSound();
                Application.OpenURL("https://github.com/YusufYildirimDev/ZiplaKac/blob/main/PRIVACY_POLICY.md");
            });

            // Anında dil değiştiğinde ana menü görsellerini güncelle
            LocalizationManager.OnLanguageChanged += () => {
                if (subImg != null) subImg.sprite = LoadMainMenuSprite("subtitle_hoppzy");
                if (subTxt != null) subTxt.text = LocalizationManager.Get("app_subtitle");
                if (divImg != null) divImg.sprite = LoadMainMenuSprite("divider_karakter_sec");
                if (playImg != null) playImg.sprite = LoadMainMenuSprite("btn_oyna");
                if (playTxt != null) playTxt.text = LocalizationManager.Get("menu_play");
                if (wardrobeImg != null) wardrobeImg.sprite = LoadMainMenuSprite("btn_wardrobe");
                if (rewardImg != null) rewardImg.sprite = LoadMainMenuSprite("btn_reward");
                if (rulesImg != null) rulesImg.sprite = LoadMainMenuSprite("btn_rules");
                if (adImg != null) adImg.sprite = LoadMainMenuSprite("btn_ad");
                if (privTxt != null) privTxt.text = $"🔒  {LocalizationManager.Get("menu_privacy_policy", "Gizlilik Politikası")}";
            };

            if (UIManager.Instance != null) UIManager.Instance.mainMenuPanel = panel;
        }

        private void CreateHUDPanel(Transform parent)
        {
            GameObject panel = new GameObject("HUDPanel");
            panel.transform.SetParent(parent, false);
            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            HUDController hud = panel.AddComponent<HUDController>();

            // SafeArea Konteyneri (Çentik, Dynamic Island ve alt bar koruması)
            GameObject safeArea = UI.SafeAreaFitter.Create(panel.transform, "HUD_SafeArea");

            // Top Header Bar — Premium Arcade Glassmorphic Badges
            // 1. Anlık Skor (Sol Üst Neon Mint Badge - Güvenli Alan Köşesine Sabitli)
            Sprite scoreIcon = ProceduralSpriteGenerator.CreateScoreStarSprite();
            CreateHUDBadge(safeArea.transform, "ScorePill", new Vector2(340f, 96f), new Vector2(30f, -28f),
                new Color(0.06f, 0.09f, 0.18f, 0.94f), new Color(0.024f, 0.839f, 0.627f, 0.95f), 3.0f,
                scoreIcon, "<size=20><color=#8da4c4>SKOR</color></size>\n<size=42><color=#ffffff><b>0</b></color></size>",
                Color.white, 24, out hud.currentScoreText, new Vector2(0f, 1f), new Vector2(0f, 1f));

            // 2. Altın Rozeti (Sol Üst Skorun altında — Parlak Altın Sikke Rozeti)
            Sprite coinIcon = ProceduralSpriteGenerator.CreateGoldCoinSprite();
            CreateHUDBadge(safeArea.transform, "GoldPill", new Vector2(280f, 86f), new Vector2(30f, -136f),
                new Color(0.09f, 0.08f, 0.14f, 0.94f), new Color(1.0f, 0.82f, 0.25f, 0.95f), 3.0f,
                coinIcon, "<size=18><color=#ffd166>ALTIN</color></size>\n<size=38><color=#ffea79><b>0</b></color></size>",
                new Color(1.0f, 0.92f, 0.45f), 22, out hud.goldText, new Vector2(0f, 1f), new Vector2(0f, 1f));

            // 3. En İyi Skor (Sağ Üst — Parlak Altın Kupa / Rekor Rozeti)
            Sprite trophyIcon = ProceduralSpriteGenerator.CreateTrophyIconSprite();
            CreateHUDBadge(safeArea.transform, "HighScorePill", new Vector2(340f, 96f), new Vector2(-30f, -28f),
                new Color(0.08f, 0.07f, 0.16f, 0.94f), new Color(0.98f, 0.75f, 0.18f, 0.95f), 3.0f,
                trophyIcon, "<size=20><color=#ffd166>EN İYİ</color></size>\n<size=42><color=#ffffff><b>0</b></color></size>",
                Color.white, 24, out hud.highScoreText, new Vector2(1f, 1f), new Vector2(1f, 1f));

            // 4. Can Rozeti (Sağ Üst En İyi Skorun altında — Pembe/Kırmızı Kalp Rozeti)
            Sprite heartIcon = ProceduralSpriteGenerator.CreateHeartIconSprite(64);
            GameObject lifeBadgeObj = CreateHUDBadge(safeArea.transform, "LifePill", new Vector2(280f, 86f), new Vector2(-30f, -136f),
                new Color(0.14f, 0.06f, 0.09f, 0.94f), new Color(1.0f, 0.35f, 0.45f, 0.95f), 3.0f,
                heartIcon, "<size=18><color=#ff6b81>CAN</color></size>\n<size=38><color=#ffffff><b>0</b></color></size>",
                new Color(1.0f, 0.85f, 0.90f), 22, out hud.lifeText, new Vector2(1f, 1f), new Vector2(1f, 1f));
            hud.lifeBadgeObject = lifeBadgeObj;

            // Active PowerUp Badges Bar (Üst Orta - İki rozet arası)
            GameObject badgeBar = new GameObject("PowerUpBadgeBar");
            badgeBar.transform.SetParent(safeArea.transform, false);
            RectTransform barRt = badgeBar.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.5f, 1f);
            barRt.anchorMax = new Vector2(0.5f, 1f);
            barRt.pivot = new Vector2(0.5f, 1f);
            barRt.anchoredPosition = new Vector2(0f, -28f);
            barRt.sizeDelta = new Vector2(300f, 80f);

            hud.shieldBadgeText = CreateText(badgeBar.transform, "ShieldBadge", "Kalkan", 26, new Vector2(-95f, -40f), new Color(0.30f, 0.79f, 0.94f), false, FontStyle.Bold);
            hud.shieldBadgeObject = hud.shieldBadgeText.gameObject;
            hud.shieldBadgeObject.SetActive(false);

            hud.slowBadgeText = CreateText(badgeBar.transform, "SlowBadge", "Yavaş", 26, new Vector2(0f, -40f), new Color(0.56f, 0.88f, 0.94f), false, FontStyle.Bold);
            hud.slowBadgeObject = hud.slowBadgeText.gameObject;
            hud.slowBadgeObject.SetActive(false);

            hud.multiBadgeText = CreateText(badgeBar.transform, "MultiBadge", "x2 Puan", 26, new Vector2(95f, -40f), new Color(1.0f, 0.84f, 0.04f), false, FontStyle.Bold);
            hud.multiBadgeObject = hud.multiBadgeText.gameObject;
            hud.multiBadgeObject.SetActive(false);

            // Merkez Güçlendirme Geri Sayaç (Ekranın Tam Ortasında Büyük Halka/Panel)
            GameObject countdownPanel = new GameObject("PowerUpCountdownPanel");
            countdownPanel.transform.SetParent(safeArea.transform, false);
            RectTransform cdRt = countdownPanel.AddComponent<RectTransform>();
            cdRt.anchorMin = new Vector2(0.5f, 0.5f);
            cdRt.anchorMax = new Vector2(0.5f, 0.5f);
            cdRt.pivot = new Vector2(0.5f, 0.5f);
            cdRt.anchoredPosition = new Vector2(0f, 60f);
            cdRt.sizeDelta = new Vector2(240f, 240f);

            Image ringImg = countdownPanel.AddComponent<Image>();
            ringImg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(
                new Color(0.08f, 0.10f, 0.22f, 0.90f),
                new Color(0.30f, 0.82f, 0.94f, 1f),
                4f, 120, 240, 240);
            ringImg.raycastTarget = false;

            Text numText = CreateText(countdownPanel.transform, "CountdownNumber", "5", 96, new Vector2(0f, 20f), Color.white, false, FontStyle.Bold);
            Shadow numShadow = numText.gameObject.AddComponent<Shadow>();
            numShadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            numShadow.effectDistance = new Vector2(3f, -3f);

            Text lblText = CreateText(countdownPanel.transform, "CountdownLabel", "YAVAŞ", 24, new Vector2(0f, -65f), new Color(1.0f, 0.84f, 0.04f), false, FontStyle.Bold);

            hud.powerupCountdownPanel = countdownPanel;
            hud.countdownNumberText = numText;
            hud.countdownLabelText = lblText;
            hud.countdownRingImage = ringImg;
            countdownPanel.SetActive(false);

            // Alt Kontrol İpucu (Güvenli Alan Altına Sabitli)
            hud.controlTipText = CreateText(safeArea.transform, "ControlTipText", "Zıpla: Dokun / Space", 24, Vector2.zero, new Color(0.6f, 0.7f, 0.85f, 0.7f), false);
            RectTransform tipRt = hud.controlTipText.GetComponent<RectTransform>();
            tipRt.anchorMin = new Vector2(0.5f, 0f);
            tipRt.anchorMax = new Vector2(0.5f, 0f);
            tipRt.pivot = new Vector2(0.5f, 0f);
            tipRt.anchoredPosition = new Vector2(0f, 28f);

            // Kombo Bildirim Kartı (Ekranın Üst-Orta Kısmı, Y: -135)
            GameObject comboCard = CreateCardContainer(safeArea.transform, "ComboNotificationCard", new Vector2(460f, 84f));
            RectTransform comboRt = comboCard.GetComponent<RectTransform>();
            comboRt.anchorMin = new Vector2(0.5f, 1f);
            comboRt.anchorMax = new Vector2(0.5f, 1f);
            comboRt.pivot = new Vector2(0.5f, 1f);
            comboRt.anchoredPosition = new Vector2(0f, -135f);

            Image comboBg = comboCard.GetComponent<Image>();
            comboBg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(
                new Color(0.08f, 0.10f, 0.22f, 0.92f),
                new Color(1.0f, 0.84f, 0.04f, 0.85f),
                3f, 26, 128, 64);
            comboBg.raycastTarget = false;

            CanvasGroup comboCg = comboCard.AddComponent<CanvasGroup>();
            comboCg.alpha = 0f;
            comboCg.blocksRaycasts = false;
            comboCg.interactable = false;

            Text comboText = CreateText(comboCard.transform, "ComboText", "KOMBO x5! +10", 38, Vector2.zero, new Color(1.0f, 0.84f, 0.04f), false, FontStyle.Bold);
            comboText.horizontalOverflow = HorizontalWrapMode.Overflow;
            comboText.verticalOverflow = VerticalWrapMode.Overflow;

            Shadow comboShadow = comboText.gameObject.AddComponent<Shadow>();
            comboShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            comboShadow.effectDistance = new Vector2(2f, -2f);

            hud.comboBannerObject = comboCard;
            hud.comboBannerText = comboText;
            hud.comboCanvasGroup = comboCg;
            comboCard.SetActive(false);

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

            // Ana Kart (920x1340 px, #18263b - Ekran Orantısı Tam Dengeli)
            GameObject card = new GameObject("GameOverCard");
            card.transform.SetParent(panel.transform, false);
            RectTransform cardRt = card.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(920f, 1340f);
            cardRt.anchoredPosition = Vector2.zero;
            card.AddComponent<UI.ResponsiveCardFitter>();

            Color cardBg = new Color(0.094f, 0.149f, 0.231f, 0.98f);
            Color cardBorder = new Color(0.18f, 0.28f, 0.40f, 0.55f);
            Image cardImg = card.AddComponent<Image>();
            cardImg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(cardBg, cardBorder, 2f, 44, 128, 128);
            cardImg.type = Image.Type.Sliced;
            cardImg.raycastTarget = true;

            // 1. BAŞLIK VE AKSANLAR
            // Orta Başlık: "OYUN BİTTİ" (64pt Bold)
            goUI.gameOverTitleText = CreateText(card.transform, "GameOverTitle", LocalizationManager.Get("go_title"), 64, new Vector2(0f, 420f), Color.white, false, FontStyle.Bold);

            // Sarı Çizgisel Aksanlar (Title Decor - Sol ve Sağ)
            GameObject accentL = new GameObject("AccentLeft");
            accentL.transform.SetParent(card.transform, false);
            RectTransform aLRt = accentL.AddComponent<RectTransform>();
            aLRt.anchoredPosition = new Vector2(-225f, 420f);
            aLRt.sizeDelta = new Vector2(50f, 36f);
            Image aLImg = accentL.AddComponent<Image>();
            aLImg.sprite = ProceduralSpriteGenerator.CreateTitleAccentSprite(true);
            aLImg.preserveAspect = true;
            aLImg.raycastTarget = false;

            GameObject accentR = new GameObject("AccentRight");
            accentR.transform.SetParent(card.transform, false);
            RectTransform aRRt = accentR.AddComponent<RectTransform>();
            aRRt.anchoredPosition = new Vector2(225f, 420f);
            aRRt.sizeDelta = new Vector2(50f, 36f);
            Image aRImg = accentR.AddComponent<Image>();
            aRImg.sprite = ProceduralSpriteGenerator.CreateTitleAccentSprite(false);
            aRImg.preserveAspect = true;
            aRImg.raycastTarget = false;

            // Alt Yazı: "Bir engele çarptın" (32pt)
            goUI.gameOverSubText = CreateText(card.transform, "GameOverSub", LocalizationManager.Get("go_sub"), 32, new Vector2(0f, 355f), new Color(0.49f, 0.62f, 0.74f), false);

            // 2. SKOR KUTUSU (Recessed Dark Blue Box: 820x260 px, #111b2b)
            GameObject scoreBox = new GameObject("ScoreBox");
            scoreBox.transform.SetParent(card.transform, false);
            RectTransform sbRt = scoreBox.AddComponent<RectTransform>();
            sbRt.anchoredPosition = new Vector2(0f, 175f);
            sbRt.sizeDelta = new Vector2(820f, 260f);

            Image sbBg = scoreBox.AddComponent<Image>();
            sbBg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(
                new Color(0.067f, 0.106f, 0.169f, 1f),
                new Color(0.13f, 0.20f, 0.30f, 0.75f),
                1.5f, 30, 128, 128
            );
            sbBg.type = Image.Type.Sliced;
            sbBg.raycastTarget = false;

            // Skor Kutusu İçi - Üst Etiket (28pt)
            goUI.gameOverScoreLabelText = CreateText(scoreBox.transform, "ScoreLabel", LocalizationManager.Get("go_score_label"), 28, new Vector2(0f, 80f), new Color(0.45f, 0.57f, 0.70f), false, FontStyle.Bold);

            // Skor Kutusu İçi - Büyük Beyaz Skor Rakamı (128pt Bold)
            goUI.finalScoreText = CreateText(scoreBox.transform, "FinalScoreText", "0", 128, new Vector2(0f, 4f), Color.white, false, FontStyle.Bold);

            // Skor Kutusu İçi - En İyi Skor (Sarı/Altın, 32pt)
            goUI.highScoreText = CreateText(scoreBox.transform, "HighScoreText", "En İyi Skor: 0", 32, new Vector2(0f, -80f), new Color(0.96f, 0.77f, 0.27f), false, FontStyle.Bold);

            // Yeni Rekor Rozeti
            GameObject recBadge = new GameObject("NewRecordBadge");
            recBadge.transform.SetParent(card.transform, false);
            RectTransform recRt = recBadge.AddComponent<RectTransform>();
            recRt.anchoredPosition = new Vector2(0f, 320f);
            recRt.sizeDelta = new Vector2(300f, 34f);
            Image recBg = recBadge.AddComponent<Image>();
            recBg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(new Color(0.96f, 0.77f, 0.27f), Color.white, 1f, 16, 128, 64);
            recBg.type = Image.Type.Sliced;
            recBg.raycastTarget = false;
            Text recTxt = CreateText(recBadge.transform, "RecText", "★ YENİ REKOR! ★", 20, Vector2.zero, new Color(0.06f, 0.09f, 0.16f), false, FontStyle.Bold);
            recTxt.rectTransform.sizeDelta = new Vector2(300f, 34f);
            goUI.newRecordBadge = recBadge;
            goUI.newRecordBadge.SetActive(false);

            // 3. ALTIN & KASA BİLGİ ŞERİDİ (840x64 px)
            GameObject earnBanner = new GameObject("EarningsBanner");
            earnBanner.transform.SetParent(card.transform, false);
            RectTransform ebRt = earnBanner.AddComponent<RectTransform>();
            ebRt.anchoredPosition = new Vector2(0f, -6f);
            ebRt.sizeDelta = new Vector2(840f, 64f);

            GameObject ebContent = new GameObject("Content");
            ebContent.transform.SetParent(earnBanner.transform, false);
            RectTransform ebcRt = ebContent.AddComponent<RectTransform>();
            ebcRt.anchorMin = Vector2.zero; ebcRt.anchorMax = Vector2.one;
            ebcRt.offsetMin = Vector2.zero; ebcRt.offsetMax = Vector2.zero;

            HorizontalLayoutGroup ebHlg = ebContent.AddComponent<HorizontalLayoutGroup>();
            ebHlg.childAlignment = TextAnchor.MiddleCenter;
            ebHlg.spacing = 12f;
            ebHlg.childControlWidth = false;
            ebHlg.childControlHeight = false;
            ebHlg.childForceExpandWidth = false;
            ebHlg.childForceExpandHeight = false;

            GameObject ebCoin = new GameObject("CoinIcon");
            ebCoin.transform.SetParent(ebContent.transform, false);
            RectTransform ebCoinRt = ebCoin.AddComponent<RectTransform>();
            ebCoinRt.sizeDelta = new Vector2(40f, 40f);
            Image ebCoinImg = ebCoin.AddComponent<Image>();
            ebCoinImg.sprite = ProceduralSpriteGenerator.CreateGoldCoinSprite(64);
            ebCoinImg.preserveAspect = true;
            ebCoinImg.raycastTarget = false;

            GameObject ebTextObj = new GameObject("GoldEarnedText");
            ebTextObj.transform.SetParent(ebContent.transform, false);
            RectTransform ebTxtRt = ebTextObj.AddComponent<RectTransform>();
            ebTxtRt.sizeDelta = new Vector2(760f, 54f);
            goUI.goldEarnedText = ebTextObj.AddComponent<Text>();
            goUI.goldEarnedText.font = GetSafeDefaultFont();
            goUI.goldEarnedText.fontSize = 28;
            goUI.goldEarnedText.fontStyle = FontStyle.Bold;
            goUI.goldEarnedText.color = Color.white;
            goUI.goldEarnedText.supportRichText = true;
            goUI.goldEarnedText.text = "+<color=#f6c445><b>0</b></color> <color=#cbd5e1><b>ALTIN KAZANDIN</b></color>    <color=#334a66>|</color>    <color=#7e9ebc><b>KASA:</b></color> <color=#f6c445><b>0</b></color> <color=#cbd5e1><b>ALTIN</b></color>";
            goUI.goldEarnedText.alignment = TextAnchor.MiddleLeft;
            goUI.goldEarnedText.resizeTextForBestFit = true;
            goUI.goldEarnedText.resizeTextMinSize = 20;
            goUI.goldEarnedText.resizeTextMaxSize = 28;
            goUI.goldEarnedText.horizontalOverflow = HorizontalWrapMode.Wrap;
            goUI.goldEarnedText.verticalOverflow = VerticalWrapMode.Truncate;
            goUI.goldEarnedText.raycastTarget = false;

            // ==========================================
            // AKSİYON BUTONLARI (3 SATIR - MOCKUP'A BİREBİR)
            // ==========================================

            // SATIR 1: "TEKRAR OYNA" (Safir Mavi #1e7af2, 820x118 px)
            Color primaryBlue = new Color(0.12f, 0.48f, 0.95f);
            GameObject restartBtn = new GameObject("RestartButton");
            restartBtn.transform.SetParent(card.transform, false);
            RectTransform resRt = restartBtn.AddComponent<RectTransform>();
            resRt.anchoredPosition = new Vector2(0f, -125f);
            resRt.sizeDelta = new Vector2(820f, 118f);

            Image resBg = restartBtn.AddComponent<Image>();
            resBg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(primaryBlue, Color.clear, 0f, 32, 128, 64);
            resBg.type = Image.Type.Sliced;

            Button resBtnComp = restartBtn.AddComponent<Button>();
            resBtnComp.targetGraphic = resBg;
            ColorBlock resColors = resBtnComp.colors;
            resColors.normalColor = Color.white;
            resColors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            resColors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            resBtnComp.colors = resColors;
            goUI.restartButton = resBtnComp;

            // İçerik: Merkeze kilitli kapsayıcı (Play İkonu + Yazı)
            GameObject resContent = new GameObject("Content");
            resContent.transform.SetParent(restartBtn.transform, false);
            RectTransform rescRt = resContent.AddComponent<RectTransform>();
            rescRt.anchorMin = new Vector2(0.5f, 0.5f);
            rescRt.anchorMax = new Vector2(0.5f, 0.5f);
            rescRt.pivot = new Vector2(0.5f, 0.5f);
            rescRt.anchoredPosition = Vector2.zero;
            rescRt.sizeDelta = new Vector2(400f, 60f);

            GameObject playIconObj = new GameObject("PlayIcon");
            playIconObj.transform.SetParent(resContent.transform, false);
            RectTransform playRt = playIconObj.AddComponent<RectTransform>();
            playRt.anchorMin = new Vector2(0f, 0.5f);
            playRt.anchorMax = new Vector2(0f, 0.5f);
            playRt.pivot = new Vector2(0f, 0.5f);
            playRt.anchoredPosition = new Vector2(0f, 0f);
            playRt.sizeDelta = new Vector2(36f, 36f);
            Image playImg = playIconObj.AddComponent<Image>();
            playImg.sprite = ProceduralSpriteGenerator.CreatePlayIconSprite(64);
            playImg.preserveAspect = true;
            playImg.raycastTarget = false;

            GameObject resTxtObj = new GameObject("Text");
            resTxtObj.transform.SetParent(resContent.transform, false);
            RectTransform resTxtRt = resTxtObj.AddComponent<RectTransform>();
            resTxtRt.anchorMin = new Vector2(0f, 0.5f);
            resTxtRt.anchorMax = new Vector2(0f, 0.5f);
            resTxtRt.pivot = new Vector2(0f, 0.5f);
            resTxtRt.anchoredPosition = new Vector2(52f, 0f);
            resTxtRt.sizeDelta = new Vector2(340f, 60f);

            Text resText = resTxtObj.AddComponent<Text>();
            resText.font = GetSafeDefaultFont();
            resText.text = "TEKRAR OYNA";
            resText.fontSize = 38;
            resText.fontStyle = FontStyle.Bold;
            resText.color = Color.white;
            resText.alignment = TextAnchor.MiddleLeft;
            resText.resizeTextForBestFit = true;
            resText.resizeTextMinSize = 26;
            resText.resizeTextMaxSize = 40;
            resText.horizontalOverflow = HorizontalWrapMode.Wrap;
            resText.verticalOverflow = VerticalWrapMode.Truncate;
            resText.raycastTarget = false;
            goUI.restartButtonText = resText;

            // SATIR 2: İKİLİ YEŞİL KAPSÜL BUTON (Zümrüt Yeşili #10b580, 820x118 px)
            Color emeraldGreen = new Color(0.06f, 0.71f, 0.50f);
            GameObject dualCapsule = new GameObject("DualActionCapsule");
            dualCapsule.transform.SetParent(card.transform, false);
            RectTransform dcRt = dualCapsule.AddComponent<RectTransform>();
            dcRt.anchoredPosition = new Vector2(0f, -265f);
            dcRt.sizeDelta = new Vector2(820f, 118f);

            Image dcBg = dualCapsule.AddComponent<Image>();
            dcBg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(emeraldGreen, Color.clear, 0f, 32, 128, 64);
            dcBg.type = Image.Type.Sliced;
            dcBg.raycastTarget = false;

            // Sol Yarı: "REKLAM İZLE" Butonu (Genişlik: 410px)
            GameObject adHalf = new GameObject("RewardedAdButton");
            adHalf.transform.SetParent(dualCapsule.transform, false);
            RectTransform adRt = adHalf.AddComponent<RectTransform>();
            adRt.anchorMin = new Vector2(0f, 0f);
            adRt.anchorMax = new Vector2(0.5f, 1f);
            adRt.offsetMin = Vector2.zero;
            adRt.offsetMax = Vector2.zero;

            Image adHit = adHalf.AddComponent<Image>();
            adHit.color = Color.clear;
            Button adBtnComp = adHalf.AddComponent<Button>();
            adBtnComp.targetGraphic = adHit;
            ColorBlock adColors = adBtnComp.colors;
            adColors.normalColor = Color.white;
            adColors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            adColors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            adBtnComp.colors = adColors;
            goUI.rewardedAdButton = adBtnComp;

            GameObject adContent = new GameObject("Content");
            adContent.transform.SetParent(adHalf.transform, false);
            RectTransform adcRt = adContent.AddComponent<RectTransform>();
            adcRt.anchorMin = new Vector2(0.5f, 0.5f);
            adcRt.anchorMax = new Vector2(0.5f, 0.5f);
            adcRt.pivot = new Vector2(0.5f, 0.5f);
            adcRt.anchoredPosition = Vector2.zero;
            adcRt.sizeDelta = new Vector2(330f, 60f);

            GameObject videoIconObj = new GameObject("VideoIcon");
            videoIconObj.transform.SetParent(adContent.transform, false);
            RectTransform videoRt = videoIconObj.AddComponent<RectTransform>();
            videoRt.anchorMin = new Vector2(0f, 0.5f);
            videoRt.anchorMax = new Vector2(0f, 0.5f);
            videoRt.pivot = new Vector2(0f, 0.5f);
            videoRt.anchoredPosition = new Vector2(0f, 0f);
            videoRt.sizeDelta = new Vector2(46f, 35f);
            Image videoImg = videoIconObj.AddComponent<Image>();
            videoImg.sprite = ProceduralSpriteGenerator.CreateVideoIconSprite(64);
            videoImg.preserveAspect = true;
            videoImg.raycastTarget = false;

            GameObject adTxtObj = new GameObject("Text");
            adTxtObj.transform.SetParent(adContent.transform, false);
            RectTransform adTxtRt = adTxtObj.AddComponent<RectTransform>();
            adTxtRt.anchorMin = new Vector2(0f, 0.5f);
            adTxtRt.anchorMax = new Vector2(0f, 0.5f);
            adTxtRt.pivot = new Vector2(0f, 0.5f);
            adTxtRt.anchoredPosition = new Vector2(56f, 0f);
            adTxtRt.sizeDelta = new Vector2(270f, 56f);

            Text adText = adTxtObj.AddComponent<Text>();
            adText.font = GetSafeDefaultFont();
            adText.text = "REKLAM İZLE";
            adText.fontSize = 34;
            adText.fontStyle = FontStyle.Bold;
            adText.color = Color.white;
            adText.alignment = TextAnchor.MiddleLeft;
            adText.resizeTextForBestFit = true;
            adText.resizeTextMinSize = 22;
            adText.resizeTextMaxSize = 36;
            adText.horizontalOverflow = HorizontalWrapMode.Wrap;
            adText.verticalOverflow = VerticalWrapMode.Truncate;
            adText.raycastTarget = false;
            goUI.rewardedAdButtonText = adText;

            // Orta Dikey Ayırıcı Çizgi
            GameObject dividerObj = new GameObject("Divider");
            dividerObj.transform.SetParent(dualCapsule.transform, false);
            RectTransform divRt = dividerObj.AddComponent<RectTransform>();
            divRt.anchorMin = new Vector2(0.5f, 0.5f);
            divRt.anchorMax = new Vector2(0.5f, 0.5f);
            divRt.anchoredPosition = Vector2.zero;
            divRt.sizeDelta = new Vector2(2f, 60f);
            Image divImg = dividerObj.AddComponent<Image>();
            divImg.color = new Color(1f, 1f, 1f, 0.30f);
            divImg.raycastTarget = false;

            // Sağ Yarı: "CAN AL DEVAM ET" Butonu (Genişlik: 410px)
            GameObject reviveHalf = new GameObject("ReviveButton");
            reviveHalf.transform.SetParent(dualCapsule.transform, false);
            RectTransform revRt = reviveHalf.AddComponent<RectTransform>();
            revRt.anchorMin = new Vector2(0.5f, 0f);
            revRt.anchorMax = new Vector2(1f, 1f);
            revRt.offsetMin = Vector2.zero;
            revRt.offsetMax = Vector2.zero;

            Image revHit = reviveHalf.AddComponent<Image>();
            revHit.color = Color.clear;
            Button revBtnComp = reviveHalf.AddComponent<Button>();
            revBtnComp.targetGraphic = revHit;
            ColorBlock revColors = revBtnComp.colors;
            revColors.normalColor = Color.white;
            revColors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            revColors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            revBtnComp.colors = revColors;
            goUI.reviveButton = revBtnComp;

            GameObject revContent = new GameObject("Content");
            revContent.transform.SetParent(reviveHalf.transform, false);
            RectTransform revcRt = revContent.AddComponent<RectTransform>();
            revcRt.anchorMin = new Vector2(0.5f, 0.5f);
            revcRt.anchorMax = new Vector2(0.5f, 0.5f);
            revcRt.pivot = new Vector2(0.5f, 0.5f);
            revcRt.anchoredPosition = Vector2.zero;
            revcRt.sizeDelta = new Vector2(330f, 84f);

            GameObject heartIconObj = new GameObject("HeartIcon");
            heartIconObj.transform.SetParent(revContent.transform, false);
            RectTransform heartRt = heartIconObj.AddComponent<RectTransform>();
            heartRt.anchorMin = new Vector2(0f, 0.5f);
            heartRt.anchorMax = new Vector2(0f, 0.5f);
            heartRt.pivot = new Vector2(0f, 0.5f);
            heartRt.anchoredPosition = new Vector2(0f, 0f);
            heartRt.sizeDelta = new Vector2(46f, 46f);
            Image heartImg = heartIconObj.AddComponent<Image>();
            heartImg.sprite = ProceduralSpriteGenerator.CreateHeartIconSprite(64);
            heartImg.preserveAspect = true;
            heartImg.raycastTarget = false;
            goUI.reviveHeartIcon = heartImg;

            GameObject revTxtObj = new GameObject("Text");
            revTxtObj.transform.SetParent(revContent.transform, false);
            RectTransform revTxtRt = revTxtObj.AddComponent<RectTransform>();
            revTxtRt.anchorMin = new Vector2(0f, 0.5f);
            revTxtRt.anchorMax = new Vector2(0f, 0.5f);
            revTxtRt.pivot = new Vector2(0f, 0.5f);
            revTxtRt.anchoredPosition = new Vector2(56f, 0f);
            revTxtRt.sizeDelta = new Vector2(270f, 80f);

            Text revText = revTxtObj.AddComponent<Text>();
            revText.font = GetSafeDefaultFont();
            revText.text = "CAN AL\nDEVAM ET";
            revText.fontSize = 32;
            revText.fontStyle = FontStyle.Bold;
            revText.color = Color.white;
            revText.lineSpacing = 0.95f;
            revText.alignment = TextAnchor.MiddleLeft;
            revText.resizeTextForBestFit = true;
            revText.resizeTextMinSize = 20;
            revText.resizeTextMaxSize = 34;
            revText.horizontalOverflow = HorizontalWrapMode.Wrap;
            revText.verticalOverflow = VerticalWrapMode.Truncate;
            revText.raycastTarget = false;
            goUI.reviveButtonText = revText;

            // SATIR 3: "ANA MENÜ" (Koyu Slate #263b59, 820x112 px)
            Color slateDark = new Color(0.15f, 0.23f, 0.35f);
            GameObject menuBtn = new GameObject("MainMenuButton");
            menuBtn.transform.SetParent(card.transform, false);
            RectTransform menuRt = menuBtn.AddComponent<RectTransform>();
            menuRt.anchoredPosition = new Vector2(0f, -400f);
            menuRt.sizeDelta = new Vector2(820f, 112f);

            Image menuBg = menuBtn.AddComponent<Image>();
            menuBg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(slateDark, Color.clear, 0f, 32, 128, 64);
            menuBg.type = Image.Type.Sliced;

            Button menuBtnComp = menuBtn.AddComponent<Button>();
            menuBtnComp.targetGraphic = menuBg;
            ColorBlock menuColors = menuBtnComp.colors;
            menuColors.normalColor = Color.white;
            menuColors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            menuColors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            menuBtnComp.colors = menuColors;
            goUI.mainMenuButton = menuBtnComp;

            GameObject menuContent = new GameObject("Content");
            menuContent.transform.SetParent(menuBtn.transform, false);
            RectTransform menucRt = menuContent.AddComponent<RectTransform>();
            menucRt.anchorMin = new Vector2(0.5f, 0.5f);
            menucRt.anchorMax = new Vector2(0.5f, 0.5f);
            menucRt.pivot = new Vector2(0.5f, 0.5f);
            menucRt.anchoredPosition = Vector2.zero;
            menucRt.sizeDelta = new Vector2(360f, 60f);

            GameObject homeIconObj = new GameObject("HomeIcon");
            homeIconObj.transform.SetParent(menuContent.transform, false);
            RectTransform homeRt = homeIconObj.AddComponent<RectTransform>();
            homeRt.anchorMin = new Vector2(0f, 0.5f);
            homeRt.anchorMax = new Vector2(0f, 0.5f);
            homeRt.pivot = new Vector2(0f, 0.5f);
            homeRt.anchoredPosition = new Vector2(0f, 0f);
            homeRt.sizeDelta = new Vector2(46f, 46f);
            Image homeImg = homeIconObj.AddComponent<Image>();
            homeImg.sprite = ProceduralSpriteGenerator.CreateHomeIconSprite(64);
            homeImg.preserveAspect = true;
            homeImg.raycastTarget = false;

            GameObject menuTxtObj = new GameObject("Text");
            menuTxtObj.transform.SetParent(menuContent.transform, false);
            RectTransform menuTxtRt = menuTxtObj.AddComponent<RectTransform>();
            menuTxtRt.anchorMin = new Vector2(0f, 0.5f);
            menuTxtRt.anchorMax = new Vector2(0f, 0.5f);
            menuTxtRt.pivot = new Vector2(0f, 0.5f);
            menuTxtRt.anchoredPosition = new Vector2(56f, 0f);
            menuTxtRt.sizeDelta = new Vector2(300f, 56f);

            Text menuText = menuTxtObj.AddComponent<Text>();
            menuText.font = GetSafeDefaultFont();
            menuText.text = "ANA MENÜ";
            menuText.fontSize = 38;
            menuText.fontStyle = FontStyle.Bold;
            menuText.color = Color.white;
            menuText.alignment = TextAnchor.MiddleLeft;
            menuText.resizeTextForBestFit = true;
            menuText.resizeTextMinSize = 26;
            menuText.resizeTextMaxSize = 40;
            menuText.horizontalOverflow = HorizontalWrapMode.Wrap;
            menuText.verticalOverflow = VerticalWrapMode.Truncate;
            menuText.raycastTarget = false;
            goUI.mainMenuButtonText = menuText;

            panel.SetActive(false);
            if (UIManager.Instance != null) UIManager.Instance.gameOverPanel = panel;
        }

        private void CreateDailyRewardModal(Transform parent)
        {
            GameObject modal = new GameObject("DailyRewardModal");
            modal.transform.SetParent(parent, false);
            RectTransform rt = modal.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            Image bg = modal.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.04f, 0.08f, 0.88f); // Koyu arka plan karartması
            bg.raycastTarget = true;

            UI.DailyRewardModalUI modalUI = modal.AddComponent<UI.DailyRewardModalUI>();

            // Modal Card (940x1320) - Referans tasarıma birebir uygun modern koyu kart
            GameObject card = CreateCardContainer(modal.transform, "RewardCard", new Vector2(940f, 1320f));
            card.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            card.AddComponent<UI.ResponsiveCardFitter>();
            Image cardImg = card.GetComponent<Image>();
            cardImg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(
                new Color(0.051f, 0.106f, 0.196f, 0.98f), // #0D1B32
                new Color(0.125f, 0.247f, 0.420f, 0.40f), // #203F6B
                2.0f, 40, 256, 256);

            // Başlık (Üstte - Büyük ve Belirgin, Altın Sarısı #FFC700, 52pt)
            modalUI.titleText = CreateText(card.transform, "Title", "GÜNLÜK ÖDÜL SANDIĞI", 52, new Vector2(0f, 520f), new Color(1.0f, 0.784f, 0.05f), false, FontStyle.Bold);

            // Sprite Kaynaklarını Yükle
            modalUI.closedChestSprite = Resources.Load<Sprite>("Sprites/UI_DailyReward/chest_closed");
            modalUI.openChestSprite = Resources.Load<Sprite>("Sprites/UI_DailyReward/chest_open");
            modalUI.claimOpenSprite = Resources.Load<Sprite>("Sprites/UI_DailyReward/btn_sandigi_ac");
            modalUI.claimClaimedSprite = Resources.Load<Sprite>("Sprites/UI_DailyReward/btn_harika");
            Sprite closeBtnSp = Resources.Load<Sprite>("Sprites/UI_DailyReward/btn_kapat");
            Sprite coinSp = Resources.Load<Sprite>("Sprites/UI_DailyReward/icon_coin_streak");

            // Sandık Görseli (640x498 - Referans oranına birebir hizalı)
            GameObject chestObj = new GameObject("ChestImage");
            chestObj.transform.SetParent(card.transform, false);
            RectTransform chestRt = chestObj.AddComponent<RectTransform>();
            chestRt.anchoredPosition = new Vector2(0f, 180f);
            chestRt.sizeDelta = new Vector2(640f, 498f);
            Image chestImg = chestObj.AddComponent<Image>();
            chestImg.sprite = SaveSystem.CanClaimDailyReward() ? modalUI.closedChestSprite : modalUI.openChestSprite;
            chestImg.preserveAspect = true;
            chestImg.raycastTarget = false;
            modalUI.chestImage = chestImg;

            // Açıklama Metni: "Her gün oyuna gir, sandığını aç ve bonus altınları topla!"
            modalUI.descriptionText = CreateText(card.transform, "Desc", LocalizationManager.Get("reward_desc"), 30, new Vector2(0f, -115f), new Color(0.62f, 0.72f, 0.84f), false);
            RectTransform descRt = modalUI.descriptionText.GetComponent<RectTransform>();
            descRt.sizeDelta = new Vector2(800f, 85f);
            modalUI.descriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
            modalUI.descriptionText.verticalOverflow = VerticalWrapMode.Truncate;
            modalUI.descriptionText.resizeTextForBestFit = true;
            modalUI.descriptionText.resizeTextMinSize = 18;
            modalUI.descriptionText.resizeTextMaxSize = 28;
            modalUI.descriptionText.lineSpacing = 1.15f;
            modalUI.descriptionText.alignment = TextAnchor.MiddleCenter;

            // Durum / Seri Satırı Konteyneri (İkon + Metin hizalama)
            GameObject statusContainer = new GameObject("StatusContainer");
            statusContainer.transform.SetParent(card.transform, false);
            RectTransform statusRt = statusContainer.AddComponent<RectTransform>();
            statusRt.anchoredPosition = new Vector2(0f, -195f);
            statusRt.sizeDelta = new Vector2(860f, 55f);

            HorizontalLayoutGroup hlg = statusContainer.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 14f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // Coin İkonu (Seri ödülü gösterildiğinde aktif)
            GameObject coinIconObj = new GameObject("CoinIcon");
            coinIconObj.transform.SetParent(statusContainer.transform, false);
            RectTransform coinRt = coinIconObj.AddComponent<RectTransform>();
            coinRt.sizeDelta = new Vector2(44f, 44f);
            Image coinImg = coinIconObj.AddComponent<Image>();
            coinImg.sprite = coinSp;
            coinImg.preserveAspect = true;
            coinImg.raycastTarget = false;
            modalUI.coinIconImage = coinImg;

            // Durum Metni
            GameObject statusTextObj = new GameObject("StatusText");
            statusTextObj.transform.SetParent(statusContainer.transform, false);
            RectTransform stRt = statusTextObj.AddComponent<RectTransform>();
            stRt.sizeDelta = new Vector2(760f, 50f);
            Text stText = statusTextObj.AddComponent<Text>();
            stText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            stText.fontStyle = FontStyle.Bold;
            stText.fontSize = 34;
            stText.color = new Color(0.00f, 0.87f, 0.56f); // #00DE8E
            stText.alignment = TextAnchor.MiddleCenter;
            stText.raycastTarget = false;
            modalUI.statusText = stText;

            // Sandığı Aç / Harika Butonu (600x100 - 1080x180 kapsül oranına 6:1 birebir)
            Vector2 btnSize = new Vector2(600f, 100f);
            Sprite initialClaimSp = SaveSystem.CanClaimDailyReward() ? modalUI.claimOpenSprite : modalUI.claimClaimedSprite;
            GameObject claimBtnObj = CreateSpriteButton(card.transform, "ClaimButton", initialClaimSp, new Vector2(0f, -320f), btnSize, modalUI.OnClaimButtonClicked);
            modalUI.claimButton = claimBtnObj.GetComponent<Button>();
            modalUI.claimButtonImage = claimBtnObj.GetComponent<Image>();

            // Kapat Butonu (600x100) - Referans görseldeki ✕ ikonuyla
            GameObject closeBtnObj = CreateSpriteButton(card.transform, "CloseButton", closeBtnSp, new Vector2(0f, -440f), btnSize, modalUI.CloseModal);
            modalUI.closeButton = closeBtnObj.GetComponent<Button>();
            modalUI.closeButtonImage = closeBtnObj.GetComponent<Image>();

            modal.SetActive(false);
        }

        private void CreateWardrobeModal(Transform parent)
        {
            GameObject modal = new GameObject("WardrobeModal");
            modal.transform.SetParent(parent, false);
            RectTransform rt = modal.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            Image bg = modal.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.03f, 0.06f, 0.95f); // Karartma ve odak
            bg.raycastTarget = true;

            UI.WardrobeModalUI modalUI = modal.AddComponent<UI.WardrobeModalUI>();

            // Modal Card (940x1520) - Modern Koyu Lacivert Kart (#0B1426)
            GameObject card = CreateCardContainer(modal.transform, "WardrobeCard", new Vector2(940f, 1520f));
            card.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            card.AddComponent<UI.ResponsiveCardFitter>();
            Image cardImg = card.GetComponent<Image>();
            cardImg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(
                new Color(0.05f, 0.08f, 0.16f, 0.98f),
                new Color(0.12f, 0.22f, 0.40f, 0.50f),
                2f, 32, 128, 128);

            // 1. Üst Sağ KAPAT Butonu (130x54 Kapsül)
            GameObject topCloseObj = CreatePillButton(card.transform, "TopCloseButton", LocalizationManager.Get("btn_close"), new Vector2(370f, 700f), new Vector2(130f, 54f), new Color(0.11f, 0.17f, 0.29f, 0.95f), Color.white, 22, new Color(0.20f, 0.30f, 0.45f, 0.5f), 1f, 27);
            Button topCloseBtn = topCloseObj.GetComponent<Button>();
            topCloseBtn.onClick.AddListener(modalUI.CloseModal);
            modalUI.topCloseButton = topCloseBtn;
            modalUI.topCloseButtonText = topCloseObj.GetComponentInChildren<Text>();

            // 2. Header Görseli (Sol Taraf: Gardırop / Güçler 3D İllüstrasyonu)
            GameObject hdrObj = new GameObject("HeaderIllustration");
            hdrObj.transform.SetParent(card.transform, false);
            RectTransform hdrRt = hdrObj.AddComponent<RectTransform>();
            hdrRt.anchoredPosition = new Vector2(-280f, 620f);
            hdrRt.sizeDelta = new Vector2(250f, 200f);
            Image hdrImg = hdrObj.AddComponent<Image>();
            hdrImg.preserveAspect = true;
            hdrImg.raycastTarget = false;
            modalUI.headerIllustration = hdrImg;

            // 3. Header Metinleri (İllüstrasyonun Sağında: Başlık + Altın)
            modalUI.titleText = CreateText(card.transform, "Title", "GÜÇLER &\nYÜKSELTMELER", 44, new Vector2(110f, 645f), new Color(0.75f, 0.52f, 0.99f), false, FontStyle.Bold);
            modalUI.titleText.alignment = TextAnchor.MiddleLeft;
            modalUI.titleText.GetComponent<RectTransform>().sizeDelta = new Vector2(460f, 110f);

            modalUI.totalGoldText = CreateText(card.transform, "TotalGold", "🪙 Toplam Altın: 1182", 32, new Vector2(110f, 565f), new Color(1.0f, 0.78f, 0.15f), false, FontStyle.Bold);
            modalUI.totalGoldText.alignment = TextAnchor.MiddleLeft;
            modalUI.totalGoldText.GetComponent<RectTransform>().sizeDelta = new Vector2(460f, 48f);

            // Durum / Hata Mesajı
            modalUI.statusMessageText = CreateText(card.transform, "StatusMsg", "", 24, new Vector2(110f, 525f), new Color(0.024f, 0.839f, 0.627f), false, FontStyle.Bold);
            modalUI.statusMessageText.alignment = TextAnchor.MiddleLeft;
            modalUI.statusMessageText.GetComponent<RectTransform>().sizeDelta = new Vector2(460f, 36f);

            // 4. Sekmeler Çubuğu (Tabs Bar - 3 Sekme)
            Color tabActive = new Color(0.55f, 0.36f, 0.96f);
            Color tabInactive = new Color(0.08f, 0.12f, 0.22f, 0.95f);

            GameObject tabUpgradesObj = CreatePillButton(card.transform, "TabUpgradesBtn", LocalizationManager.Get("shop_tab_upgrades"), new Vector2(-286f, 475f), new Vector2(274f, 76f), tabActive, Color.white, 24);
            modalUI.tabUpgradesButton = tabUpgradesObj.GetComponent<Button>();
            modalUI.tabUpgradesBg = tabUpgradesObj.GetComponent<Image>();
            modalUI.tabUpgradesText = tabUpgradesObj.GetComponentInChildren<Text>();
            modalUI.tabUpgradesButton.onClick.AddListener(() => modalUI.SwitchTab(UI.ShopTab.Upgrades));

            GameObject tabOutfitsObj = CreatePillButton(card.transform, "TabOutfitsBtn", LocalizationManager.Get("shop_tab_outfits"), new Vector2(0f, 475f), new Vector2(274f, 76f), tabInactive, new Color(0.58f, 0.64f, 0.72f), 24);
            modalUI.tabOutfitsButton = tabOutfitsObj.GetComponent<Button>();
            modalUI.tabOutfitsBg = tabOutfitsObj.GetComponent<Image>();
            modalUI.tabOutfitsText = tabOutfitsObj.GetComponentInChildren<Text>();
            modalUI.tabOutfitsButton.onClick.AddListener(() => modalUI.SwitchTab(UI.ShopTab.Outfits));

            GameObject tabGoldPacksObj = CreatePillButton(card.transform, "TabGoldPacksBtn", LocalizationManager.Get("shop_tab_gold_packs"), new Vector2(286f, 475f), new Vector2(274f, 76f), tabInactive, new Color(0.58f, 0.64f, 0.72f), 24);
            modalUI.tabGoldPacksButton = tabGoldPacksObj.GetComponent<Button>();
            modalUI.tabGoldPacksBg = tabGoldPacksObj.GetComponent<Image>();
            modalUI.tabGoldPacksText = tabGoldPacksObj.GetComponentInChildren<Text>();
            modalUI.tabGoldPacksButton.onClick.AddListener(() => modalUI.SwitchTab(UI.ShopTab.GoldPacks));

            // 5. Kaydırılabilir Liste Alanı (ScrollView)
            GameObject scrollViewObj = new GameObject("ScrollView");
            scrollViewObj.transform.SetParent(card.transform, false);
            RectTransform scrollRt = scrollViewObj.AddComponent<RectTransform>();
            scrollRt.anchoredPosition = new Vector2(0f, -65f);
            scrollRt.sizeDelta = new Vector2(880f, 920f);

            Image scrollBg = scrollViewObj.AddComponent<Image>();
            scrollBg.color = Color.clear;
            scrollBg.raycastTarget = true;

            scrollViewObj.AddComponent<RectMask2D>();

            ScrollRect scrollRect = scrollViewObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.scrollSensitivity = 35f;

            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(scrollViewObj.transform, false);
            RectTransform contentRt = contentObj.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0.5f, 1f);
            contentRt.anchorMax = new Vector2(0.5f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(880f, 920f);

            scrollRect.content = contentRt;
            scrollRect.viewport = scrollRt;

            modalUI.itemsContainer = contentRt;
            modalUI.itemsScrollRect = scrollRect;

            // 6. Alt Kapat Butonu (500x88)
            GameObject closeBtnObj = CreatePillButton(card.transform, "CloseButton", LocalizationManager.Get("btn_close"), new Vector2(0f, -675f), new Vector2(500f, 88f), new Color(0.11f, 0.18f, 0.30f, 0.95f), Color.white, 30, new Color(0.18f, 0.28f, 0.45f, 0.6f), 1.5f, 32);
            modalUI.closeButton = closeBtnObj.GetComponent<Button>();
            modalUI.closeButtonText = closeBtnObj.GetComponentInChildren<Text>();
            modalUI.closeButton.onClick.AddListener(modalUI.CloseModal);

            modal.SetActive(false);
        }

        private void CreateOnboardingModal(Transform parent)
        {
            GameObject modal = new GameObject("OnboardingModal");
            modal.transform.SetParent(parent, false);
            RectTransform rt = modal.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            Image bg = modal.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.03f, 0.07f, 0.94f);
            bg.raycastTarget = true;

            UI.OnboardingModalUI modalUI = modal.AddComponent<UI.OnboardingModalUI>();

            // Onboarding Card (940x1400)
            GameObject card = CreateCardContainer(modal.transform, "OnboardingCard", new Vector2(940f, 1400f));
            card.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            card.AddComponent<UI.ResponsiveCardFitter>();
            modalUI.modalCard = card;

            // 1. Üst Rozet & Başlık
            modalUI.stepBadgeText = CreateText(card.transform, "StepBadge", "REHBER • ADIM 1 / 4", 28, new Vector2(0f, 620f), new Color(0.00f, 0.898f, 1.00f), false, FontStyle.Bold);
            RectTransform sbRt = modalUI.stepBadgeText.GetComponent<RectTransform>();
            sbRt.sizeDelta = new Vector2(400f, 36f);

            modalUI.titleText = CreateText(card.transform, "TitleText", "OYUN KURALLARI & KONTROLLER", 44, new Vector2(0f, 560f), new Color(1.0f, 0.84f, 0.25f), false, FontStyle.Bold);
            RectTransform ttRt = modalUI.titleText.GetComponent<RectTransform>();
            ttRt.sizeDelta = new Vector2(860f, 54f);
            modalUI.titleText.resizeTextForBestFit = true;
            modalUI.titleText.resizeTextMinSize = 28;
            modalUI.titleText.resizeTextMaxSize = 44;

            // Sağ Üst "KAPAT" Butonu
            GameObject skipBtnObj = CreatePillButton(card.transform, "SkipButton", LocalizationManager.Get("btn_close"), Vector2.zero, new Vector2(136f, 48f), new Color(0.12f, 0.17f, 0.28f, 0.95f), Color.white, 22, new Color(0.24f, 0.36f, 0.55f, 0.6f), 1.5f, 24);
            RectTransform skipRt = skipBtnObj.GetComponent<RectTransform>();
            skipRt.anchorMin = new Vector2(1f, 1f);
            skipRt.anchorMax = new Vector2(1f, 1f);
            skipRt.pivot = new Vector2(1f, 1f);
            skipRt.anchoredPosition = new Vector2(-36f, -36f);
            modalUI.closeButton = skipBtnObj.GetComponent<Button>();
            modalUI.closeButtonText = skipBtnObj.GetComponentInChildren<Text>();
            modalUI.closeButton.onClick.AddListener(modalUI.CloseModal);

            modalUI.slides = new GameObject[4];
            modalUI.slideSubTexts = new Text[4];

            // -------------------------------------------------------------
            // SLIDE 0: OYUN KURALLARI & TEMEL MEKANİKLER
            // -------------------------------------------------------------
            GameObject slide0 = new GameObject("Slide_Rules");
            slide0.transform.SetParent(card.transform, false);
            RectTransform s0Rt = slide0.AddComponent<RectTransform>();
            s0Rt.anchorMin = Vector2.zero; s0Rt.anchorMax = Vector2.one;
            s0Rt.offsetMin = Vector2.zero; s0Rt.offsetMax = Vector2.zero;
            modalUI.slides[0] = slide0;

            modalUI.slideSubTexts[0] = CreateOnboardingSubText(slide0.transform, "Sub0", "onb_s0_sub", modalUI);

            CreateOnboardingRow(slide0.transform, "Row0_1", 285f, 168f, "Sprites/UI_Onboarding/s0_jump", "onb_s0_r1_title", "onb_s0_r1_desc", new Color(0.00f, 0.875f, 0.627f), modalUI);
            CreateOnboardingRow(slide0.transform, "Row0_2", 98f, 168f, "Sprites/UI_Onboarding/s0_double_jump", "onb_s0_r2_title", "onb_s0_r2_desc", new Color(0.22f, 0.74f, 0.97f), modalUI);
            CreateOnboardingRow(slide0.transform, "Row0_3", -89f, 168f, "Sprites/UI_Onboarding/s0_combo_coins", "onb_s0_r3_title", "onb_s0_r3_desc", new Color(0.98f, 0.75f, 0.14f), modalUI);
            CreateOnboardingRow(slide0.transform, "Row0_4", -276f, 168f, "Sprites/UI_Onboarding/s0_worlds", "onb_s0_r4_title", "onb_s0_r4_desc", new Color(0.98f, 0.45f, 0.24f), modalUI);

            // -------------------------------------------------------------
            // SLIDE 1: 5 KARAKTER & SIRALI 2500 SKOR KİLİT SİSTEMİ
            // -------------------------------------------------------------
            GameObject slide1 = new GameObject("Slide_Characters");
            slide1.transform.SetParent(card.transform, false);
            RectTransform s1Rt = slide1.AddComponent<RectTransform>();
            s1Rt.anchorMin = Vector2.zero; s1Rt.anchorMax = Vector2.one;
            s1Rt.offsetMin = Vector2.zero; s1Rt.offsetMax = Vector2.zero;
            modalUI.slides[1] = slide1;

            modalUI.slideSubTexts[1] = CreateOnboardingSubText(slide1.transform, "Sub1", "onb_s1_sub", modalUI);

            CreateOnboardingCharacterRow(slide1.transform, "Char_classic", 310f, "Sprites/UI_Onboarding/s1_char_classic", "char_classic_name", new Color(1f, 0.82f, 0.26f), "onb_s1_c0_badge", new Color(0.02f, 0.16f, 0.14f, 0.95f), new Color(0.00f, 0.87f, 0.56f, 0.85f), new Color(0.00f, 0.87f, 0.56f), 280f, "onb_s1_c0_desc", new Color(0.18f, 0.28f, 0.44f, 0.55f), modalUI);
            CreateOnboardingCharacterRow(slide1.transform, "Char_fire", 155f, "Sprites/UI_Onboarding/s1_char_fire", "char_fire_name", new Color(1f, 0.45f, 0.20f), "onb_s1_c1_badge", new Color(0.24f, 0.11f, 0.02f, 0.95f), new Color(0.85f, 0.48f, 0.05f, 0.85f), new Color(1f, 0.78f, 0.25f), 380f, "onb_s1_c1_desc", new Color(0.40f, 0.18f, 0.10f, 0.55f), modalUI);
            CreateOnboardingCharacterRow(slide1.transform, "Char_ice", 0f, "Sprites/UI_Onboarding/s1_char_ice", "char_ice_name", new Color(0.25f, 0.75f, 1f), "onb_s1_c2_badge", new Color(0.02f, 0.16f, 0.24f, 0.95f), new Color(0.10f, 0.55f, 0.85f, 0.85f), new Color(0.40f, 0.82f, 1f), 380f, "onb_s1_c2_desc", new Color(0.10f, 0.30f, 0.48f, 0.55f), modalUI);
            CreateOnboardingCharacterRow(slide1.transform, "Char_toxic", -155f, "Sprites/UI_Onboarding/s1_char_toxic", "char_poison_name", new Color(0.35f, 0.90f, 0.45f), "onb_s1_c3_badge", new Color(0.04f, 0.20f, 0.08f, 0.95f), new Color(0.12f, 0.65f, 0.25f, 0.85f), new Color(0.40f, 0.95f, 0.55f), 380f, "onb_s1_c3_desc", new Color(0.10f, 0.35f, 0.18f, 0.55f), modalUI);
            CreateOnboardingCharacterRow(slide1.transform, "Char_shadow", -310f, "Sprites/UI_Onboarding/s1_char_shadow", "char_night_name", new Color(0.75f, 0.45f, 1f), "onb_s1_c4_badge", new Color(0.18f, 0.06f, 0.28f, 0.95f), new Color(0.55f, 0.20f, 0.85f, 0.85f), new Color(0.80f, 0.55f, 1f), 380f, "onb_s1_c4_desc", new Color(0.30f, 0.12f, 0.45f, 0.55f), modalUI);

            // -------------------------------------------------------------
            // SLIDE 2: GÜÇLER, YÜKSELTMELER VE BİLETLER
            // -------------------------------------------------------------
            GameObject slide2 = new GameObject("Slide_Powers");
            slide2.transform.SetParent(card.transform, false);
            RectTransform s2Rt = slide2.AddComponent<RectTransform>();
            s2Rt.anchorMin = Vector2.zero; s2Rt.anchorMax = Vector2.one;
            s2Rt.offsetMin = Vector2.zero; s2Rt.offsetMax = Vector2.zero;
            modalUI.slides[2] = slide2;

            modalUI.slideSubTexts[2] = CreateOnboardingSubText(slide2.transform, "Sub2", "onb_s2_sub", modalUI);

            CreateOnboardingRow(slide2.transform, "Power_Shield", 285f, 168f, "Sprites/UI_Onboarding/s2_shield", "onb_s2_r1_title", "onb_s2_r1_desc", new Color(0.22f, 0.74f, 0.97f), modalUI);
            CreateOnboardingRow(slide2.transform, "Power_Slow", 98f, 168f, "Sprites/UI_Onboarding/s2_slow", "onb_s2_r2_title", "onb_s2_r2_desc", new Color(0.22f, 0.74f, 0.97f), modalUI);
            CreateOnboardingRow(slide2.transform, "Power_Multi", -89f, 168f, "Sprites/UI_Onboarding/s2_multiplier", "onb_s2_r3_title", "onb_s2_r3_desc", new Color(0.98f, 0.75f, 0.14f), modalUI);
            CreateOnboardingRow(slide2.transform, "Power_SecondChance", -276f, 168f, "Sprites/UI_Onboarding/s2_ticket", "onb_s2_r4_title", "onb_s2_r4_desc", new Color(0.80f, 0.45f, 0.95f), modalUI);

            // -------------------------------------------------------------
            // SLIDE 3: GÜNLÜK SANDIK, ALTIN EKONOMİSİ & GARDIROP
            // -------------------------------------------------------------
            GameObject slide3 = new GameObject("Slide_Economy");
            slide3.transform.SetParent(card.transform, false);
            RectTransform s3Rt = slide3.AddComponent<RectTransform>();
            s3Rt.anchorMin = Vector2.zero; s3Rt.anchorMax = Vector2.one;
            s3Rt.offsetMin = Vector2.zero; s3Rt.offsetMax = Vector2.zero;
            modalUI.slides[3] = slide3;

            modalUI.slideSubTexts[3] = CreateOnboardingSubText(slide3.transform, "Sub3", "onb_s3_sub", modalUI);

            CreateOnboardingShowcaseRow(slide3.transform, "Showcase_Chest", 215f, 210f, "Sprites/UI_Onboarding/s3_chest", "onb_s3_r1_title", "onb_s3_r1_desc", new Color(0.98f, 0.75f, 0.14f), modalUI);
            CreateOnboardingShowcaseRow(slide3.transform, "Showcase_Gold", -10f, 210f, "Sprites/UI_Onboarding/s3_coin", "onb_s3_r2_title", "onb_s3_r2_desc", new Color(0.22f, 0.74f, 0.97f), modalUI);
            CreateOnboardingShowcaseRow(slide3.transform, "Showcase_Wardrobe", -235f, 210f, "Sprites/UI_Onboarding/s3_hat", "onb_s3_r3_title", "onb_s3_r3_desc", new Color(0.80f, 0.45f, 0.95f), modalUI);

            // -------------------------------------------------------------
            // ALT NAVİGASYON (Dots + Önceki + Sonraki Butonları)
            // -------------------------------------------------------------
            GameObject dotsObj = new GameObject("DotsContainer");
            dotsObj.transform.SetParent(card.transform, false);
            RectTransform dotsRt = dotsObj.AddComponent<RectTransform>();
            dotsRt.anchoredPosition = new Vector2(0f, -475f);
            dotsRt.sizeDelta = new Vector2(240f, 24f);

            modalUI.pageDots = new Image[4];
            float[] dotXs = new float[] { -48f, -16f, 16f, 48f };
            for (int d = 0; d < 4; d++)
            {
                GameObject dObj = new GameObject($"Dot_{d}");
                dObj.transform.SetParent(dotsObj.transform, false);
                RectTransform dRt = dObj.AddComponent<RectTransform>();
                dRt.anchoredPosition = new Vector2(dotXs[d], 0f);
                dRt.sizeDelta = new Vector2(14f, 12f);

                Image dImg = dObj.AddComponent<Image>();
                dImg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(Color.white, Color.clear, 0f, 6, 32, 32);
                dImg.type = Image.Type.Sliced;
                dImg.raycastTarget = false;
                modalUI.pageDots[d] = dImg;
            }

            // Önceki Butonu (Sol Alt - Standart 360x96)
            GameObject prevBtnObj = CreatePillButton(card.transform, "PrevButton", LocalizationManager.Get("btn_prev"), new Vector2(-215f, -575f), new Vector2(360f, 96f), new Color(0.10f, 0.14f, 0.24f, 0.95f), Color.white, 30, new Color(0.20f, 0.32f, 0.50f, 0.7f), 1.5f, 32);
            modalUI.prevButton = prevBtnObj.GetComponent<Button>();
            modalUI.prevButtonText = prevBtnObj.GetComponentInChildren<Text>();
            modalUI.prevButton.onClick.AddListener(modalUI.PreviousSlide);

            // Sonraki Butonu (Sağ Alt / Mint Yeşili - Standart 360x96)
            Color mintColor = new Color(0.00f, 0.875f, 0.56f);
            Color darkNavy = new Color(0.03f, 0.05f, 0.10f);
            GameObject nextBtnObj = CreatePillButton(card.transform, "NextButton", LocalizationManager.Get("onb_btn_next", "SONRAKİ ➔"), new Vector2(215f, -575f), new Vector2(360f, 96f), mintColor, darkNavy, 32, null, 0f, 32);
            modalUI.nextButton = nextBtnObj.GetComponent<Button>();
            modalUI.nextButtonText = nextBtnObj.GetComponentInChildren<Text>();
            modalUI.nextButton.onClick.AddListener(modalUI.NextSlide);

            CanvasGroup cg = modal.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = true;

            bool shouldShow = PlayerPrefs.GetInt(UI.OnboardingModalUI.KEY_ONBOARDING_SEEN, 0) == 0;
            modal.SetActive(shouldShow);
            if (shouldShow)
            {
                modalUI.OpenModal();
            }
        }

        private static Text CreateOnboardingSubText(Transform parent, string name, string key, UI.OnboardingModalUI modalUI)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0f, 498f);
            rt.sizeDelta = new Vector2(860f, 65f);

            Text txt = obj.AddComponent<Text>();
            txt.font = GetSafeDefaultFont();
            txt.fontSize = 28;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.68f, 0.80f, 0.96f);
            txt.raycastTarget = false;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Truncate;
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize = 18;
            txt.resizeTextMaxSize = 28;

            modalUI.RegisterLocalizedText(txt, key);
            return txt;
        }

        private static void CreateOnboardingRow(Transform parent, string name, float yPos, float height, string spritePath, string titleKey, string descKey, Color titleColor, UI.OnboardingModalUI modalUI)
        {
            GameObject row = new GameObject(name);
            row.transform.SetParent(parent, false);
            RectTransform rt = row.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0f, yPos);
            rt.sizeDelta = new Vector2(880f, height);

            Image bg = row.AddComponent<Image>();
            bg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(new Color(0.045f, 0.075f, 0.14f, 0.98f), new Color(0.14f, 0.25f, 0.42f, 0.60f), 1.6f, 24);
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = false;

            // İkon (140x130)
            Sprite sp = !string.IsNullOrEmpty(spritePath) ? Resources.Load<Sprite>(spritePath) : null;
            if (sp != null)
            {
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(row.transform, false);
                RectTransform iconRt = iconObj.AddComponent<RectTransform>();
                iconRt.anchoredPosition = new Vector2(-325f, 0f);
                iconRt.sizeDelta = new Vector2(140f, 130f);
                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = sp;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
            }

            // Başlık
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(row.transform, false);
            RectTransform titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.5f);
            titleRt.anchorMax = new Vector2(0f, 0.5f);
            titleRt.pivot = new Vector2(0f, 0.5f);
            titleRt.anchoredPosition = new Vector2(215f, 30f);
            titleRt.sizeDelta = new Vector2(635f, 44f);
            Text titleTxt = titleObj.AddComponent<Text>();
            titleTxt.font = GetSafeDefaultFont();
            titleTxt.fontSize = 32;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleLeft;
            titleTxt.color = titleColor;
            titleTxt.raycastTarget = false;
            titleTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            titleTxt.verticalOverflow = VerticalWrapMode.Truncate;
            titleTxt.resizeTextForBestFit = true;
            titleTxt.resizeTextMinSize = 20;
            titleTxt.resizeTextMaxSize = 32;
            modalUI.RegisterLocalizedText(titleTxt, titleKey);

            // Açıklama
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(row.transform, false);
            RectTransform descRt = descObj.AddComponent<RectTransform>();
            descRt.anchorMin = new Vector2(0f, 0.5f);
            descRt.anchorMax = new Vector2(0f, 0.5f);
            descRt.pivot = new Vector2(0f, 0.5f);
            descRt.anchoredPosition = new Vector2(215f, -28f);
            descRt.sizeDelta = new Vector2(635f, 75f);
            Text descTxt = descObj.AddComponent<Text>();
            descTxt.font = GetSafeDefaultFont();
            descTxt.fontSize = 24;
            descTxt.alignment = TextAnchor.MiddleLeft;
            descTxt.color = new Color(0.82f, 0.90f, 0.98f);
            descTxt.raycastTarget = false;
            descTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            descTxt.verticalOverflow = VerticalWrapMode.Truncate;
            descTxt.resizeTextForBestFit = true;
            descTxt.resizeTextMinSize = 16;
            descTxt.resizeTextMaxSize = 24;
            modalUI.RegisterLocalizedText(descTxt, descKey);
        }

        private static void CreateOnboardingCharacterRow(Transform parent, string name, float yPos, string spritePath, string charNameKey, Color charColor, string badgeKey, Color badgeFillColor, Color badgeBorderColor, Color badgeTextColor, float badgeWidth, string descKey, Color rowBorderColor, UI.OnboardingModalUI modalUI)
        {
            GameObject row = new GameObject(name);
            row.transform.SetParent(parent, false);
            RectTransform rt = row.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0f, yPos);
            rt.sizeDelta = new Vector2(880f, 138f);

            Image bg = row.AddComponent<Image>();
            bg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(new Color(0.045f, 0.075f, 0.14f, 0.98f), rowBorderColor, 1.8f, 22);
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = false;

            // Karakter Avatar İkonu (110x110)
            Sprite sp = !string.IsNullOrEmpty(spritePath) ? Resources.Load<Sprite>(spritePath) : null;
            if (sp != null)
            {
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(row.transform, false);
                RectTransform iconRt = iconObj.AddComponent<RectTransform>();
                iconRt.anchoredPosition = new Vector2(-360f, 0f);
                iconRt.sizeDelta = new Vector2(110f, 110f);
                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = sp;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
            }

            // Karakter Adı
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(row.transform, false);
            RectTransform titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.5f);
            titleRt.anchorMax = new Vector2(0f, 0.5f);
            titleRt.pivot = new Vector2(0f, 0.5f);
            titleRt.anchoredPosition = new Vector2(150f, 22f);
            titleRt.sizeDelta = new Vector2(240f, 38f);
            Text titleTxt = titleObj.AddComponent<Text>();
            titleTxt.font = GetSafeDefaultFont();
            titleTxt.fontSize = 32;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleLeft;
            titleTxt.color = charColor;
            titleTxt.raycastTarget = false;
            titleTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            titleTxt.verticalOverflow = VerticalWrapMode.Truncate;
            titleTxt.resizeTextForBestFit = true;
            titleTxt.resizeTextMinSize = 20;
            titleTxt.resizeTextMaxSize = 32;
            modalUI.RegisterLocalizedText(titleTxt, charNameKey);

            // Kilit Rozeti (Pill Badge - Sağa Yaslı)
            float bWidth = Mathf.Max(badgeWidth, 380f);
            GameObject badgeObj = new GameObject("Badge");
            badgeObj.transform.SetParent(row.transform, false);
            RectTransform badgeRt = badgeObj.AddComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(1f, 0.5f);
            badgeRt.anchorMax = new Vector2(1f, 0.5f);
            badgeRt.pivot = new Vector2(1f, 0.5f);
            badgeRt.anchoredPosition = new Vector2(-25f, 22f);
            badgeRt.sizeDelta = new Vector2(bWidth, 42f);
            Image badgeImg = badgeObj.AddComponent<Image>();
            badgeImg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(badgeFillColor, badgeBorderColor, 1.5f, 20);
            badgeImg.type = Image.Type.Sliced;
            badgeImg.raycastTarget = false;

            GameObject badgeTxtObj = new GameObject("Text");
            badgeTxtObj.transform.SetParent(badgeObj.transform, false);
            RectTransform bTxtRt = badgeTxtObj.AddComponent<RectTransform>();
            bTxtRt.anchorMin = Vector2.zero; bTxtRt.anchorMax = Vector2.one;
            bTxtRt.offsetMin = new Vector2(8f, 0f); bTxtRt.offsetMax = new Vector2(-8f, 0f);
            Text bTxt = badgeTxtObj.AddComponent<Text>();
            bTxt.font = GetSafeDefaultFont();
            bTxt.fontSize = 20;
            bTxt.fontStyle = FontStyle.Bold;
            bTxt.alignment = TextAnchor.MiddleCenter;
            bTxt.color = badgeTextColor;
            bTxt.raycastTarget = false;
            bTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            bTxt.verticalOverflow = VerticalWrapMode.Truncate;
            bTxt.resizeTextForBestFit = true;
            bTxt.resizeTextMinSize = 13;
            bTxt.resizeTextMaxSize = 20;
            modalUI.RegisterLocalizedText(bTxt, badgeKey);

            // Dünya ve Pasif Açıklaması (Alt satır)
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(row.transform, false);
            RectTransform descRt = descObj.AddComponent<RectTransform>();
            descRt.anchorMin = new Vector2(0f, 0.5f);
            descRt.anchorMax = new Vector2(0f, 0.5f);
            descRt.pivot = new Vector2(0f, 0.5f);
            descRt.anchoredPosition = new Vector2(150f, -24f);
            descRt.sizeDelta = new Vector2(705f, 52f);
            Text descTxt = descObj.AddComponent<Text>();
            descTxt.font = GetSafeDefaultFont();
            descTxt.fontSize = 23;
            descTxt.alignment = TextAnchor.MiddleLeft;
            descTxt.color = new Color(0.82f, 0.90f, 0.98f);
            descTxt.raycastTarget = false;
            descTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            descTxt.verticalOverflow = VerticalWrapMode.Truncate;
            descTxt.resizeTextForBestFit = true;
            descTxt.resizeTextMinSize = 14;
            descTxt.resizeTextMaxSize = 23;
            modalUI.RegisterLocalizedText(descTxt, descKey);
        }

        private static void CreateOnboardingShowcaseRow(Transform parent, string name, float yPos, float height, string spritePath, string titleKey, string descKey, Color titleColor, UI.OnboardingModalUI modalUI)
        {
            GameObject row = new GameObject(name);
            row.transform.SetParent(parent, false);
            RectTransform rt = row.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0f, yPos);
            rt.sizeDelta = new Vector2(880f, height);

            Image bg = row.AddComponent<Image>();
            bg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(new Color(0.045f, 0.075f, 0.14f, 0.98f), new Color(0.14f, 0.25f, 0.42f, 0.60f), 1.6f, 24);
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = false;

            // İkon (160x150)
            Sprite sp = !string.IsNullOrEmpty(spritePath) ? Resources.Load<Sprite>(spritePath) : null;
            if (sp != null)
            {
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(row.transform, false);
                RectTransform iconRt = iconObj.AddComponent<RectTransform>();
                iconRt.anchoredPosition = new Vector2(-310f, 0f);
                iconRt.sizeDelta = new Vector2(160f, 150f);
                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = sp;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
            }

            // Başlık
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(row.transform, false);
            RectTransform titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.5f);
            titleRt.anchorMax = new Vector2(0f, 0.5f);
            titleRt.pivot = new Vector2(0f, 0.5f);
            titleRt.anchoredPosition = new Vector2(235f, 38f);
            titleRt.sizeDelta = new Vector2(625f, 46f);
            Text titleTxt = titleObj.AddComponent<Text>();
            titleTxt.font = GetSafeDefaultFont();
            titleTxt.fontSize = 34;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleLeft;
            titleTxt.color = titleColor;
            titleTxt.raycastTarget = false;
            titleTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            titleTxt.verticalOverflow = VerticalWrapMode.Truncate;
            titleTxt.resizeTextForBestFit = true;
            titleTxt.resizeTextMinSize = 22;
            titleTxt.resizeTextMaxSize = 34;
            modalUI.RegisterLocalizedText(titleTxt, titleKey);

            // Açıklama
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(row.transform, false);
            RectTransform descRt = descObj.AddComponent<RectTransform>();
            descRt.anchorMin = new Vector2(0f, 0.5f);
            descRt.anchorMax = new Vector2(0f, 0.5f);
            descRt.pivot = new Vector2(0f, 0.5f);
            descRt.anchoredPosition = new Vector2(235f, -28f);
            descRt.sizeDelta = new Vector2(625f, 95f);
            Text descTxt = descObj.AddComponent<Text>();
            descTxt.font = GetSafeDefaultFont();
            descTxt.fontSize = 25;
            descTxt.alignment = TextAnchor.MiddleLeft;
            descTxt.color = new Color(0.82f, 0.90f, 0.98f);
            descTxt.raycastTarget = false;
            descTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            descTxt.verticalOverflow = VerticalWrapMode.Truncate;
            descTxt.resizeTextForBestFit = true;
            descTxt.resizeTextMinSize = 15;
            descTxt.resizeTextMaxSize = 25;
            modalUI.RegisterLocalizedText(descTxt, descKey);
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, Vector2 position, Color color, bool raycastTarget = false, FontStyle fontStyle = FontStyle.Normal)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(850f, 140f);

            Text txt = obj.AddComponent<Text>();
            txt.font = GetSafeDefaultFont();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = color;
            txt.fontStyle = fontStyle;
            txt.raycastTarget = raycastTarget;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;

            return txt;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
        {
            Color mintColor = new Color(0.024f, 0.839f, 0.627f);
            Color darkNavy = new Color(0.05f, 0.07f, 0.13f);
            return CreatePillButton(parent, name, label, position, size, mintColor, darkNavy, 32);
        }

        private void CreateSplashScreenOverlay(Transform parent)
        {
            GameObject splashObj = new GameObject("SplashScreenOverlay");
            splashObj.transform.SetParent(parent, false);
            splashObj.transform.SetAsLastSibling();

            RectTransform rt = splashObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Arka plan derin gece rengi (#070b16)
            Image bg = splashObj.AddComponent<Image>();
            bg.color = new Color(0.027f, 0.043f, 0.086f, 1f);
            bg.raycastTarget = true;

            // Açılış görseli (Launch Screen Sprite)
            GameObject imgObj = new GameObject("SplashArt");
            imgObj.transform.SetParent(splashObj.transform, false);
            RectTransform imgRt = imgObj.AddComponent<RectTransform>();
            imgRt.anchorMin = Vector2.zero;
            imgRt.anchorMax = Vector2.one;
            imgRt.offsetMin = Vector2.zero;
            imgRt.offsetMax = Vector2.zero;

            Image splashImg = imgObj.AddComponent<Image>();
            splashImg.raycastTarget = false;

            Sprite splashSprite = Resources.Load<Sprite>("Sprites/launch_screen");
            if (splashSprite == null)
            {
                Texture2D tex = Resources.Load<Texture2D>("Sprites/launch_screen");
                if (tex != null)
                {
                    splashSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }

            if (splashSprite != null)
            {
                splashImg.sprite = splashSprite;
                splashImg.preserveAspect = true;
            }

            // İlerleme Çubuğu Çerçevesi (Alt kısımda parlayan modern loader)
            GameObject barTrack = new GameObject("ProgressBarTrack");
            barTrack.transform.SetParent(splashObj.transform, false);
            RectTransform btRt = barTrack.AddComponent<RectTransform>();
            btRt.anchorMin = new Vector2(0.5f, 0f);
            btRt.anchorMax = new Vector2(0.5f, 0f);
            btRt.pivot = new Vector2(0.5f, 0.5f);
            btRt.anchoredPosition = new Vector2(0f, 160f);
            btRt.sizeDelta = new Vector2(620f, 26f);

            Image trackImg = barTrack.AddComponent<Image>();
            trackImg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(
                new Color(0.06f, 0.09f, 0.16f, 0.95f),
                new Color(0.18f, 0.35f, 0.65f, 0.8f),
                2f, 13, 128, 32);
            trackImg.type = Image.Type.Sliced;
            trackImg.raycastTarget = false;

            // İlerleme Çubuğu Dolgusu (Canlı Cyan / Neon Altın Degrade)
            GameObject barFill = new GameObject("ProgressBarFill");
            barFill.transform.SetParent(barTrack.transform, false);
            RectTransform bfRt = barFill.AddComponent<RectTransform>();
            bfRt.anchorMin = Vector2.zero;
            bfRt.anchorMax = Vector2.one;
            bfRt.offsetMin = new Vector2(3f, 3f);
            bfRt.offsetMax = new Vector2(-3f, -3f);

            Image fillImg = barFill.AddComponent<Image>();
            fillImg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(
                new Color(0.00f, 0.898f, 1.00f, 1f),
                Color.clear, 0f, 10, 64, 32);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = 0;
            fillImg.fillAmount = 0f;
            fillImg.raycastTarget = false;

            // Yükleniyor / Yüzde Yazısı
            Text pctTxt = CreateText(splashObj.transform, "LoadingText", "0%", 24, new Vector2(0f, 205f), new Color(0.85f, 0.92f, 1.0f, 0.9f), false, FontStyle.Bold);
            pctTxt.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            pctTxt.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            pctTxt.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            // Dokunarak geçiş (Skip) butonu ve kararma denetleyicisi
            Button btn = splashObj.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            CanvasGroup cg = splashObj.AddComponent<CanvasGroup>();
            cg.alpha = 1f;

            UI.SplashScreenController controller = splashObj.AddComponent<UI.SplashScreenController>();
            controller.canvasGroup = cg;
            controller.progressBarFill = fillImg;
            controller.loadingText = pctTxt;
            btn.onClick.AddListener(controller.Skip);
        }
    }
}
