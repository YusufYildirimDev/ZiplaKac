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

        private static GameObject CreateHUDBadge(Transform parent, string name, Vector2 size, Vector2 position, Color bgColor, Color borderColor, float borderWidth, Sprite iconSprite, string initialText, Color textColor, int fontSize, out Text outText)
        {
            GameObject card = new GameObject(name);
            card.transform.SetParent(parent, false);
            RectTransform rt = card.AddComponent<RectTransform>();
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

        private void CreateMainMenuPanel(Transform parent)
        {
            GameObject panel = new GameObject("MainMenuPanel");
            panel.transform.SetParent(parent, false);
            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.45f);

            // Centered Glassmorphic Card (920x1260, Y: +90 so player is clearly visible below)
            GameObject card = CreateCardContainer(panel.transform, "MenuCard", new Vector2(920f, 1260f));
            card.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 90f);

            // Başlık "ZIPLA KAÇ" (Büyük ve Belirgin, Şık Sıcak Altın)
            CreateText(card.transform, "TitleText", "ZIPLA KAÇ", 72, new Vector2(0f, 510f), new Color(1.0f, 0.84f, 0.35f), false, FontStyle.Bold);

            // Alt Başlık "tek dokunuş, sonsuz kaçış"
            CreateText(card.transform, "SubtitleText", "tek dokunuş, sonsuz kaçış", 28, new Vector2(0f, 435f), new Color(0.62f, 0.70f, 0.82f), false);

            // "KARAKTER SEÇ" etiketi
            CreateText(card.transform, "SelectPrompt", "KARAKTER SEÇ", 26, new Vector2(0f, 320f), new Color(0.72f, 0.80f, 0.92f), false, FontStyle.Bold);

            // Karakter Galerisi UI (Swatches - Genişletilmiş Alan)
            GameObject galleryObj = new GameObject("CharacterGallery");
            galleryObj.transform.SetParent(card.transform, false);
            RectTransform galleryRt = galleryObj.AddComponent<RectTransform>();
            galleryRt.anchoredPosition = new Vector2(0f, 175f);
            galleryRt.sizeDelta = new Vector2(880f, 210f);

            galleryObj.AddComponent<CharacterSelectorUI>();

            // 1. "BAŞLA" Butonu (Hero Action - Şık Safir Mavi #2563eb - Standart 540x86, Çerçevesiz)
            Color primaryBlue = new Color(0.14f, 0.40f, 0.90f);
            GameObject btnObj = CreatePillButton(card.transform, "StartButton", "BAŞLA", new Vector2(0f, -60f), new Vector2(540f, 86f), primaryBlue, Color.white, 32);
            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => {
                if (GameManager.Instance != null) GameManager.Instance.StartGame();
            });

            // 2. 🛒 Mağaza & Gardırop Butonu (Şık Ametist / Asil Mor #6d28d9 - Standart 540x86, Çerçevesiz)
            Color purpleBg = new Color(0.42f, 0.22f, 0.78f);
            GameObject wardrobeBtnObj = CreatePillButton(card.transform, "WardrobeButton", "🛒 MAĞAZA & GARDIROP", new Vector2(0f, -165f), new Vector2(540f, 86f), purpleBg, Color.white, 30);
            Button wardrobeBtn = wardrobeBtnObj.GetComponent<Button>();
            wardrobeBtn.onClick.AddListener(() => {
                if (UI.WardrobeModalUI.Instance != null)
                {
                    UI.WardrobeModalUI.Instance.gameObject.SetActive(true);
                }
            });

            // 3. 🎁 Günlük Ödül Sandığı Butonu (Sıcak Kehribar / Hazine Altını #d97706 - Standart 540x86, Çerçevesiz)
            Color goldBg = new Color(0.85f, 0.52f, 0.08f);
            GameObject rewardBtnObj = CreatePillButton(card.transform, "DailyRewardButton", "🎁 GÜNLÜK ÖDÜL SANDIĞI", new Vector2(0f, -270f), new Vector2(540f, 86f), goldBg, Color.white, 30);
            Button rewardBtn = rewardBtnObj.GetComponent<Button>();
            rewardBtn.onClick.AddListener(() => {
                if (DailyRewardModalUI.Instance != null)
                {
                    DailyRewardModalUI.Instance.gameObject.SetActive(true);
                }
            });

            // 4. 📖 Nasıl Oynanır & Rehber Butonu (Derin Okyanus / Çelik Camgöbeği #0284c7 - Standart 540x86, Çerçevesiz)
            Color tealBg = new Color(0.08f, 0.50f, 0.72f);
            GameObject rulesBtnObj = CreatePillButton(card.transform, "RulesButton", "📖 NASIL OYNANIR?", new Vector2(0f, -375f), new Vector2(540f, 86f), tealBg, Color.white, 30);
            Button rulesBtn = rulesBtnObj.GetComponent<Button>();
            rulesBtn.onClick.AddListener(() => {
                Debug.Log("[RulesButton] Clicked! Opening onboarding modal...");
                AudioManager.Instance?.PlayJumpSound();
                if (UI.OnboardingModalUI.Instance != null)
                {
                    UI.OnboardingModalUI.Instance.OpenModal();
                }
                else
                {
                    var found = Object.FindObjectOfType<UI.OnboardingModalUI>(true);
                    if (found != null)
                    {
                        found.OpenModal();
                    }
                    else
                    {
                        Debug.LogError("[RulesButton] OnboardingModalUI not found!");
                    }
                }
            });

            // Alt İpucu
            CreateText(card.transform, "ControlHint", "Zıplamak için dokun / Boşluk", 24, new Vector2(0f, -470f), new Color(0.55f, 0.65f, 0.8f), false);

            if (UIManager.Instance != null) UIManager.Instance.mainMenuPanel = panel;
        }

        private void CreateHUDPanel(Transform parent)
        {
            GameObject panel = new GameObject("HUDPanel");
            panel.transform.SetParent(parent, false);
            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            HUDController hud = panel.AddComponent<HUDController>();

            // Top Header Bar — Premium Arcade Glassmorphic Badges
            // 1. Anlık Skor (Sol Üst Neon Mint Badge)
            Sprite scoreIcon = ProceduralSpriteGenerator.CreateScoreStarSprite();
            CreateHUDBadge(panel.transform, "ScorePill", new Vector2(340f, 96f), new Vector2(-330f, 830f),
                new Color(0.06f, 0.09f, 0.18f, 0.94f), new Color(0.024f, 0.839f, 0.627f, 0.95f), 3.0f,
                scoreIcon, "<size=20><color=#8da4c4>SKOR</color></size>\n<size=42><color=#ffffff><b>0</b></color></size>",
                Color.white, 24, out hud.currentScoreText);

            // 2. Altın Rozeti (Sol Üst Skorun altında — Parlak Altın Sikke Rozeti)
            Sprite coinIcon = ProceduralSpriteGenerator.CreateGoldCoinSprite();
            CreateHUDBadge(panel.transform, "GoldPill", new Vector2(280f, 86f), new Vector2(-360f, 715f),
                new Color(0.09f, 0.08f, 0.14f, 0.94f), new Color(1.0f, 0.82f, 0.25f, 0.95f), 3.0f,
                coinIcon, "<size=18><color=#ffd166>ALTIN</color></size>\n<size=38><color=#ffea79><b>0</b></color></size>",
                new Color(1.0f, 0.92f, 0.45f), 22, out hud.goldText);

            // 3. En İyi Skor (Sağ Üst — Parlak Altın Kupa / Rekor Rozeti)
            Sprite trophyIcon = ProceduralSpriteGenerator.CreateTrophyIconSprite();
            CreateHUDBadge(panel.transform, "HighScorePill", new Vector2(340f, 96f), new Vector2(330f, 830f),
                new Color(0.08f, 0.07f, 0.16f, 0.94f), new Color(0.98f, 0.75f, 0.18f, 0.95f), 3.0f,
                trophyIcon, "<size=20><color=#ffd166>EN İYİ</color></size>\n<size=42><color=#ffffff><b>0</b></color></size>",
                Color.white, 24, out hud.highScoreText);

            // Active PowerUp Badges Bar (Üst Orta - İki rozet arası)
            GameObject badgeBar = new GameObject("PowerUpBadgeBar");
            badgeBar.transform.SetParent(panel.transform, false);
            RectTransform barRt = badgeBar.AddComponent<RectTransform>();
            barRt.anchoredPosition = new Vector2(0f, 830f);
            barRt.sizeDelta = new Vector2(300f, 80f);

            hud.shieldBadgeText = CreateText(badgeBar.transform, "ShieldBadge", "Kalkan", 26, new Vector2(-95f, 0f), new Color(0.30f, 0.79f, 0.94f), false, FontStyle.Bold);
            hud.shieldBadgeObject = hud.shieldBadgeText.gameObject;
            hud.shieldBadgeObject.SetActive(false);

            hud.slowBadgeText = CreateText(badgeBar.transform, "SlowBadge", "Yavaş", 26, new Vector2(0f, 0f), new Color(0.56f, 0.88f, 0.94f), false, FontStyle.Bold);
            hud.slowBadgeObject = hud.slowBadgeText.gameObject;
            hud.slowBadgeObject.SetActive(false);

            hud.multiBadgeText = CreateText(badgeBar.transform, "MultiBadge", "x2 Puan", 26, new Vector2(95f, 0f), new Color(1.0f, 0.84f, 0.04f), false, FontStyle.Bold);
            hud.multiBadgeObject = hud.multiBadgeText.gameObject;
            hud.multiBadgeObject.SetActive(false);

            // Merkez Güçlendirme Geri Sayaç (Ekranın Tam Ortasında Büyük Halka/Panel)
            GameObject countdownPanel = new GameObject("PowerUpCountdownPanel");
            countdownPanel.transform.SetParent(panel.transform, false);
            RectTransform cdRt = countdownPanel.AddComponent<RectTransform>();
            cdRt.anchoredPosition = new Vector2(0f, 150f);
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

            // Alt Kontrol İpucu
            hud.controlTipText = CreateText(panel.transform, "ControlTipText", "Zıpla: Dokun / Space", 24, new Vector2(0f, -840f), new Color(0.6f, 0.7f, 0.85f, 0.7f), false);

            // Kombo Bildirim Kartı (Ekranın Üst-Orta Kısmı, Y: 520, X: 0 - Asla sola kaymaz, taşmaz)
            GameObject comboCard = CreateCardContainer(panel.transform, "ComboNotificationCard", new Vector2(460f, 84f));
            RectTransform comboRt = comboCard.GetComponent<RectTransform>();
            comboRt.anchoredPosition = new Vector2(0f, 520f);

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
            bg.color = new Color(0f, 0f, 0f, 0.78f);

            GameOverUI goUI = panel.AddComponent<GameOverUI>();

            // Kompakt & Şık Obsidian Glass Kart (860x1080, Y: +40)
            GameObject card = CreateCardContainer(panel.transform, "GameOverCard", new Vector2(860f, 1080f));
            card.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 40f);

            // 1. Başlık "OYUN BİTTİ" (Şık Beyaz #f8fafc)
            CreateText(card.transform, "GameOverTitle", "OYUN BİTTİ", 48, new Vector2(0f, 450f), new Color(0.97f, 0.98f, 0.99f), false, FontStyle.Bold);

            // Alt Bilgi "Bir engele çarptın" (Muted Slate #94a3b8)
            CreateText(card.transform, "GameOverSub", "Bir engele çarptın", 22, new Vector2(0f, 402f), new Color(0.58f, 0.64f, 0.72f), false);

            // 2. Özel Vurgulu Skor Kutusu (Glassmorphic Score Box)
            GameObject scoreBox = new GameObject("ScoreBox");
            scoreBox.transform.SetParent(card.transform, false);
            RectTransform sbRt = scoreBox.AddComponent<RectTransform>();
            sbRt.anchoredPosition = new Vector2(0f, 275f);
            sbRt.sizeDelta = new Vector2(780f, 190f);

            Image sbBg = scoreBox.AddComponent<Image>();
            sbBg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(
                new Color(0.08f, 0.11f, 0.20f, 0.92f),
                new Color(0.20f, 0.27f, 0.38f, 0.75f),
                1.5f, 20, 128, 128
            );
            sbBg.type = Image.Type.Sliced;
            sbBg.raycastTarget = false;

            // Skor Kutusu İçi - Başlık
            CreateText(scoreBox.transform, "ScoreLabel", "BU KOŞUDAKİ SKORUN", 20, new Vector2(0f, 56f), new Color(0.58f, 0.64f, 0.72f), false, FontStyle.Bold);

            // Skor Kutusu İçi - Büyük Net Rakam (78pt Bold - Bembeyaz & Kırpılmaz)
            goUI.finalScoreText = CreateText(scoreBox.transform, "FinalScoreText", "0", 78, new Vector2(0f, 2f), Color.white, false, FontStyle.Bold);

            // Skor Kutusu İçi - En İyi Skor
            goUI.highScoreText = CreateText(scoreBox.transform, "HighScoreText", "🏆 En İyi Skor: 0", 22, new Vector2(0f, -56f), new Color(0.96f, 0.62f, 0.04f), false, FontStyle.Bold);

            // 3. Yeni Rekor Rozeti
            GameObject recBadge = new GameObject("NewRecordBadge");
            recBadge.transform.SetParent(card.transform, false);
            RectTransform recRt = recBadge.AddComponent<RectTransform>();
            recRt.anchoredPosition = new Vector2(0f, 155f);
            recRt.sizeDelta = new Vector2(340f, 36f);
            Image recBg = recBadge.AddComponent<Image>();
            recBg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(new Color(0.96f, 0.62f, 0.04f), Color.white, 1f, 18, 128, 64);
            recBg.type = Image.Type.Sliced;
            recBg.raycastTarget = false;

            Text recTxt = CreateText(recBadge.transform, "RecText", "★ YENİ REKOR! ★", 20, Vector2.zero, new Color(0.06f, 0.09f, 0.16f), false, FontStyle.Bold);
            recTxt.rectTransform.sizeDelta = new Vector2(340f, 36f);
            goUI.newRecordBadge = recBadge;
            goUI.newRecordBadge.SetActive(false);

            // 4. Altın & Kasa Bilgi Şeridi (Büyük ve Belirgin!)
            GameObject earnBanner = new GameObject("EarningsBanner");
            earnBanner.transform.SetParent(card.transform, false);
            RectTransform ebRt = earnBanner.AddComponent<RectTransform>();
            ebRt.anchoredPosition = new Vector2(0f, 100f);
            ebRt.sizeDelta = new Vector2(780f, 66f);

            Image ebBg = earnBanner.AddComponent<Image>();
            ebBg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(
                new Color(0.08f, 0.11f, 0.20f, 0.90f),
                new Color(0.96f, 0.70f, 0.18f, 0.40f),
                1.5f, 18, 128, 64
            );
            ebBg.type = Image.Type.Sliced;
            ebBg.raycastTarget = false;

            GameObject ebContent = new GameObject("Content");
            ebContent.transform.SetParent(earnBanner.transform, false);
            RectTransform ebcRt = ebContent.AddComponent<RectTransform>();
            ebcRt.anchorMin = Vector2.zero; ebcRt.anchorMax = Vector2.one;
            ebcRt.offsetMin = Vector2.zero; ebcRt.offsetMax = Vector2.zero;

            HorizontalLayoutGroup ebHlg = ebContent.AddComponent<HorizontalLayoutGroup>();
            ebHlg.childAlignment = TextAnchor.MiddleCenter;
            ebHlg.spacing = 12f;
            ebHlg.childControlWidth = true;
            ebHlg.childControlHeight = false;
            ebHlg.childForceExpandWidth = false;
            ebHlg.childForceExpandHeight = false;

            GameObject ebCoin = new GameObject("CoinIcon");
            ebCoin.transform.SetParent(ebContent.transform, false);
            RectTransform ebCoinRt = ebCoin.AddComponent<RectTransform>();
            ebCoinRt.sizeDelta = new Vector2(32f, 32f);
            LayoutElement coinLe = ebCoin.AddComponent<LayoutElement>();
            coinLe.preferredWidth = 32f;
            coinLe.preferredHeight = 32f;
            coinLe.minWidth = 32f;
            coinLe.minHeight = 32f;
            coinLe.flexibleWidth = 0f;

            Image ebCoinImg = ebCoin.AddComponent<Image>();
            ebCoinImg.sprite = ProceduralSpriteGenerator.CreateGoldCoinSprite(64);
            ebCoinImg.preserveAspect = true;
            ebCoinImg.raycastTarget = false;

            GameObject ebTextObj = new GameObject("GoldEarnedText");
            ebTextObj.transform.SetParent(ebContent.transform, false);
            goUI.goldEarnedText = ebTextObj.AddComponent<Text>();
            goUI.goldEarnedText.font = GetSafeDefaultFont();
            goUI.goldEarnedText.fontSize = 24;
            goUI.goldEarnedText.fontStyle = FontStyle.Bold;
            goUI.goldEarnedText.color = Color.white;
            goUI.goldEarnedText.supportRichText = true;
            goUI.goldEarnedText.text = "<size=32><color=#ffd166><b>+0</b></color></size> <color=#ffffff>ALTIN KAZANDIN</color>    <color=#64748b>•</color>    <color=#94a3b8>KASA:</color> <size=32><color=#ffd166><b>0</b></color></size> <color=#ffd166>ALTIN</color>";
            goUI.goldEarnedText.alignment = TextAnchor.MiddleLeft;
            goUI.goldEarnedText.horizontalOverflow = HorizontalWrapMode.Overflow;
            goUI.goldEarnedText.verticalOverflow = VerticalWrapMode.Overflow;
            goUI.goldEarnedText.raycastTarget = false;

            Shadow txtShadow = ebTextObj.AddComponent<Shadow>();
            txtShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            txtShadow.effectDistance = new Vector2(2f, -2f);

            // 5. Son 5 Skor Listesi
            goUI.scoreHistoryListText = CreateText(card.transform, "HistoryListText", "", 20, new Vector2(0f, 50f), new Color(0.40f, 0.46f, 0.56f), false);

            // ==========================================
            // BUTONLAR (Standart 540x86 px, Taşmasız Rozetler)
            // ==========================================

            // 6. ❤️ CAN AL & DEVAM ET Butonu (Derin Rose-Crimson #be123c)
            GameObject reviveBtn = new GameObject("ReviveButton");
            reviveBtn.transform.SetParent(card.transform, false);
            RectTransform reviveRt = reviveBtn.AddComponent<RectTransform>();
            reviveRt.anchoredPosition = new Vector2(0f, -30f);
            reviveRt.sizeDelta = new Vector2(540f, 86f);

            Image reviveBg = reviveBtn.AddComponent<Image>();
            reviveBg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(
                new Color(0.74f, 0.07f, 0.24f),
                Color.clear,
                0f, 22, 128, 64
            );
            reviveBg.type = Image.Type.Sliced;
            goUI.reviveButtonImage = reviveBg;

            Button reviveBtnComp = reviveBtn.AddComponent<Button>();
            reviveBtnComp.targetGraphic = reviveBg;
            goUI.reviveButton = reviveBtnComp;

            // Aksiyon Yazısı (Sol tarafa sabit, taşma payı bırakılmış)
            GameObject revTitleObj = new GameObject("Title");
            revTitleObj.transform.SetParent(reviveBtn.transform, false);
            RectTransform revTitleRt = revTitleObj.AddComponent<RectTransform>();
            revTitleRt.anchorMin = Vector2.zero;
            revTitleRt.anchorMax = Vector2.one;
            revTitleRt.offsetMin = new Vector2(28f, 0f);
            revTitleRt.offsetMax = new Vector2(-140f, 0f);

            Text revTitleTxt = revTitleObj.AddComponent<Text>();
            revTitleTxt.font = GetSafeDefaultFont();
            revTitleTxt.fontSize = 25;
            revTitleTxt.fontStyle = FontStyle.Bold;
            revTitleTxt.color = Color.white;
            revTitleTxt.text = "CAN AL & DEVAM ET";
            revTitleTxt.alignment = TextAnchor.MiddleLeft;
            revTitleTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            revTitleTxt.verticalOverflow = VerticalWrapMode.Overflow;
            revTitleTxt.raycastTarget = false;
            goUI.reviveButtonText = revTitleTxt;

            // Sağ Taraf: Sağ kenara 16px mesafeyle kilitli rozet (Kesinlikle taşmaz!)
            GameObject revBadgeObj = new GameObject("PriceBadge");
            revBadgeObj.transform.SetParent(reviveBtn.transform, false);
            RectTransform revBadgeRt = revBadgeObj.AddComponent<RectTransform>();
            revBadgeRt.anchorMin = new Vector2(1f, 0.5f);
            revBadgeRt.anchorMax = new Vector2(1f, 0.5f);
            revBadgeRt.pivot = new Vector2(1f, 0.5f);
            revBadgeRt.anchoredPosition = new Vector2(-16f, 0f);
            revBadgeRt.sizeDelta = new Vector2(116f, 42f);

            Image revBadgeBg = revBadgeObj.AddComponent<Image>();
            revBadgeBg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(new Color(0f, 0f, 0f, 0.35f), Color.clear, 0f, 14, 64, 64);
            revBadgeBg.type = Image.Type.Sliced;
            revBadgeBg.raycastTarget = false;

            // Rozet içi Altın Sikkesi (24x24)
            GameObject revCoinObj = new GameObject("CoinIcon");
            revCoinObj.transform.SetParent(revBadgeObj.transform, false);
            RectTransform revCoinRt = revCoinObj.AddComponent<RectTransform>();
            revCoinRt.anchorMin = new Vector2(0f, 0.5f);
            revCoinRt.anchorMax = new Vector2(0f, 0.5f);
            revCoinRt.pivot = new Vector2(0.5f, 0.5f);
            revCoinRt.anchoredPosition = new Vector2(20f, 0f);
            revCoinRt.sizeDelta = new Vector2(24f, 24f);
            Image revCoinImg = revCoinObj.AddComponent<Image>();
            revCoinImg.sprite = ProceduralSpriteGenerator.CreateGoldCoinSprite(64);
            revCoinImg.preserveAspect = true;
            revCoinImg.raycastTarget = false;
            goUI.reviveCoinIcon = revCoinImg;

            // Rozet içi Fiyat Rakamı
            GameObject revPriceObj = new GameObject("Price");
            revPriceObj.transform.SetParent(revBadgeObj.transform, false);
            RectTransform revPriceRt = revPriceObj.AddComponent<RectTransform>();
            revPriceRt.anchorMin = Vector2.zero;
            revPriceRt.anchorMax = Vector2.one;
            revPriceRt.offsetMin = new Vector2(34f, 0f);
            revPriceRt.offsetMax = new Vector2(-4f, 0f);

            Text revPriceTxt = revPriceObj.AddComponent<Text>();
            revPriceTxt.font = GetSafeDefaultFont();
            revPriceTxt.fontSize = 22;
            revPriceTxt.fontStyle = FontStyle.Bold;
            revPriceTxt.color = new Color(0.98f, 0.75f, 0.14f);
            revPriceTxt.text = "500";
            revPriceTxt.alignment = TextAnchor.MiddleCenter;
            revPriceTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            revPriceTxt.verticalOverflow = VerticalWrapMode.Overflow;
            revPriceTxt.raycastTarget = false;
            goUI.revivePriceText = revPriceTxt;

            // 7. TEKRAR OYNA Butonu (Ana Eylem - Şık Safir/İndigo #2563eb)
            Color primaryBlue = new Color(0.14f, 0.38f, 0.92f);
            GameObject restartBtn = CreatePillButton(card.transform, "RestartButton", "TEKRAR OYNA", new Vector2(0f, -130f), new Vector2(540f, 86f), primaryBlue, Color.white, 30);
            goUI.restartButton = restartBtn.GetComponent<Button>();

            // 8. REKLAM İZLE Butonu (Koyu Slate #182033 + Altın Rozeti)
            GameObject adBtn = new GameObject("RewardedAdButton");
            adBtn.transform.SetParent(card.transform, false);
            RectTransform adRt = adBtn.AddComponent<RectTransform>();
            adRt.anchoredPosition = new Vector2(0f, -230f);
            adRt.sizeDelta = new Vector2(540f, 86f);

            Color slateBtnBg = new Color(0.10f, 0.14f, 0.24f);
            Color slateBorder = new Color(0.24f, 0.32f, 0.46f, 0.6f);
            Image adBg = adBtn.AddComponent<Image>();
            adBg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(slateBtnBg, slateBorder, 1.5f, 22, 128, 64);
            adBg.type = Image.Type.Sliced;

            Button adBtnComp = adBtn.AddComponent<Button>();
            adBtnComp.targetGraphic = adBg;
            goUI.rewardedAdButton = adBtnComp;

            GameObject adTitleObj = new GameObject("Title");
            adTitleObj.transform.SetParent(adBtn.transform, false);
            RectTransform adTitleRt = adTitleObj.AddComponent<RectTransform>();
            adTitleRt.anchorMin = Vector2.zero;
            adTitleRt.anchorMax = Vector2.one;
            adTitleRt.offsetMin = new Vector2(28f, 0f);
            adTitleRt.offsetMax = new Vector2(-140f, 0f);

            Text adTitleTxt = adTitleObj.AddComponent<Text>();
            adTitleTxt.font = GetSafeDefaultFont();
            adTitleTxt.fontSize = 25;
            adTitleTxt.fontStyle = FontStyle.Bold;
            adTitleTxt.color = new Color(0.95f, 0.97f, 1.0f);
            adTitleTxt.text = "REKLAM İZLE";
            adTitleTxt.alignment = TextAnchor.MiddleLeft;
            adTitleTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            adTitleTxt.verticalOverflow = VerticalWrapMode.Overflow;
            adTitleTxt.raycastTarget = false;
            goUI.rewardedAdButtonText = adTitleTxt;

            // Sağ Taraf: Reklam Ödül Rozeti (+100 🪙 - Sağ kenara kilitli!)
            GameObject adBadgeObj = new GameObject("RewardBadge");
            adBadgeObj.transform.SetParent(adBtn.transform, false);
            RectTransform adBadgeRt = adBadgeObj.AddComponent<RectTransform>();
            adBadgeRt.anchorMin = new Vector2(1f, 0.5f);
            adBadgeRt.anchorMax = new Vector2(1f, 0.5f);
            adBadgeRt.pivot = new Vector2(1f, 0.5f);
            adBadgeRt.anchoredPosition = new Vector2(-16f, 0f);
            adBadgeRt.sizeDelta = new Vector2(116f, 42f);

            Image adBadgeBg = adBadgeObj.AddComponent<Image>();
            adBadgeBg.sprite = ProceduralSpriteGenerator.CreateRoundedRectSprite(new Color(0f, 0f, 0f, 0.35f), Color.clear, 0f, 14, 64, 64);
            adBadgeBg.type = Image.Type.Sliced;
            adBadgeBg.raycastTarget = false;
            goUI.rewardedAdBadge = adBadgeObj;

            GameObject adCoinObj = new GameObject("CoinIcon");
            adCoinObj.transform.SetParent(adBadgeObj.transform, false);
            RectTransform adCoinRt = adCoinObj.AddComponent<RectTransform>();
            adCoinRt.anchorMin = new Vector2(0f, 0.5f);
            adCoinRt.anchorMax = new Vector2(0f, 0.5f);
            adCoinRt.pivot = new Vector2(0.5f, 0.5f);
            adCoinRt.anchoredPosition = new Vector2(20f, 0f);
            adCoinRt.sizeDelta = new Vector2(24f, 24f);
            Image adCoinImg = adCoinObj.AddComponent<Image>();
            adCoinImg.sprite = ProceduralSpriteGenerator.CreateGoldCoinSprite(64);
            adCoinImg.preserveAspect = true;
            adCoinImg.raycastTarget = false;

            GameObject adValObj = new GameObject("Value");
            adValObj.transform.SetParent(adBadgeObj.transform, false);
            RectTransform adValRt = adValObj.AddComponent<RectTransform>();
            adValRt.anchorMin = Vector2.zero;
            adValRt.anchorMax = Vector2.one;
            adValRt.offsetMin = new Vector2(34f, 0f);
            adValRt.offsetMax = new Vector2(-4f, 0f);

            Text adValTxt = adValObj.AddComponent<Text>();
            adValTxt.font = GetSafeDefaultFont();
            adValTxt.fontSize = 22;
            adValTxt.fontStyle = FontStyle.Bold;
            adValTxt.color = new Color(0.98f, 0.75f, 0.14f);
            adValTxt.text = "+100";
            adValTxt.alignment = TextAnchor.MiddleCenter;
            adValTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            adValTxt.verticalOverflow = VerticalWrapMode.Overflow;
            adValTxt.raycastTarget = false;

            // 9. ANA MENÜ Butonu (Minimalist Slate #0b1120)
            Color ghostBg = new Color(0.06f, 0.09f, 0.16f);
            GameObject menuBtn = CreatePillButton(card.transform, "MainMenuButton", "ANA MENÜ", new Vector2(0f, -330f), new Vector2(540f, 86f), ghostBg, new Color(0.65f, 0.72f, 0.82f), 28);
            goUI.mainMenuButton = menuBtn.GetComponent<Button>();

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

            // 1. Üst Rozet & Başlık (Büyütülmüş)
            modalUI.stepBadgeText = CreateText(card.transform, "StepBadge", "REHBER • ADIM 1 / 4", 28, new Vector2(0f, 660f), new Color(0.024f, 0.839f, 0.627f), false, FontStyle.Bold);
            modalUI.titleText = CreateText(card.transform, "TitleText", "OYUN KURALLARI & KONTROLLER", 44, new Vector2(0f, 605f), new Color(1.0f, 0.84f, 0.35f), false, FontStyle.Bold);

            // Sağ Üst "✕ KAPAT" Butonu (Genişletilmiş dokunma alanı)
            GameObject skipBtnObj = CreatePillButton(card.transform, "SkipButton", "✕ KAPAT", new Vector2(350f, 660f), new Vector2(180f, 64f), new Color(0.18f, 0.24f, 0.38f, 0.95f), new Color(0.9f, 0.95f, 1f), 24);
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

            CreateOnboardingRow(slide0.transform, "Row0_1", 345f, 165f, "👆", "Tek Dokunuşla Zıpla", "Ekrana dokunarak sivri kaktüs ve engellerin üzerinden atla.", new Color(0.024f, 0.839f, 0.627f));
            CreateOnboardingRow(slide0.transform, "Row0_2", 165f, 165f, "🚀", "Çift Zıplama", "Havada bir kez daha dokun, 2 kat yükseğe zıpla!", new Color(0.30f, 0.79f, 0.94f));
            CreateOnboardingRow(slide0.transform, "Row0_3", -15f, 165f, "🔥", "Kombo & Bonus Altın", "Engelleri art arda geç, bonus skor ve altın kazan!", new Color(1.0f, 0.82f, 0.40f));
            CreateOnboardingRow(slide0.transform, "Row0_4", -195f, 165f, "🌅", "3 Zorluk Kademesi", "Puanın arttıkça dünya hızlanır ve geceye döner!", new Color(1.0f, 0.42f, 0.21f));

            // -------------------------------------------------------------
            // SLIDE 1: 5 KARAKTER & SIRALI 2500 SKOR KİLİT SİSTEMİ
            // -------------------------------------------------------------
            GameObject slide1 = new GameObject("Slide_Characters");
            slide1.transform.SetParent(card.transform, false);
            RectTransform s1Rt = slide1.AddComponent<RectTransform>();
            s1Rt.anchorMin = Vector2.zero; s1Rt.anchorMax = Vector2.one;
            s1Rt.offsetMin = Vector2.zero; s1Rt.offsetMax = Vector2.zero;
            modalUI.slides[1] = slide1;

            CreateText(slide1.transform, "Sub1", "Sırayla 2500 skor yaparak karakterlerin kilidini aç:", 30, new Vector2(0f, 520f), new Color(1.0f, 0.84f, 0.35f), false, FontStyle.Bold);

            CreateOnboardingCharacterRow(slide1.transform, "Char_classic", 360f, "classic", "Klasik", "🔓 BAŞLANGIÇTA AÇIK", "Çayır Diyarı • Kalıcı +%15 ekstra skor yeteneği", new Color(1.0f, 0.82f, 0.40f), new Color(0.024f, 0.839f, 0.627f));
            CreateOnboardingCharacterRow(slide1.transform, "Char_fire", 200f, "fire", "Ateş", "🔒 KLASİK İLE 2500 SKOR", "Volkan Diyarı • Hızlı sıçrama ve kor alev dünyası", new Color(1.0f, 0.42f, 0.21f), new Color(1.0f, 0.65f, 0.20f));
            CreateOnboardingCharacterRow(slide1.transform, "Char_ice", 40f, "ice", "Buz", "🔒 ATEŞ İLE 2500 SKOR", "Buzul Dağları • Net görüş ve buz diyarı atmosferi", new Color(0.30f, 0.79f, 0.94f), new Color(0.30f, 0.79f, 0.94f));
            CreateOnboardingCharacterRow(slide1.transform, "Char_toxic", -120f, "toxic", "Zehir", "🔒 BUZ İLE 2500 SKOR", "Toksik Orman • x1.25 ekstra kombo altın bonusu", new Color(0.49f, 0.95f, 0.61f), new Color(0.49f, 0.95f, 0.61f));
            CreateOnboardingCharacterRow(slide1.transform, "Char_shadow", -280f, "shadow", "Gece", "🔒 ZEHİR İLE 2500 SKOR", "Nebula Uzayı • %28 engellerden otomatik sıyrılma", new Color(0.61f, 0.36f, 0.90f), new Color(0.61f, 0.36f, 0.90f));

            // -------------------------------------------------------------
            // SLIDE 2: GÜÇLER, YÜKSELTMELER VE BİLETLER
            // -------------------------------------------------------------
            GameObject slide2 = new GameObject("Slide_Powers");
            slide2.transform.SetParent(card.transform, false);
            RectTransform s2Rt = slide2.AddComponent<RectTransform>();
            s2Rt.anchorMin = Vector2.zero; s2Rt.anchorMax = Vector2.one;
            s2Rt.offsetMin = Vector2.zero; s2Rt.offsetMax = Vector2.zero;
            modalUI.slides[2] = slide2;

            CreateText(slide2.transform, "Sub2", "Güçlendirmeleri topla ve mağazadan yetenekler edin!", 32, new Vector2(0f, 520f), new Color(0.70f, 0.80f, 0.95f), false, FontStyle.Bold);

            CreateOnboardingPowerRow(slide2.transform, "Power_Shield", 345f, "shield", "🛡️", "Koruyucu Kalkan", "Engellere çarpmayı önler, koşunu güvenle devam ettirir.", new Color(0.30f, 0.79f, 0.94f));
            CreateOnboardingPowerRow(slide2.transform, "Power_Slow", 165f, "slow", "⏱️", "Ağır Çekim (Slow-Mo)", "5 saniye oyunu yavaşlatır, kolayca sıyrılmanı sağlar.", new Color(0.024f, 0.839f, 0.627f));
            CreateOnboardingPowerRow(slide2.transform, "Power_Multi", -15f, "multi", "✖️2️⃣", "x2 Skor & Altın Çarpanı", "Tüm skor ve engellerden gelen altınları 2 katına çıkarır!", new Color(1.0f, 0.82f, 0.40f));
            CreateOnboardingPowerRow(slide2.transform, "Power_SecondChance", -195f, "second_chance", "🎫", "İkinci Şans Bileti", "Yandığında baştan başlamak yerine kaldığın yerden devam et!", new Color(0.61f, 0.36f, 0.90f));

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

            CreateOnboardingShowcaseRow(slide3.transform, "Showcase_Chest", 310f, 220f, "Sprites/treasure_chest", "🎁 Günlük Ödül Sandığı", "Her gün oyuna girerek sandığını aç! Seri yaptıkça altın ödülü katlanır.", new Color(1.0f, 0.82f, 0.40f));
            CreateOnboardingShowcaseRow(slide3.transform, "Showcase_Gold", 65f, 220f, null, "🪙 Altın Topla & Kasanı Büyüt", "Engelleri aştıkça altın topla, kasanı zenginleştir ve yeni eşyalar al!", new Color(0.024f, 0.839f, 0.627f));
            CreateOnboardingShowcaseRow(slide3.transform, "Showcase_Wardrobe", -180f, 220f, null, "👔 Gardırop & Havalı Kostümler", "Topladığın altınlarla kahramanlarına pelerin, gözlük ve şapkalar giydir!", new Color(0.61f, 0.36f, 0.90f));

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
            titleRt.anchorMin = new Vector2(0f, 0.5f);
            titleRt.anchorMax = new Vector2(0f, 0.5f);
            titleRt.pivot = new Vector2(0f, 0.5f);
            titleRt.anchoredPosition = new Vector2(170f, 28f);
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
            descRt.anchorMin = new Vector2(0f, 0.5f);
            descRt.anchorMax = new Vector2(0f, 0.5f);
            descRt.pivot = new Vector2(0f, 0.5f);
            descRt.anchoredPosition = new Vector2(170f, -24f);
            descRt.sizeDelta = new Vector2(680f, 65f);
            Text descTxt = descObj.AddComponent<Text>();
            descTxt.font = GetSafeDefaultFont();
            descTxt.text = desc;
            descTxt.fontSize = 28;
            descTxt.alignment = TextAnchor.MiddleLeft;
            descTxt.color = new Color(0.88f, 0.94f, 1f);
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
            titleRt.anchorMin = new Vector2(0f, 0.5f);
            titleRt.anchorMax = new Vector2(0f, 0.5f);
            titleRt.pivot = new Vector2(0f, 0.5f);
            titleRt.anchoredPosition = new Vector2(170f, 26f);
            titleRt.sizeDelta = new Vector2(230f, 44f);
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
            reqRt.anchorMin = new Vector2(1f, 0.5f);
            reqRt.anchorMax = new Vector2(1f, 0.5f);
            reqRt.pivot = new Vector2(1f, 0.5f);
            reqRt.anchoredPosition = new Vector2(-30f, 26f);
            reqRt.sizeDelta = new Vector2(450f, 44f);
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
            descRt.anchorMin = new Vector2(0f, 0.5f);
            descRt.anchorMax = new Vector2(0f, 0.5f);
            descRt.pivot = new Vector2(0f, 0.5f);
            descRt.anchoredPosition = new Vector2(170f, -24f);
            descRt.sizeDelta = new Vector2(670f, 50f);
            Text descTxt = descObj.AddComponent<Text>();
            descTxt.font = GetSafeDefaultFont();
            descTxt.text = desc;
            descTxt.fontSize = 27;
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
            titleRt.anchorMin = new Vector2(0f, 0.5f);
            titleRt.anchorMax = new Vector2(0f, 0.5f);
            titleRt.pivot = new Vector2(0f, 0.5f);
            titleRt.anchoredPosition = new Vector2(170f, 28f);
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
            descRt.anchorMin = new Vector2(0f, 0.5f);
            descRt.anchorMax = new Vector2(0f, 0.5f);
            descRt.pivot = new Vector2(0f, 0.5f);
            descRt.anchoredPosition = new Vector2(170f, -24f);
            descRt.sizeDelta = new Vector2(680f, 65f);
            Text descTxt = descObj.AddComponent<Text>();
            descTxt.font = GetSafeDefaultFont();
            descTxt.text = desc;
            descTxt.fontSize = 28;
            descTxt.alignment = TextAnchor.MiddleLeft;
            descTxt.color = new Color(0.88f, 0.94f, 1f);
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
            titleRt.anchorMin = new Vector2(0f, 0.5f);
            titleRt.anchorMax = new Vector2(0f, 0.5f);
            titleRt.pivot = new Vector2(0f, 0.5f);
            titleRt.anchoredPosition = new Vector2(190f, 36f);
            titleRt.sizeDelta = new Vector2(660f, 48f);
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
            descRt.anchorMin = new Vector2(0f, 0.5f);
            descRt.anchorMax = new Vector2(0f, 0.5f);
            descRt.pivot = new Vector2(0f, 0.5f);
            descRt.anchoredPosition = new Vector2(190f, -24f);
            descRt.sizeDelta = new Vector2(660f, 85f);
            Text descTxt = descObj.AddComponent<Text>();
            descTxt.font = GetSafeDefaultFont();
            descTxt.text = desc;
            descTxt.fontSize = 28;
            descTxt.alignment = TextAnchor.MiddleLeft;
            descTxt.color = new Color(0.88f, 0.94f, 1f);
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
