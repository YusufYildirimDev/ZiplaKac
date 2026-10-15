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

                float tileW = 11.13f;
                sg.tileWidth = tileW;
                sg.resetThresholdX = -tileW;

                int tileCount = 3;
                sg.tiles = new Transform[tileCount];
                sg.renderers = new SpriteRenderer[tileCount];
                Material safeMat = GetSafeSpriteMaterial();

                for (int i = 0; i < tileCount; i++)
                {
                    GameObject tileObj = new GameObject($"GroundTile_{i}");
                    tileObj.transform.SetParent(ground.transform, false);
                    tileObj.transform.localPosition = new Vector3(i * tileW, 0f, 0f);

                    SpriteRenderer sr = tileObj.AddComponent<SpriteRenderer>();
                    sr.sprite = sg.tierSprites[0];
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

            Color obsColor = new Color(0.94f, 0.28f, 0.44f); // #ef476f (HTML Obstacle Coral)
            Color airColor = new Color(0.35f, 0.28f, 0.5f);  // #30264a (HTML Air Bird Violet)

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

            // Centered Glassmorphic Card (760x840, Y: +120 so player is clearly visible below)
            GameObject card = CreateCardContainer(panel.transform, "MenuCard", new Vector2(760f, 840f));
            card.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 120f);

            // Başlık "ZIPLA KAÇ"
            CreateText(card.transform, "TitleText", "ZIPLA KAÇ", 54, new Vector2(0f, 315f), new Color(1.0f, 0.82f, 0.4f), false, FontStyle.Bold); // #ffd166

            // Alt Başlık "tek dokunuş, sonsuz kaçış"
            CreateText(card.transform, "SubtitleText", "tek dokunuş, sonsuz kaçış", 24, new Vector2(0f, 260f), new Color(0.6f, 0.7f, 0.85f), false);

            // "KARAKTER SEÇ" etiketi
            CreateText(card.transform, "SelectPrompt", "KARAKTER SEÇ", 20, new Vector2(0f, 160f), new Color(0.75f, 0.82f, 0.95f), false, FontStyle.Bold);

            // Karakter Galerisi UI (Swatches)
            GameObject galleryObj = new GameObject("CharacterGallery");
            galleryObj.transform.SetParent(card.transform, false);
            RectTransform galleryRt = galleryObj.AddComponent<RectTransform>();
            galleryRt.anchoredPosition = new Vector2(0f, 55f);
            galleryRt.sizeDelta = new Vector2(700f, 130f);

            galleryObj.AddComponent<CharacterSelectorUI>();

            // Neon Mint "BAŞLA" Butonu (#06d6a0)
            Color mintColor = new Color(0.024f, 0.839f, 0.627f);
            Color darkNavy = new Color(0.05f, 0.07f, 0.13f);
            GameObject btnObj = CreatePillButton(card.transform, "StartButton", "BAŞLA", new Vector2(0f, -125f), new Vector2(400f, 82f), mintColor, darkNavy, 32);
            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => {
                if (GameManager.Instance != null) GameManager.Instance.StartGame();
            });

            // 🎁 Günlük Ödül Sandığı Butonu (#ffd166 / Amber Vurgusu)
            Color goldColor = new Color(1.0f, 0.82f, 0.40f);
            GameObject rewardBtnObj = CreatePillButton(card.transform, "DailyRewardButton", "🎁 GÜNLÜK ÖDÜL SANDIĞI", new Vector2(0f, -225f), new Vector2(440f, 74f), goldColor, darkNavy, 24);
            Button rewardBtn = rewardBtnObj.GetComponent<Button>();
            rewardBtn.onClick.AddListener(() => {
                if (DailyRewardModalUI.Instance != null)
                {
                    DailyRewardModalUI.Instance.gameObject.SetActive(true);
                }
            });

            // Alt İpucu
            CreateText(card.transform, "ControlHint", "Zıplamak için dokun / Boşluk", 20, new Vector2(0f, -330f), new Color(0.55f, 0.65f, 0.8f), false);

            if (UIManager.Instance != null) UIManager.Instance.mainMenuPanel = panel;
        }

        private void CreateHUDPanel(Transform parent)
        {
            GameObject panel = new GameObject("HUDPanel");
            panel.transform.SetParent(parent, false);
            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

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
            bg.color = new Color(0f, 0f, 0f, 0.65f);

            GameOverUI goUI = panel.AddComponent<GameOverUI>();

            // Centered Glassmorphic Card (760x860, Y: +80)
            GameObject card = CreateCardContainer(panel.transform, "GameOverCard", new Vector2(760f, 860f));
            card.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 80f);

            // Başlık "OYUN BİTTİ" (Coral #ef476f)
            CreateText(card.transform, "GameOverTitle", "OYUN BİTTİ", 52, new Vector2(0f, 325f), new Color(0.94f, 0.28f, 0.44f), false, FontStyle.Bold);

            // Alt Bilgi "Bir engele çarptın!"
            CreateText(card.transform, "GameOverSub", "Bir engele çarptın!", 24, new Vector2(0f, 270f), new Color(0.65f, 0.72f, 0.85f), false);

            // Skor Büyük Rakam
            goUI.finalScoreText = CreateText(card.transform, "FinalScoreText", "0", 80, new Vector2(0f, 185f), Color.white, false, FontStyle.Bold);

            // En İyi Skor
            goUI.highScoreText = CreateText(card.transform, "HighScoreText", "En iyi skor: 0", 28, new Vector2(0f, 110f), new Color(1.0f, 0.82f, 0.4f), false, FontStyle.Bold);

            // Yeni Rekor Rozeti
            Text newRecText = CreateText(card.transform, "NewRecordBadge", "★ YENİ REKOR! ★", 28, new Vector2(0f, 55f), new Color(0.024f, 0.839f, 0.627f), false, FontStyle.Bold);
            goUI.newRecordBadge = newRecText.gameObject;
            goUI.newRecordBadge.SetActive(false);

            // Son 5 Skor Listesi (Kompakt Tek Satır)
            goUI.scoreHistoryListText = CreateText(card.transform, "HistoryListText", "", 22, new Vector2(0f, 5f), new Color(0.7f, 0.78f, 0.9f), false);

            // Tekrar Oyna Butonu (Neon Mint #06d6a0)
            Color mintColor = new Color(0.024f, 0.839f, 0.627f);
            Color darkNavy = new Color(0.05f, 0.07f, 0.13f);
            GameObject restartBtn = CreatePillButton(card.transform, "RestartButton", "TEKRAR OYNA", new Vector2(0f, -95f), new Vector2(400f, 80f), mintColor, darkNavy, 30);
            goUI.restartButton = restartBtn.GetComponent<Button>();

            // 🎁 Ödüllü Reklam İzle Butonu (#ffd166 / Altın Amber Vurgusu)
            Color goldColor = new Color(1.0f, 0.82f, 0.40f);
            GameObject adBtn = CreatePillButton(card.transform, "RewardedAdButton", "🎁 REKLAM İZLE: +50 PUAN", new Vector2(0f, -195f), new Vector2(440f, 74f), goldColor, darkNavy, 24);
            goUI.rewardedAdButton = adBtn.GetComponent<Button>();
            goUI.rewardedAdButtonText = adBtn.GetComponentInChildren<Text>();

            // Ana Menü Butonu (Sleek ikincil buton)
            GameObject menuBtn = CreatePillButton(card.transform, "MainMenuButton", "ANA MENÜ", new Vector2(0f, -295f), new Vector2(320f, 68f), new Color(0.12f, 0.16f, 0.29f, 0.9f), new Color(0.9f, 0.95f, 1f), 24);
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

            // Modal Card (720x820)
            GameObject card = CreateCardContainer(modal.transform, "RewardCard", new Vector2(720f, 820f));
            card.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            // Başlık
            modalUI.titleText = CreateText(card.transform, "Title", "GÜNLÜK ÖDÜL SANDIĞI", 44, new Vector2(0f, 320f), new Color(1.0f, 0.84f, 0.04f), false, FontStyle.Bold);

            // Sandık Görseli (220x220)
            GameObject chestObj = new GameObject("ChestImage");
            chestObj.transform.SetParent(card.transform, false);
            RectTransform chestRt = chestObj.AddComponent<RectTransform>();
            chestRt.anchoredPosition = new Vector2(0f, 130f);
            chestRt.sizeDelta = new Vector2(220f, 220f);
            Image chestImg = chestObj.AddComponent<Image>();
            chestImg.sprite = ProceduralSpriteGenerator.GetSpriteOrGenerate("Sprites/treasure_chest", () => ProceduralSpriteGenerator.CreateRectangleSprite(Color.yellow, 128, 128));
            chestImg.preserveAspect = true;
            chestImg.raycastTarget = false;
            modalUI.chestImage = chestImg;

            // Açıklama
            modalUI.descriptionText = CreateText(card.transform, "Desc", "Her gün oyuna gir, sandığını aç ve bonus puanları topla!", 24, new Vector2(0f, -15f), new Color(0.75f, 0.85f, 0.98f), false);

            // Durum Mesajı
            modalUI.statusText = CreateText(card.transform, "Status", "🎁 Bugünün sandığı açılmaya hazır!", 28, new Vector2(0f, -90f), new Color(0.024f, 0.839f, 0.627f), false, FontStyle.Bold);

            // Sandığı Aç Butonu
            Color mintColor = new Color(0.024f, 0.839f, 0.627f);
            Color darkNavy = new Color(0.05f, 0.07f, 0.13f);
            GameObject claimBtnObj = CreatePillButton(card.transform, "ClaimButton", "SANDIĞI AÇ", new Vector2(0f, -195f), new Vector2(380f, 80f), mintColor, darkNavy, 30);
            modalUI.claimButton = claimBtnObj.GetComponent<Button>();
            modalUI.claimButtonText = claimBtnObj.GetComponentInChildren<Text>();
            modalUI.claimButton.onClick.AddListener(modalUI.OnClaimButtonClicked);

            // Kapat Butonu
            GameObject closeBtnObj = CreatePillButton(card.transform, "CloseButton", "KAPAT", new Vector2(0f, -295f), new Vector2(280f, 64f), new Color(0.15f, 0.20f, 0.35f, 0.9f), Color.white, 22);
            modalUI.closeButton = closeBtnObj.GetComponent<Button>();
            modalUI.closeButton.onClick.AddListener(modalUI.CloseModal);

            modal.SetActive(false);
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
