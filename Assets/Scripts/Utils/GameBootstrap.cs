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
            mainCam.backgroundColor = new Color(0.12f, 0.16f, 0.29f); // #1e2a4a (Modern Gece Mavisi)
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

        private void SetupGround()
        {
            GameObject ground = GameObject.Find("Ground");
            if (ground == null)
            {
                ground = new GameObject("Ground");
                ground.transform.position = new Vector3(0f, -4.5f, 0f);

                SpriteRenderer sr = ground.AddComponent<SpriteRenderer>();
                sr.sprite = ProceduralSpriteGenerator.CreateRectangleSprite(new Color(0.17f, 0.23f, 0.39f), 1024, 128); // #2b3a63 (Modern Zemin)
                sr.sortingOrder = 2;
                Material safeMat = GetSafeSpriteMaterial();
                if (safeMat != null) sr.material = safeMat;

                BoxCollider2D col = ground.AddComponent<BoxCollider2D>();
                col.size = new Vector2(40f, 2f); // Üst yüzey Y = -3.5f

                int groundLayer = LayerMask.NameToLayer("Ground");
                if (groundLayer != -1) ground.layer = groundLayer;
            }
        }

        private void SetupPlayer()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                player = new GameObject("Player");
                try { player.tag = "Player"; } catch { }

                float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
                float halfWidth = 5.0f * aspect;
                // Ekranın sol kenarından %22 içeride konumlandır
                float playerX = -halfWidth + (halfWidth * 2f) * 0.22f;
                player.transform.position = new Vector3(playerX, -2.5f, 0f);

                Material safeMat = GetSafeSpriteMaterial();

                // Gövde Görseli (Daire)
                SpriteRenderer bodySr = player.AddComponent<SpriteRenderer>();
                bodySr.sprite = ProceduralSpriteGenerator.CreatePlayerSprite(new Color(1.0f, 0.82f, 0.4f), new Color(1.0f, 0.91f, 0.66f, 0.8f), 64);
                bodySr.sortingOrder = 10;
                if (safeMat != null) bodySr.material = safeMat;

                // Parıltı (Glow) Alt Nesnesi
                GameObject glowObj = new GameObject("Glow");
                glowObj.transform.SetParent(player.transform, false);
                glowObj.transform.localScale = Vector3.one * 1.25f;
                SpriteRenderer glowSr = glowObj.AddComponent<SpriteRenderer>();
                glowSr.sprite = ProceduralSpriteGenerator.CreateCircleSprite(new Color(1f, 0.85f, 0.3f, 0.6f), 64);
                glowSr.sortingOrder = 9;
                if (safeMat != null) glowSr.material = safeMat;

                // Kalkan Halkası (Shield Ring) Alt Nesnesi
                GameObject shieldObj = new GameObject("ShieldRing");
                shieldObj.transform.SetParent(player.transform, false);
                shieldObj.transform.localScale = Vector3.one * 1.5f;
                SpriteRenderer shieldSr = shieldObj.AddComponent<SpriteRenderer>();
                shieldSr.sprite = ProceduralSpriteGenerator.CreateRingSprite(Color.cyan, 64, 0.8f);
                shieldSr.sortingOrder = 11;
                if (safeMat != null) shieldSr.material = safeMat;
                shieldObj.SetActive(false);

                // Zemin Kontrol Noktası (GroundCheck)
                GameObject groundCheckObj = new GameObject("GroundCheck");
                groundCheckObj.transform.SetParent(player.transform, false);
                groundCheckObj.transform.localPosition = new Vector3(0f, -0.45f, 0f);

                // Fizik & Kontrolcü
                CircleCollider2D col = player.AddComponent<CircleCollider2D>();
                col.radius = 0.45f;

                Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
                rb.gravityScale = 3.5f;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                PlayerController pc = player.AddComponent<PlayerController>();
                pc.fixedXPosition = playerX;
                pc.bodySpriteRenderer = bodySr;
                pc.glowSpriteRenderer = glowSr;
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

            // Power-up Prefabları
            GameObject pShield = CreatePowerUpPrefab("PowerUp_Shield", PowerUpType.Shield, Color.cyan, "S");
            ObjectPooler.Instance.RegisterPool("PowerUp_Shield", pShield, 3);

            GameObject pSlow = CreatePowerUpPrefab("PowerUp_Slow", PowerUpType.Slow, Color.blue, "T");
            ObjectPooler.Instance.RegisterPool("PowerUp_Slow", pSlow, 3);

            GameObject pMulti = CreatePowerUpPrefab("PowerUp_Multi", PowerUpType.ScoreMultiplier, Color.yellow, "2X");
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
                col.size = new Vector2(0.55f, 0.85f);
                col.offset = new Vector2(0f, 0.45f);
            }
            else if (name == "Rock")
            {
                sr.sprite = ProceduralSpriteGenerator.CreateRockSprite(obsColor);
                col.size = new Vector2(0.75f, 0.50f);
                col.offset = new Vector2(0f, 0.25f);
            }
            else if (name == "Spike")
            {
                sr.sprite = ProceduralSpriteGenerator.CreateSpikeSprite(obsColor);
                col.size = new Vector2(0.50f, 1.0f);
                col.offset = new Vector2(0f, 0.52f);
            }
            else if (name.Contains("Bird"))
            {
                sr.sprite = ProceduralSpriteGenerator.CreateBirdSprite(airColor);
                col.size = new Vector2(0.75f, 0.50f);
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
            sr.sprite = ProceduralSpriteGenerator.CreateCircleSprite(color, 64);
            sr.sortingOrder = 7;
            Material safeMat = GetSafeSpriteMaterial();
            if (safeMat != null) sr.material = safeMat;

            CircleCollider2D col = obj.AddComponent<CircleCollider2D>();
            col.radius = 0.35f;
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

        private void CreateMainMenuPanel(Transform parent)
        {
            GameObject panel = new GameObject("MainMenuPanel");
            panel.transform.SetParent(parent, false);
            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.4f);

            // Ekrana herhangi bir yere dokunarak da başlatma desteği
            Button panelBtn = panel.AddComponent<Button>();
            panelBtn.transition = Selectable.Transition.None;
            panelBtn.onClick.AddListener(() => {
                if (GameManager.Instance != null && GameManager.Instance.State == GameState.MainMenu)
                {
                    GameManager.Instance.StartGame();
                }
            });

            // Başlık
            CreateText(panel.transform, "TitleText", "ZIPLA KAÇ", 64, new Vector2(0f, 600f), Color.yellow);

            // Başla Butonu
            GameObject btnObj = CreateButton(panel.transform, "StartButton", "BAŞLA", new Vector2(0f, -500f), new Vector2(360f, 120f));
            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => {
                if (GameManager.Instance != null) GameManager.Instance.StartGame();
            });

            // Karakter Galerisi UI
            GameObject galleryObj = new GameObject("CharacterGallery");
            galleryObj.transform.SetParent(panel.transform, false);
            RectTransform galleryRt = galleryObj.AddComponent<RectTransform>();
            galleryRt.anchoredPosition = new Vector2(0f, 50f);
            galleryRt.sizeDelta = new Vector2(800f, 400f);

            CharacterSelectorUI csUI = galleryObj.AddComponent<CharacterSelectorUI>();

            if (UIManager.Instance != null) UIManager.Instance.mainMenuPanel = panel;
        }

        private void CreateHUDPanel(Transform parent)
        {
            GameObject panel = new GameObject("HUDPanel");
            panel.transform.SetParent(parent, false);
            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            HUDController hud = panel.AddComponent<HUDController>();

            // Anlık Skor (Sol Üst)
            hud.currentScoreText = CreateText(panel.transform, "CurrentScoreText", "SKOR: 0", 40, new Vector2(-350f, 800f), Color.white);

            // En İyi Skor (Sağ Üst)
            hud.highScoreText = CreateText(panel.transform, "HighScoreText", "EN İYİ: 0", 40, new Vector2(350f, 800f), Color.yellow);

            // Kontrol İpucu (Alt)
            hud.controlTipText = CreateText(panel.transform, "ControlTipText", "Zıplamak İçin Dokun / Boşluk", 32, new Vector2(0f, -800f), Color.white);

            // Active PowerUp Badges
            GameObject badgeBar = new GameObject("PowerUpBadgeBar");
            badgeBar.transform.SetParent(panel.transform, false);
            RectTransform barRt = badgeBar.AddComponent<RectTransform>();
            barRt.anchoredPosition = new Vector2(0f, 800f);

            hud.shieldBadgeText = CreateText(badgeBar.transform, "ShieldBadge", "KALKAN", 28, new Vector2(-200f, 0f), Color.cyan);
            hud.shieldBadgeObject = hud.shieldBadgeText.gameObject;
            hud.shieldBadgeObject.SetActive(false);

            hud.slowBadgeText = CreateText(badgeBar.transform, "SlowBadge", "YAVAŞ", 28, new Vector2(0f, 0f), Color.blue);
            hud.slowBadgeObject = hud.slowBadgeText.gameObject;
            hud.slowBadgeObject.SetActive(false);

            hud.multiBadgeText = CreateText(badgeBar.transform, "MultiBadge", "x2 PUAN", 28, new Vector2(200f, 0f), Color.yellow);
            hud.multiBadgeObject = hud.multiBadgeText.gameObject;
            hud.multiBadgeObject.SetActive(false);

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
            bg.color = new Color(0.1f, 0.05f, 0.05f, 0.85f);

            GameOverUI goUI = panel.AddComponent<GameOverUI>();

            // Başlık
            CreateText(panel.transform, "GameOverTitle", "OYUN BİTTİ", 54, new Vector2(0f, 650f), Color.red);

            // Final Skor
            goUI.finalScoreText = CreateText(panel.transform, "FinalScoreText", "0", 80, new Vector2(0f, 450f), Color.white);

            // En İyi Skor
            goUI.highScoreText = CreateText(panel.transform, "HighScoreText", "EN İYİ SKOR: 0", 36, new Vector2(0f, 320f), Color.yellow);

            // Yeni Rekor Rozeti
            Text newRecText = CreateText(panel.transform, "NewRecordBadge", "★ YENİ REKOR! ★", 40, new Vector2(0f, 220f), Color.green);
            goUI.newRecordBadge = newRecText.gameObject;
            goUI.newRecordBadge.SetActive(false);

            // Son 5 Skor Listesi
            goUI.scoreHistoryListText = CreateText(panel.transform, "HistoryListText", "", 28, new Vector2(0f, -50f), Color.white);

            // Tekrar Oyna Butonu
            GameObject restartBtn = CreateButton(panel.transform, "RestartButton", "TEKRAR OYNA", new Vector2(0f, -450f), new Vector2(360f, 100f));
            goUI.restartButton = restartBtn.GetComponent<Button>();

            // Ana Menü Butonu
            GameObject menuBtn = CreateButton(panel.transform, "MainMenuButton", "ANA MENÜ", new Vector2(0f, -600f), new Vector2(300f, 80f));
            goUI.mainMenuButton = menuBtn.GetComponent<Button>();

            panel.SetActive(false);
            if (UIManager.Instance != null) UIManager.Instance.gameOverPanel = panel;
        }

        private Text CreateText(Transform parent, string name, string content, int fontSize, Vector2 position, Color color, bool raycastTarget = false)
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
            txt.raycastTarget = raycastTarget;

            return txt;
        }

        private GameObject CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.6f, 1.0f);
            img.raycastTarget = true;

            Button btn = obj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.highlightedColor = new Color(0.4f, 0.8f, 1.0f);
            colors.pressedColor = new Color(0.1f, 0.4f, 0.8f);
            btn.colors = colors;

            Text labelTxt = CreateText(obj.transform, "Label", label, 32, Vector2.zero, Color.white, false);
            RectTransform labelRt = labelTxt.GetComponent<RectTransform>();
            labelRt.sizeDelta = size;

            return obj;
        }
    }
}
