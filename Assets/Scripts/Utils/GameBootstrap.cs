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
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
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

                int tileCount = 4;
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
                col.size = new Vector2(50f, 4f);
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

                player.transform.position = new Vector3(PlayerController.FIXED_PLAYER_X, PlayerController.GROUND_FLOOR_Y, 0f);

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

        private static GameObject CreatePillButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Color bgColor, Color textColor, int fontSize = 32, Color? borderColor = null, float borderWidth = 0f)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            Color border = borderColor ?? Color.clear;
            img.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(bgColor, border, borderWidth, 22, 128, 64);
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

        private static GameObject CreateSquareIconButton(Transform parent, string name, string iconText, string labelText, Vector2 position, Vector2 size, Color bgColor, Color borderColor, float borderWidth = 2f)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(bgColor, borderColor, borderWidth, 24, 128, 128);
            img.type = Image.Type.Sliced;
            img.raycastTarget = true;

            Button btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            btn.colors = colors;

            // Üst İkon / Emoji
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(obj.transform, false);
            RectTransform iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchoredPosition = new Vector2(0f, 32f);
            iconRt.sizeDelta = new Vector2(size.x - 20f, 64f);
            Text iconTxt = iconObj.AddComponent<Text>();
            iconTxt.font = GetSafeDefaultFont();
            iconTxt.text = iconText;
            iconTxt.fontSize = 48;
            iconTxt.alignment = TextAnchor.MiddleCenter;
            iconTxt.color = Color.white;
            iconTxt.raycastTarget = false;

            // Alt Metin
            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(obj.transform, false);
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchoredPosition = new Vector2(0f, -40f);
            textRt.sizeDelta = new Vector2(size.x - 16f, 62f);
            Text labelComp = textObj.AddComponent<Text>();
            labelComp.font = GetSafeDefaultFont();
            labelComp.text = labelText;
            labelComp.fontSize = 20;
            labelComp.fontStyle = FontStyle.Bold;
            labelComp.alignment = TextAnchor.MiddleCenter;
            labelComp.color = Color.white;
            labelComp.lineSpacing = 1.05f;
            labelComp.resizeTextForBestFit = true;
            labelComp.resizeTextMinSize = 13;
            labelComp.resizeTextMaxSize = 20;
            labelComp.horizontalOverflow = HorizontalWrapMode.Wrap;
            labelComp.verticalOverflow = VerticalWrapMode.Truncate;
            labelComp.raycastTarget = false;

            Shadow shadow = textObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);

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

        private void CreateMainMenuPanel(Transform parent)
        {
            GameObject panel = new GameObject("MainMenuPanel");
            panel.transform.SetParent(parent, false);
            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.45f);

            // Centered Glassmorphic Card (920x1200, Y: +75 so player is clearly visible below)
            GameObject card = CreateCardContainer(panel.transform, "MenuCard", new Vector2(920f, 1200f));
            card.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 75f);

            // Başlık "HOPPZY" (Büyük ve Belirgin, Şık Sıcak Altın)
            CreateText(card.transform, "TitleText", "HOPPZY", 72, new Vector2(0f, 490f), new Color(1.0f, 0.84f, 0.35f), false, FontStyle.Bold);

            // Alt Başlık "tek dokunuş, sonsuz kaçış"
            CreateText(card.transform, "SubtitleText", "tek dokunuş, sonsuz kaçış", 28, new Vector2(0f, 420f), new Color(0.62f, 0.70f, 0.82f), false);

            // "KARAKTER SEÇ" etiketi
            CreateText(card.transform, "SelectPrompt", "KARAKTER SEÇ", 26, new Vector2(0f, 310f), new Color(0.72f, 0.80f, 0.92f), false, FontStyle.Bold);

            // Karakter Galerisi UI (Swatches - Orijinal zengin avatarlar ve kilit sistemi)
            GameObject galleryObj = new GameObject("CharacterGallery");
            galleryObj.transform.SetParent(card.transform, false);
            RectTransform galleryRt = galleryObj.AddComponent<RectTransform>();
            galleryRt.anchoredPosition = new Vector2(0f, 165f);
            galleryRt.sizeDelta = new Vector2(880f, 210f);

            galleryObj.AddComponent<CharacterSelectorUI>();

            // 1. "OYNA" Butonu (Hero Action - Şık Safir Mavi #2563eb - 620x96, Belirgin Vurgulu Çerçeve)
            Color primaryBlue = new Color(0.14f, 0.40f, 0.92f);
            Color blueBorder = new Color(0.40f, 0.65f, 1.0f, 0.55f);
            GameObject btnObj = CreatePillButton(card.transform, "PlayButton", "▶  OYNA", new Vector2(0f, -30f), new Vector2(620f, 96f), primaryBlue, Color.white, 36, blueBorder, 2.5f);
            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => {
                AudioManager.Instance?.PlayJumpSound();
                if (GameManager.Instance != null) GameManager.Instance.StartGame();
            });

            // 2. 4 Tane Kare Yan Yana Buton:
            // [MAĞAZA & GARDIROP] [GÜNLÜK ÖDÜL] [NASIL OYNANIR] [REKLAM İZLE]
            float sqY = -195f;
            Vector2 sqSize = new Vector2(195f, 195f);

            // Buton 1: 🛒 Mağaza & Gardırop (Asil Mor #6D28D9)
            Color purpleBg = new Color(0.42f, 0.22f, 0.78f);
            Color purpleBorder = new Color(0.68f, 0.45f, 1.0f, 0.50f);
            GameObject wardrobeBtnObj = CreateSquareIconButton(card.transform, "WardrobeSquareButton", "🛒", "MAĞAZA &\nGARDIROP", new Vector2(-316.5f, sqY), sqSize, purpleBg, purpleBorder, 2.5f);
            Button wardrobeBtn = wardrobeBtnObj.GetComponent<Button>();
            wardrobeBtn.onClick.AddListener(() => {
                AudioManager.Instance?.PlayJumpSound();
                if (UI.WardrobeModalUI.Instance != null)
                {
                    UI.WardrobeModalUI.Instance.gameObject.SetActive(true);
                }
            });

            // Buton 2: 🎁 Günlük Ödül Sandığı (Hazine Altını #D97706)
            Color goldBg = new Color(0.85f, 0.50f, 0.08f);
            Color goldBorder = new Color(1.0f, 0.78f, 0.30f, 0.50f);
            GameObject rewardBtnObj = CreateSquareIconButton(card.transform, "DailyRewardSquareButton", "🎁", "GÜNLÜK\nÖDÜL", new Vector2(-105.5f, sqY), sqSize, goldBg, goldBorder, 2.5f);
            Button rewardBtn = rewardBtnObj.GetComponent<Button>();
            rewardBtn.onClick.AddListener(() => {
                AudioManager.Instance?.PlayJumpSound();
                if (DailyRewardModalUI.Instance != null)
                {
                    DailyRewardModalUI.Instance.gameObject.SetActive(true);
                }
            });

            // Buton 3: 📖 Nasıl Oynanır (Okyanus Mavisi #0284C7)
            Color tealBg = new Color(0.08f, 0.50f, 0.75f);
            Color tealBorder = new Color(0.35f, 0.78f, 1.0f, 0.50f);
            GameObject rulesBtnObj = CreateSquareIconButton(card.transform, "RulesSquareButton", "📖", "NASIL\nOYNANIR?", new Vector2(105.5f, sqY), sqSize, tealBg, tealBorder, 2.5f);
            Button rulesBtn = rulesBtnObj.GetComponent<Button>();
            rulesBtn.onClick.AddListener(() => {
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

            // Buton 4: 🎬 Reklam İzle (Zümrüt Yeşili #059669 - +35 Altın Ödülü)
            Color greenBg = new Color(0.05f, 0.60f, 0.40f);
            Color greenBorder = new Color(0.25f, 0.88f, 0.55f, 0.50f);
            GameObject adBtnObj = CreateSquareIconButton(card.transform, "RewardedAdSquareButton", "🎬", "REKLAM\nİZLE", new Vector2(316.5f, sqY), sqSize, greenBg, greenBorder, 2.5f);
            Button adBtn = adBtnObj.GetComponent<Button>();
            adBtn.onClick.AddListener(() => {
                AudioManager.Instance?.PlayJumpSound();
                if (MonetizationManager.Instance != null)
                {
                    MonetizationManager.Instance.ShowRewardedAd(
                        () => {
                            SaveSystem.AddGold(35);
                            AudioManager.Instance?.PlayNewRecordSound();
                            AudioManager.Instance?.TriggerHapticFeedback();
                            if (Instance != null)
                            {
                                Instance.ShowRewardToast(card.transform, "🎉 +35 ALTIN KAZANDIN!");
                            }
                            if (UI.WardrobeModalUI.Instance != null && UI.WardrobeModalUI.Instance.gameObject.activeInHierarchy)
                            {
                                UI.WardrobeModalUI.Instance.RefreshUI();
                            }
                        },
                        () => {
                            if (Instance != null)
                            {
                                Instance.ShowRewardToast(card.transform, "Reklam henüz hazır değil!");
                            }
                        }
                    );
                }
                else
                {
                    SaveSystem.AddGold(35);
                    AudioManager.Instance?.PlayNewRecordSound();
                    if (Instance != null)
                    {
                        Instance.ShowRewardToast(card.transform, "🎉 +35 ALTIN KAZANDIN!");
                    }
                }
            });

            // Alt İpucu
            CreateText(card.transform, "ControlHint", "Zıplamak için dokun / Boşluk", 24, new Vector2(0f, -340f), new Color(0.55f, 0.65f, 0.8f), false);

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

            Color cardBg = new Color(0.094f, 0.149f, 0.231f, 0.98f);
            Color cardBorder = new Color(0.18f, 0.28f, 0.40f, 0.55f);
            Image cardImg = card.AddComponent<Image>();
            cardImg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(cardBg, cardBorder, 2f, 44, 128, 128);
            cardImg.type = Image.Type.Sliced;
            cardImg.raycastTarget = true;

            // 1. BAŞLIK VE AKSANLAR
            // Orta Başlık: "OYUN BİTTİ" (64pt Bold)
            CreateText(card.transform, "GameOverTitle", "OYUN BİTTİ", 64, new Vector2(0f, 420f), Color.white, false, FontStyle.Bold);

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
            CreateText(card.transform, "GameOverSub", "Bir engele çarptın", 32, new Vector2(0f, 355f), new Color(0.49f, 0.62f, 0.74f), false);

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
            CreateText(scoreBox.transform, "ScoreLabel", "BU KOŞUDAKİ SKORUN", 28, new Vector2(0f, 80f), new Color(0.45f, 0.57f, 0.70f), false, FontStyle.Bold);

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
            bg.color = new Color(0f, 0f, 0f, 0.75f);

            UI.DailyRewardModalUI modalUI = modal.AddComponent<UI.DailyRewardModalUI>();

            // Modal Card (920x1360) - Ferah ve büyük ekran kartı
            GameObject card = CreateCardContainer(modal.transform, "RewardCard", new Vector2(920f, 1360f));
            card.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            // Başlık (Büyük ve Belirgin)
            modalUI.titleText = CreateText(card.transform, "Title", "GÜNLÜK ÖDÜL SANDIĞI", 52, new Vector2(0f, 540f), new Color(1.0f, 0.84f, 0.04f), false, FontStyle.Bold);

            // Sandık Görseli (420x420)
            GameObject chestObj = new GameObject("ChestImage");
            chestObj.transform.SetParent(card.transform, false);
            RectTransform chestRt = chestObj.AddComponent<RectTransform>();
            chestRt.anchoredPosition = new Vector2(0f, 210f);
            chestRt.sizeDelta = new Vector2(420f, 420f);
            Image chestImg = chestObj.AddComponent<Image>();
            modalUI.closedChestSprite = ProceduralSpriteGenerator.GetSpriteOrGenerate("Sprites/treasure_chest", () => ProceduralSpriteGenerator.CreateRectangleSprite(Color.yellow, 128, 128));
            modalUI.openChestSprite = ProceduralSpriteGenerator.GetSpriteOrGenerate("Sprites/treasure_chest_open", () => modalUI.closedChestSprite);
            chestImg.sprite = SaveSystem.CanClaimDailyReward() ? modalUI.closedChestSprite : modalUI.openChestSprite;
            chestImg.preserveAspect = true;
            chestImg.raycastTarget = false;
            modalUI.chestImage = chestImg;

            // Açıklama (Büyük ve Okunaklı)
            modalUI.descriptionText = CreateText(card.transform, "Desc", "Her gün oyuna gir, sandığını aç ve bonus altınları topla!", 32, new Vector2(0f, -80f), new Color(0.75f, 0.85f, 0.98f), false);

            // Durum Mesajı
            modalUI.statusText = CreateText(card.transform, "Status", "🎁 Bugünün sandığı açılmaya hazır!", 36, new Vector2(0f, -190f), new Color(0.024f, 0.839f, 0.627f), false, FontStyle.Bold);

            // Sandığı Aç Butonu (Standart 540x96)
            Color mintColor = new Color(0.024f, 0.839f, 0.627f);
            Color darkNavy = new Color(0.05f, 0.07f, 0.13f);
            GameObject claimBtnObj = CreatePillButton(card.transform, "ClaimButton", "SANDIĞI AÇ", new Vector2(0f, -320f), new Vector2(540f, 96f), mintColor, darkNavy, 34);
            modalUI.claimButton = claimBtnObj.GetComponent<Button>();
            modalUI.claimButtonText = claimBtnObj.GetComponentInChildren<Text>();
            modalUI.claimButton.onClick.AddListener(modalUI.OnClaimButtonClicked);

            // Kapat Butonu (Standart 540x96)
            GameObject closeBtnObj = CreatePillButton(card.transform, "CloseButton", "KAPAT", new Vector2(0f, -440f), new Vector2(540f, 96f), new Color(0.15f, 0.20f, 0.35f, 0.9f), Color.white, 32);
            modalUI.closeButton = closeBtnObj.GetComponent<Button>();
            modalUI.closeButton.onClick.AddListener(modalUI.CloseModal);

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

            // Modal Card (940x1520) - Geniş, ferah ve büyük ekran kartı
            GameObject card = CreateCardContainer(modal.transform, "WardrobeCard", new Vector2(940f, 1520f));
            card.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            Image cardImg = card.GetComponent<Image>();
            cardImg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(
                new Color(0.07f, 0.09f, 0.17f, 0.98f),
                new Color(1f, 1f, 1f, 0.15f),
                2f, 28, 128, 128);

            // Başlık (Üstte - Büyük ve Belirgin)
            modalUI.titleText = CreateText(card.transform, "Title", "⚡ GÜÇLER & YÜKSELTMELER", 48, new Vector2(0f, 660f), new Color(0.65f, 0.40f, 0.95f), false, FontStyle.Bold);

            // Toplam Altın Göstergesi (Büyük)
            modalUI.totalGoldText = CreateText(card.transform, "TotalGold", "🪙 Toplam Altın: 150", 38, new Vector2(0f, 595f), new Color(1.0f, 0.84f, 0.04f), false, FontStyle.Bold);

            // Durum / Hata Mesajı
            modalUI.statusMessageText = CreateText(card.transform, "StatusMsg", "", 28, new Vector2(0f, 545f), new Color(0.024f, 0.839f, 0.627f), false, FontStyle.Bold);

            // Sekmeler Çubuğu (Tabs Bar - Genişletilmiş ve Büyük Butonlar)
            Color tabActive = new Color(0.61f, 0.36f, 0.90f);
            Color tabInactive = new Color(0.12f, 0.15f, 0.28f, 0.9f);

            GameObject tabUpgradesObj = CreatePillButton(card.transform, "TabUpgradesBtn", "⚡ YÜKSELTMELER", new Vector2(-225f, 470f), new Vector2(430f, 82f), tabActive, Color.white, 30);
            modalUI.tabUpgradesButton = tabUpgradesObj.GetComponent<Button>();
            modalUI.tabUpgradesBg = tabUpgradesObj.GetComponent<Image>();
            modalUI.tabUpgradesText = tabUpgradesObj.GetComponentInChildren<Text>();
            modalUI.tabUpgradesButton.onClick.AddListener(() => modalUI.SwitchTab(UI.ShopTab.Upgrades));

            GameObject tabOutfitsObj = CreatePillButton(card.transform, "TabOutfitsBtn", "👔 GARDIROP", new Vector2(225f, 470f), new Vector2(430f, 82f), tabInactive, new Color(0.7f, 0.8f, 0.9f), 30);
            modalUI.tabOutfitsButton = tabOutfitsObj.GetComponent<Button>();
            modalUI.tabOutfitsBg = tabOutfitsObj.GetComponent<Image>();
            modalUI.tabOutfitsText = tabOutfitsObj.GetComponentInChildren<Text>();
            modalUI.tabOutfitsButton.onClick.AddListener(() => modalUI.SwitchTab(UI.ShopTab.Outfits));

            // Kaydırılabilir Liste Alanı (ScrollView + Viewport + Content)
            GameObject scrollViewObj = new GameObject("ScrollView");
            scrollViewObj.transform.SetParent(card.transform, false);
            RectTransform scrollRt = scrollViewObj.AddComponent<RectTransform>();
            scrollRt.anchoredPosition = new Vector2(0f, -50f);
            scrollRt.sizeDelta = new Vector2(890f, 900f);

            Image scrollBg = scrollViewObj.AddComponent<Image>();
            scrollBg.color = Color.clear;
            scrollBg.raycastTarget = true;

            scrollViewObj.AddComponent<RectMask2D>(); // Dışarı taşan nesneleri otomatik maskele

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
            contentRt.sizeDelta = new Vector2(890f, 900f);

            scrollRect.content = contentRt;
            scrollRect.viewport = scrollRt;

            modalUI.itemsContainer = contentRt;
            modalUI.itemsScrollRect = scrollRect;

            // Kapat Butonu (Standart 540x96)
            GameObject closeBtnObj = CreatePillButton(card.transform, "CloseButton", "KAPAT", new Vector2(0f, -670f), new Vector2(540f, 96f), new Color(0.15f, 0.20f, 0.35f, 0.95f), Color.white, 32);
            modalUI.closeButton = closeBtnObj.GetComponent<Button>();
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
            bg.color = new Color(0.02f, 0.03f, 0.07f, 0.96f);
            bg.raycastTarget = true;

            UI.OnboardingModalUI modalUI = modal.AddComponent<UI.OnboardingModalUI>();

            // Onboarding Card (940x1520)
            GameObject card = CreateCardContainer(modal.transform, "OnboardingCard", new Vector2(940f, 1520f));
            card.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            modalUI.modalCard = card;

            // 1. Üst Rozet & Başlık
            modalUI.stepBadgeText = CreateText(card.transform, "StepBadge", "REHBER • ADIM 1 / 4", 28, new Vector2(0f, 695f), new Color(0.024f, 0.839f, 0.627f), false, FontStyle.Bold);
            RectTransform sbRt = modalUI.stepBadgeText.GetComponent<RectTransform>();
            sbRt.sizeDelta = new Vector2(400f, 44f);

            modalUI.titleText = CreateText(card.transform, "TitleText", "OYUN KURALLARI & KONTROLLER", 44, new Vector2(0f, 622f), new Color(1.0f, 0.84f, 0.35f), false, FontStyle.Bold);
            RectTransform ttRt = modalUI.titleText.GetComponent<RectTransform>();
            ttRt.sizeDelta = new Vector2(860f, 56f);
            modalUI.titleText.resizeTextForBestFit = true;
            modalUI.titleText.resizeTextMinSize = 28;
            modalUI.titleText.resizeTextMaxSize = 44;

            // Sağ Üst "✕ KAPAT" Butonu (Kartın sağ üst köşesine 32px simetrik kenar boşluğuyla tam hizalı)
            GameObject skipBtnObj = CreatePillButton(card.transform, "SkipButton", "✕ KAPAT", Vector2.zero, new Vector2(160f, 58f), new Color(0.18f, 0.24f, 0.38f, 0.95f), new Color(0.9f, 0.95f, 1f), 24);
            RectTransform skipRt = skipBtnObj.GetComponent<RectTransform>();
            skipRt.anchorMin = new Vector2(1f, 1f);
            skipRt.anchorMax = new Vector2(1f, 1f);
            skipRt.pivot = new Vector2(1f, 1f);
            skipRt.anchoredPosition = new Vector2(-32f, -32f);
            modalUI.closeButton = skipBtnObj.GetComponent<Button>();
            modalUI.closeButton.onClick.AddListener(modalUI.CloseModal);

            modalUI.slides = new GameObject[4];

            // -------------------------------------------------------------
            // SLIDE 0: OYUN KURALLARI & TEMEL MEKANİKLER
            // -------------------------------------------------------------
            GameObject slide0 = new GameObject("Slide_Rules");
            slide0.transform.SetParent(card.transform, false);
            RectTransform s0Rt = slide0.AddComponent<RectTransform>();
            s0Rt.anchorMin = Vector2.zero; s0Rt.anchorMax = Vector2.one;
            s0Rt.offsetMin = Vector2.zero; s0Rt.offsetMax = Vector2.zero;
            modalUI.slides[0] = slide0;

            CreateText(slide0.transform, "Sub0", "Zıpla, engellerden kaç ve rekorunu kır!", 32, new Vector2(0f, 520f), new Color(0.70f, 0.80f, 0.95f), false, FontStyle.Bold);

            CreateOnboardingRow(slide0.transform, "Row0_1", 345f, 165f, "👆", "Tek Dokunuşla Zıpla", "Ekrana dokunarak veya Space tuşuna basarak yaklaşan sivri dikenlerin, kaktüs ve kayaların üzerinden atla.", new Color(0.024f, 0.839f, 0.627f));
            CreateOnboardingRow(slide0.transform, "Row0_2", 165f, 165f, "🚀", "Çift Zıplama (Double Jump)", "Havada bir kez daha dokunarak ikinci kez zıpla! Yüksek ve arka arkaya gelen engelleri aşmak için en önemli yeteneğin.", new Color(0.30f, 0.79f, 0.94f));
            CreateOnboardingRow(slide0.transform, "Row0_3", -15f, 165f, "🔥", "Kombo Serisi & Bonus Altın", "Engelleri çarpmadan art arda geç. Her 5 engelde ekstra kombo puanı ve çarpanlı altın ödülleri kazanırsın!", new Color(1.0f, 0.82f, 0.40f));
            CreateOnboardingRow(slide0.transform, "Row0_4", -195f, 165f, "🌅", "3 Zorluk Aşaması (Gündüz, Akşam, Gece)", "Puanın arttıkça dünya değişir, oyun hızlanır ve reflekslerini sonuna kadar zorlar!", new Color(1.0f, 0.42f, 0.21f));

            // -------------------------------------------------------------
            // SLIDE 1: 5 KARAKTER & SIRALI 2500 SKOR KİLİT SİSTEMİ
            // -------------------------------------------------------------
            GameObject slide1 = new GameObject("Slide_Characters");
            slide1.transform.SetParent(card.transform, false);
            RectTransform s1Rt = slide1.AddComponent<RectTransform>();
            s1Rt.anchorMin = Vector2.zero; s1Rt.anchorMax = Vector2.one;
            s1Rt.offsetMin = Vector2.zero; s1Rt.offsetMax = Vector2.zero;
            modalUI.slides[1] = slide1;

            CreateText(slide1.transform, "Sub1", "İlk başta sadece Klasik açıktır! Sırayla 2500 skor yaparak kilidi aç:", 30, new Vector2(0f, 520f), new Color(1.0f, 0.84f, 0.35f), false, FontStyle.Bold);

            CreateOnboardingCharacterRow(slide1.transform, "Char_classic", 360f, "classic", "Klasik", "🔓 BAŞLANGIÇTA AÇIK", "Çayır Diyarı • Kalıcı +%15 ekstra skor yeteneği!", new Color(1.0f, 0.82f, 0.40f), new Color(0.024f, 0.839f, 0.627f));
            CreateOnboardingCharacterRow(slide1.transform, "Char_fire", 200f, "fire", "Ateş", "🔒 Klasik ile 2500 Skor Yap", "Volkan & Lav Diyarı • Ateş topu ve kor alev dünyası!", new Color(1.0f, 0.42f, 0.21f), new Color(1.0f, 0.65f, 0.20f));
            CreateOnboardingCharacterRow(slide1.transform, "Char_ice", 40f, "ice", "Buz", "🔒 Ateş ile 2500 Skor Yap", "Buzul & Kar Dağları • Dondurucu soğuk ve kutup gecesi!", new Color(0.30f, 0.79f, 0.94f), new Color(0.30f, 0.79f, 0.94f));
            CreateOnboardingCharacterRow(slide1.transform, "Char_toxic", -120f, "toxic", "Zehir", "🔒 Buz ile 2500 Skor Yap", "Toksik Sis Ormanı • Kombo ödüllerini x1.25 katına çıkarır!", new Color(0.49f, 0.95f, 0.61f), new Color(0.49f, 0.95f, 0.61f));
            CreateOnboardingCharacterRow(slide1.transform, "Char_shadow", -280f, "shadow", "Gece", "🔒 Zehir ile 2500 Skor Yap", "Derin Uzay & Nebula • Engellerden %28 otomatik sıyrılma!", new Color(0.61f, 0.36f, 0.90f), new Color(0.61f, 0.36f, 0.90f));

            // -------------------------------------------------------------
            // SLIDE 2: GÜÇLER, YÜKSELTMELER VE BİLETLER
            // -------------------------------------------------------------
            GameObject slide2 = new GameObject("Slide_Powers");
            slide2.transform.SetParent(card.transform, false);
            RectTransform s2Rt = slide2.AddComponent<RectTransform>();
            s2Rt.anchorMin = Vector2.zero; s2Rt.anchorMax = Vector2.one;
            s2Rt.offsetMin = Vector2.zero; s2Rt.offsetMax = Vector2.zero;
            modalUI.slides[2] = slide2;

            CreateText(slide2.transform, "Sub2", "Güçlendirmeleri topla ve mağazadan kalıcı yetenekler edin!", 32, new Vector2(0f, 520f), new Color(0.70f, 0.80f, 0.95f), false, FontStyle.Bold);

            CreateOnboardingPowerRow(slide2.transform, "Power_Shield", 345f, "shield", "🛡️", "Koruyucu Kalkan (Shield)", "Bir engele çarpışmayı tamamen emer, engelleri temizler ve koşunu kurtarır.", new Color(0.30f, 0.79f, 0.94f));
            CreateOnboardingPowerRow(slide2.transform, "Power_Slow", 165f, "slow", "⏱️", "Zamanı Yavaşlatma (Slow-Mo)", "5 saniye boyunca dünya hızını %40 düşürerek karmaşık engellerden kolayca sıyrılmanı sağlar.", new Color(0.024f, 0.839f, 0.627f));
            CreateOnboardingPowerRow(slide2.transform, "Power_Multi", -15f, "multi", "✖️2️⃣", "x2 Çift Skor & Altın Çarpanı", "Aktif olduğu sürece kazanılan tüm koşu puanlarını ve engellerden gelen altınları 2 katına çıkarır!", new Color(1.0f, 0.82f, 0.40f));
            CreateOnboardingPowerRow(slide2.transform, "Power_SecondChance", -195f, "second_chance", "🎫", "İkinci Şans Bileti & Kalıcı Güçler", "Mağaza'dan İkinci Şans Bileti alıp yandığın yerden devam edebilir, zıplama ve kombo süreni geliştirebilirsin.", new Color(0.61f, 0.36f, 0.90f));

            // -------------------------------------------------------------
            // SLIDE 3: GÜNLÜK SANDIK, ALTIN EKONOMİSİ & GARDIROP
            // -------------------------------------------------------------
            GameObject slide3 = new GameObject("Slide_Economy");
            slide3.transform.SetParent(card.transform, false);
            RectTransform s3Rt = slide3.AddComponent<RectTransform>();
            s3Rt.anchorMin = Vector2.zero; s3Rt.anchorMax = Vector2.one;
            s3Rt.offsetMin = Vector2.zero; s3Rt.offsetMax = Vector2.zero;
            modalUI.slides[3] = slide3;

            CreateText(slide3.transform, "Sub3", "Her gün giriş yap, sandığını aç ve altınlarınla tarzını yarat!", 32, new Vector2(0f, 520f), new Color(0.70f, 0.80f, 0.95f), false, FontStyle.Bold);

            CreateOnboardingShowcaseRow(slide3.transform, "Showcase_Chest", 310f, 220f, "Sprites/treasure_chest", "🎁 Günlük Ödül Sandığı", "Her gün oyuna girerek sandığını aç! Günlük seri yaptıkça (Streak) kazandığın altın miktarı 20'den 50'ye kadar katlanır.", new Color(1.0f, 0.82f, 0.40f));
            CreateOnboardingShowcaseRow(slide3.transform, "Showcase_Gold", 65f, 220f, null, "🪙 Altın Toplama & Yeni Rekorlar", "Engelleri aştıkça, kombo yaptıkça ve rekor kırdıkça altın topla. Her koşu sonunda kasan zenginleşir!", new Color(0.024f, 0.839f, 0.627f));
            CreateOnboardingShowcaseRow(slide3.transform, "Showcase_Wardrobe", -180f, 220f, null, "👔 Gardırop & Kostümler", "Topladığın altınlarla kahramanlarına süper kahraman pelerini, havalı güneş gözlükleri ve maskeler satın al!", new Color(0.61f, 0.36f, 0.90f));

            // -------------------------------------------------------------
            // ALT NAVİGASYON (Dots + Önceki + Sonraki Butonları)
            // -------------------------------------------------------------
            GameObject dotsObj = new GameObject("DotsContainer");
            dotsObj.transform.SetParent(card.transform, false);
            RectTransform dotsRt = dotsObj.AddComponent<RectTransform>();
            dotsRt.anchoredPosition = new Vector2(0f, -540f);
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

            // Önceki Butonu (Sol Alt - Standart 380x96)
            GameObject prevBtnObj = CreatePillButton(card.transform, "PrevButton", "◀ ÖNCEKİ", new Vector2(-210f, -630f), new Vector2(380f, 96f), new Color(0.12f, 0.16f, 0.28f, 0.95f), Color.white, 30);
            modalUI.prevButton = prevBtnObj.GetComponent<Button>();
            modalUI.prevButton.onClick.AddListener(modalUI.PreviousSlide);

            // Sonraki Butonu (Sağ Alt / Mint Yeşili - Standart 380x96)
            Color mintColor = new Color(0.024f, 0.839f, 0.627f);
            Color darkNavy = new Color(0.05f, 0.07f, 0.13f);
            GameObject nextBtnObj = CreatePillButton(card.transform, "NextButton", "SONRAKİ ➔", new Vector2(210f, -630f), new Vector2(380f, 96f), mintColor, darkNavy, 32);
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

        private static void CreateOnboardingRow(Transform parent, string name, float yPos, float height, string emoji, string title, string desc, Color titleColor)
        {
            GameObject row = new GameObject(name);
            row.transform.SetParent(parent, false);
            RectTransform rt = row.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0f, yPos);
            rt.sizeDelta = new Vector2(880f, height);

            Image bg = row.AddComponent<Image>();
            bg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(new Color(0.10f, 0.13f, 0.24f, 0.95f), new Color(1f, 1f, 1f, 0.12f), 1.5f, 22);
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = false;

            // Emoji / İkon Kutusu (106x106)
            GameObject iconObj = new GameObject("Emoji");
            iconObj.transform.SetParent(row.transform, false);
            RectTransform iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchoredPosition = new Vector2(-360f, 0f);
            iconRt.sizeDelta = new Vector2(106f, 106f);
            Text emojiTxt = iconObj.AddComponent<Text>();
            emojiTxt.font = GetSafeDefaultFont();
            emojiTxt.text = emoji;
            emojiTxt.fontSize = 62;
            emojiTxt.alignment = TextAnchor.MiddleCenter;
            emojiTxt.color = Color.white;
            emojiTxt.raycastTarget = false;

            // Başlık
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(row.transform, false);
            RectTransform titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchoredPosition = new Vector2(40f, 32f);
            titleRt.sizeDelta = new Vector2(680f, 44f);
            Text titleTxt = titleObj.AddComponent<Text>();
            titleTxt.font = GetSafeDefaultFont();
            titleTxt.text = title;
            titleTxt.fontSize = 34;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleLeft;
            titleTxt.color = titleColor;
            titleTxt.raycastTarget = false;

            // Açıklama
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(row.transform, false);
            RectTransform descRt = descObj.AddComponent<RectTransform>();
            descRt.anchoredPosition = new Vector2(40f, -26f);
            descRt.sizeDelta = new Vector2(680f, 75f);
            Text descTxt = descObj.AddComponent<Text>();
            descTxt.font = GetSafeDefaultFont();
            descTxt.text = desc;
            descTxt.fontSize = 26;
            descTxt.alignment = TextAnchor.MiddleLeft;
            descTxt.color = new Color(0.85f, 0.92f, 1f);
            descTxt.raycastTarget = false;
        }

        private static void CreateOnboardingCharacterRow(Transform parent, string name, float yPos, string charId, string charName, string reqText, string desc, Color charColor, Color reqColor)
        {
            GameObject row = new GameObject(name);
            row.transform.SetParent(parent, false);
            RectTransform rt = row.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0f, yPos);
            rt.sizeDelta = new Vector2(880f, 142f);

            Image bg = row.AddComponent<Image>();
            bg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(new Color(0.10f, 0.13f, 0.24f, 0.95f), new Color(1f, 1f, 1f, 0.12f), 1.5f, 22);
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = false;

            // Karakter Avatar İkonu (108x108)
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(row.transform, false);
            RectTransform iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchoredPosition = new Vector2(-360f, 0f);
            iconRt.sizeDelta = new Vector2(108f, 108f);
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = ProceduralSpriteGenerator.GetSpriteOrGenerate($"Sprites/{charId}_hero", () => ProceduralSpriteGenerator.CreateMiniCharacterIcon(charColor, 128));
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            // Karakter Adı
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(row.transform, false);
            RectTransform titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchoredPosition = new Vector2(40f, 26f);
            titleRt.sizeDelta = new Vector2(280f, 40f);
            Text titleTxt = titleObj.AddComponent<Text>();
            titleTxt.font = GetSafeDefaultFont();
            titleTxt.text = charName;
            titleTxt.fontSize = 34;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleLeft;
            titleTxt.color = charColor;
            titleTxt.raycastTarget = false;

            // Kilit Gereksinim Rozeti (Sağ üst)
            GameObject reqObj = new GameObject("Req");
            reqObj.transform.SetParent(row.transform, false);
            RectTransform reqRt = reqObj.AddComponent<RectTransform>();
            reqRt.anchoredPosition = new Vector2(170f, 26f);
            reqRt.sizeDelta = new Vector2(430f, 40f);
            Text reqTxt = reqObj.AddComponent<Text>();
            reqTxt.font = GetSafeDefaultFont();
            reqTxt.text = reqText;
            reqTxt.fontSize = 26;
            reqTxt.fontStyle = FontStyle.Bold;
            reqTxt.alignment = TextAnchor.MiddleRight;
            reqTxt.color = reqColor;
            reqTxt.raycastTarget = false;

            // Dünya ve Pasif Açıklaması (Alt satır)
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(row.transform, false);
            RectTransform descRt = descObj.AddComponent<RectTransform>();
            descRt.anchoredPosition = new Vector2(40f, -24f);
            descRt.sizeDelta = new Vector2(680f, 50f);
            Text descTxt = descObj.AddComponent<Text>();
            descTxt.font = GetSafeDefaultFont();
            descTxt.text = desc;
            descTxt.fontSize = 25;
            descTxt.alignment = TextAnchor.MiddleLeft;
            descTxt.color = new Color(0.85f, 0.92f, 1f);
            descTxt.raycastTarget = false;
        }

        private static void CreateOnboardingPowerRow(Transform parent, string name, float yPos, string shopId, string fallbackEmoji, string title, string desc, Color titleColor)
        {
            GameObject row = new GameObject(name);
            row.transform.SetParent(parent, false);
            RectTransform rt = row.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0f, yPos);
            rt.sizeDelta = new Vector2(880f, 165f);

            Image bg = row.AddComponent<Image>();
            bg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(new Color(0.10f, 0.13f, 0.24f, 0.95f), new Color(1f, 1f, 1f, 0.12f), 1.5f, 22);
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = false;

            // İkon (112x112)
            Sprite sp = Resources.Load<Sprite>($"Sprites/Shop/shop_{shopId}");
            if (sp == null && shopId == "slow") sp = Resources.Load<Sprite>("Sprites/slow_powerup");
            if (sp == null && shopId == "multi") sp = Resources.Load<Sprite>("Sprites/multi_powerup");

            if (sp != null)
            {
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(row.transform, false);
                RectTransform iconRt = iconObj.AddComponent<RectTransform>();
                iconRt.anchoredPosition = new Vector2(-360f, 0f);
                iconRt.sizeDelta = new Vector2(112f, 112f);
                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = sp;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
            }
            else
            {
                GameObject iconObj = new GameObject("Emoji");
                iconObj.transform.SetParent(row.transform, false);
                RectTransform iconRt = iconObj.AddComponent<RectTransform>();
                iconRt.anchoredPosition = new Vector2(-360f, 0f);
                iconRt.sizeDelta = new Vector2(112f, 112f);
                Text emojiTxt = iconObj.AddComponent<Text>();
                emojiTxt.font = GetSafeDefaultFont();
                emojiTxt.text = fallbackEmoji;
                emojiTxt.fontSize = 62;
                emojiTxt.alignment = TextAnchor.MiddleCenter;
                emojiTxt.color = Color.white;
                emojiTxt.raycastTarget = false;
            }

            // Başlık
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(row.transform, false);
            RectTransform titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchoredPosition = new Vector2(40f, 32f);
            titleRt.sizeDelta = new Vector2(680f, 44f);
            Text titleTxt = titleObj.AddComponent<Text>();
            titleTxt.font = GetSafeDefaultFont();
            titleTxt.text = title;
            titleTxt.fontSize = 34;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleLeft;
            titleTxt.color = titleColor;
            titleTxt.raycastTarget = false;

            // Açıklama
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(row.transform, false);
            RectTransform descRt = descObj.AddComponent<RectTransform>();
            descRt.anchoredPosition = new Vector2(40f, -26f);
            descRt.sizeDelta = new Vector2(680f, 75f);
            Text descTxt = descObj.AddComponent<Text>();
            descTxt.font = GetSafeDefaultFont();
            descTxt.text = desc;
            descTxt.fontSize = 26;
            descTxt.alignment = TextAnchor.MiddleLeft;
            descTxt.color = new Color(0.85f, 0.92f, 1f);
            descTxt.raycastTarget = false;
        }

        private static void CreateOnboardingShowcaseRow(Transform parent, string name, float yPos, float height, string spritePath, string title, string desc, Color titleColor)
        {
            GameObject row = new GameObject(name);
            row.transform.SetParent(parent, false);
            RectTransform rt = row.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0f, yPos);
            rt.sizeDelta = new Vector2(880f, height);

            Image bg = row.AddComponent<Image>();
            bg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(new Color(0.10f, 0.13f, 0.24f, 0.95f), new Color(1f, 1f, 1f, 0.12f), 1.5f, 22);
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = false;

            // İkon (140x140)
            Sprite sp = !string.IsNullOrEmpty(spritePath) ? Resources.Load<Sprite>(spritePath) : null;
            if (sp == null && name.Contains("Gold")) sp = ProceduralSpriteGenerator.CreateGoldCoinSprite(128);
            if (sp == null && name.Contains("Wardrobe")) sp = Resources.Load<Sprite>("Sprites/Kiyafetler/kiyafet_klasik_pelerin");

            if (sp != null)
            {
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(row.transform, false);
                RectTransform iconRt = iconObj.AddComponent<RectTransform>();
                iconRt.anchoredPosition = new Vector2(-350f, 0f);
                iconRt.sizeDelta = new Vector2(140f, 140f);
                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = sp;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
            }

            // Başlık
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(row.transform, false);
            RectTransform titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchoredPosition = new Vector2(40f, 42f);
            titleRt.sizeDelta = new Vector2(670f, 48f);
            Text titleTxt = titleObj.AddComponent<Text>();
            titleTxt.font = GetSafeDefaultFont();
            titleTxt.text = title;
            titleTxt.fontSize = 36;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleLeft;
            titleTxt.color = titleColor;
            titleTxt.raycastTarget = false;

            // Açıklama
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(row.transform, false);
            RectTransform descRt = descObj.AddComponent<RectTransform>();
            descRt.anchoredPosition = new Vector2(40f, -24f);
            descRt.sizeDelta = new Vector2(670f, 90f);
            Text descTxt = descObj.AddComponent<Text>();
            descTxt.font = GetSafeDefaultFont();
            descTxt.text = desc;
            descTxt.fontSize = 27;
            descTxt.alignment = TextAnchor.MiddleLeft;
            descTxt.color = new Color(0.85f, 0.92f, 1f);
            descTxt.raycastTarget = false;
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
    }
}
