using UnityEngine;

namespace ZiplaKac.Utils
{
    /// <summary>
    /// Modern HTML5 referansına (index.html) birebir sadık kalarak, yumuşak kenarlı (anti-aliased)
    /// sevimli küp karakter, kalkan halkası, kaktüs, sivri diken, kaya, kuş ve modern UI kartları (9-slice rounded rect) üreten sınıf.
    /// </summary>
    public static class ProceduralSpriteGenerator
    {
        public const float GAME_PPU = 200f;

        /// <summary>
        /// Resources/ klasöründeki gerçek sanat sprite'ını yükler; eğer yoksa prosedürel üreteci çağırır.
        /// </summary>
        public static Sprite GetSpriteOrGenerate(string resourcePath, System.Func<Sprite> fallbackGenerator)
        {
            try
            {
                Sprite s = Resources.Load<Sprite>(resourcePath);
                if (s != null) return s;
            }
            catch { }
            return fallbackGenerator?.Invoke();
        }

        /// <summary>
        /// Yüksek çözünürlüklü, sevimli (kawaii) küp kahraman avatarı:
        /// 3D jel/şeker degrade dolgu, tepe parlama çizgisi, parlak gözbebekleri, çift ışık yansıması ve pembe yanaklar.
        /// PPU: 200f -> Dünya boyutu: 0.52f x 0.52f
        /// </summary>
        public static Sprite CreatePlayerSprite(Color bodyColor, Color glowColor, int size = 128)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            ClearTexture(tex);

            float pad = 12f;
            Vector2 min = new Vector2(pad, pad);
            Vector2 max = new Vector2(size - pad, size - pad);
            float cornerRadius = 24f;

            Color topHighlight = Color.Lerp(bodyColor, Color.white, 0.35f);
            Color bottomShadow = Color.Lerp(bodyColor, new Color(0.08f, 0.08f, 0.15f), 0.28f);

            // 1. Gövde & Yumuşak Kenar
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pt = new Vector2(x + 0.5f, y + 0.5f);
                    float d = SignedDistanceRoundedRect(pt, min, max, cornerRadius);

                    if (d <= -2f)
                    {
                        // Dikey Degrade (Jel / Marshmallow efekti)
                        float vertT = Mathf.Clamp01((y - min.y) / (max.y - min.y));
                        Color c = Color.Lerp(bottomShadow, topHighlight, vertT);

                        // Tepe parlama yansıması (Glossy Specular)
                        if (y > max.y - 18f && x > min.x + 10f && x < max.x - 10f)
                        {
                            float glossT = (y - (max.y - 18f)) / 18f;
                            c = Color.Lerp(c, Color.white, glossT * 0.40f);
                        }

                        tex.SetPixel(x, y, c);
                    }
                    else if (d <= 0f)
                    {
                        // Kenar Çizgisi (İnce ve temiz)
                        float edgeT = (d + 2f) / 2f;
                        Color edgeCol = Color.Lerp(bottomShadow, topHighlight, (float)y / size);
                        tex.SetPixel(x, y, Color.Lerp(edgeCol, Color.white, 0.15f));
                    }
                    else if (d <= 2.5f)
                    {
                        // Çok hafif dış antialias
                        float alpha = (1f - (d / 2.5f)) * 0.45f;
                        tex.SetPixel(x, y, new Color(bodyColor.r, bodyColor.g, bodyColor.b, alpha));
                    }
                }
            }

            // 2. Canlı Sevimli Gözler (Büyük ve Parlak)
            int eyeY = Mathf.RoundToInt(min.y + (max.y - min.y) * 0.52f);
            int eye1X = Mathf.RoundToInt(min.x + (max.x - min.x) * 0.62f);
            int eye2X = Mathf.RoundToInt(min.x + (max.x - min.x) * 0.84f);
            int eyeRadius = 9;
            Color pupilColor = new Color(0.08f, 0.10f, 0.18f, 1f); // Koyu lacivert göz

            DrawFilledCircleAA(tex, eye1X, eyeY, eyeRadius, pupilColor);
            DrawFilledCircleAA(tex, eye2X, eyeY, eyeRadius, pupilColor);

            // Göz içi büyük ışıltı (Üst-sağ catchlight)
            DrawFilledCircleAA(tex, eye1X + 3, eyeY + 3, 3, Color.white);
            DrawFilledCircleAA(tex, eye2X + 3, eyeY + 3, 3, Color.white);

            // Göz içi küçük ışıltı (Alt-sol mini twinkle)
            DrawFilledCircleAA(tex, eye1X - 3, eyeY - 3, 1, new Color(1f, 1f, 1f, 0.8f));
            DrawFilledCircleAA(tex, eye2X - 3, eyeY - 3, 1, new Color(1f, 1f, 1f, 0.8f));

            // 3. Tatlı Yanaklar (Pastel Gül Pembesi Allık)
            Color blushColor = new Color(1.0f, 0.42f, 0.58f, 0.45f);
            DrawFilledCircleAA(tex, eye1X - 2, eyeY - 11, 6, blushColor);
            DrawFilledCircleAA(tex, eye2X + 2, eyeY - 11, 6, blushColor);

            // 4. Minik Tebessüm (Ağız)
            int mouthX = (eye1X + eye2X) / 2;
            int mouthY = eyeY - 9;
            tex.SetPixel(mouthX - 2, mouthY, pupilColor);
            tex.SetPixel(mouthX - 1, mouthY - 1, pupilColor);
            tex.SetPixel(mouthX, mouthY - 1, pupilColor);
            tex.SetPixel(mouthX + 1, mouthY - 1, pupilColor);
            tex.SetPixel(mouthX + 2, mouthY, pupilColor);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), GAME_PPU);
        }

        /// <summary>
        /// Karakter seçici galerisi (swatch) için minyatür yüz ikonu
        /// </summary>
        public static Sprite CreateMiniCharacterIcon(Color bodyColor, int size = 80)
        {
            return CreatePlayerSprite(bodyColor, bodyColor, size);
        }

        /// <summary>
        /// Doğal orantılı Kaktüs (Stem + 2 dal, tepe aydınlatması)
        /// PPU: 200f -> Genişlik: 0.40f, Yükseklik: 0.60f
        /// </summary>
        public static Sprite CreateCactusSprite(Color color, int w = 80, int h = 120)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            ClearTexture(tex);

            float stemW = 32f;
            float cx = w * 0.5f;
            float cornerRadius = 10f;

            Color topLight = Color.Lerp(color, Color.white, 0.28f);
            Color darkContour = Color.Lerp(color, Color.black, 0.20f);

            // 1. Ana Gövde (Stem)
            Vector2 stemMin = new Vector2(cx - stemW * 0.5f, 0f);
            Vector2 stemMax = new Vector2(cx + stemW * 0.5f, h);
            DrawRoundedRectOnto(tex, stemMin, stemMax, cornerRadius, color);

            // 2. Sol Dal
            float leftY = h * 0.30f;
            float leftH = h * 0.40f;
            float armThick = 18f;
            DrawRoundedRectOnto(tex, new Vector2(cx - stemW * 0.5f - 22f, leftY), new Vector2(cx - stemW * 0.4f, leftY + armThick), 6f, color);
            DrawRoundedRectOnto(tex, new Vector2(cx - stemW * 0.5f - 22f, leftY), new Vector2(cx - stemW * 0.5f - 22f + armThick, leftY + leftH), 6f, color);

            // 3. Sağ Dal
            float rightY = h * 0.45f;
            float rightH = h * 0.38f;
            DrawRoundedRectOnto(tex, new Vector2(cx + stemW * 0.4f, rightY), new Vector2(cx + stemW * 0.5f + 22f, rightY + armThick), 6f, color);
            DrawRoundedRectOnto(tex, new Vector2(cx + stemW * 0.5f + 22f - armThick, rightY), new Vector2(cx + stemW * 0.5f + 22f, rightY + rightH), 6f, color);

            // Tepe yüzey aydınlatmaları
            for (int x = (int)(cx - stemW * 0.35f); x <= (int)(cx + stemW * 0.35f); x++)
            {
                if (x >= 0 && x < w) tex.SetPixel(x, h - 3, topLight);
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), GAME_PPU);
        }

        /// <summary>
        /// Doğal orantılı Sivri Kristal Diken (Spike)
        /// PPU: 200f -> Genişlik: 0.34f, Yükseklik: 0.80f
        /// </summary>
        public static Sprite CreateSpikeSprite(Color color, int w = 68, int h = 160)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            ClearTexture(tex);

            float baseH = h * 0.35f;

            // Alt taban yuvarlak dikdörtgen
            DrawRoundedRectOnto(tex, new Vector2(6, 0), new Vector2(w - 6, baseH), 8f, color);

            // Üst sivri üçgen kule
            for (int y = (int)baseH - 2; y < h; y++)
            {
                float t = (float)(y - (baseH - 2)) / Mathf.Max(1, (h - (baseH - 2)));
                float halfWidthAtY = (w * 0.5f - 6) * (1f - t);
                int startX = Mathf.RoundToInt(w * 0.5f - halfWidthAtY);
                int endX = Mathf.RoundToInt(w * 0.5f + halfWidthAtY);

                for (int x = startX; x <= endX; x++)
                {
                    if (x >= 0 && x < w && y >= 0 && y < h)
                    {
                        // Orta dikey parlama omurgası
                        float distFromCenter = Mathf.Abs(x - w * 0.5f);
                        Color c = Color.Lerp(color, Color.white, (1f - (distFromCenter / Mathf.Max(1f, halfWidthAtY))) * 0.45f);
                        tex.SetPixel(x, y, c);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), GAME_PPU);
        }

        /// <summary>
        /// Doğal orantılı Kaya (Boulder)
        /// PPU: 200f -> Genişlik: 0.55f, Yükseklik: 0.35f
        /// </summary>
        public static Sprite CreateRockSprite(Color color, int w = 110, int h = 70)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            ClearTexture(tex);

            DrawRoundedRectOnto(tex, new Vector2(4, 0), new Vector2(w - 4, h - 4), 18f, color);

            // Üst parlak kaya faseti
            Color facetLight = Color.Lerp(color, Color.white, 0.30f);
            for (int x = 20; x < w - 20; x++)
            {
                for (int y = h - 14; y < h - 5; y++)
                {
                    if (tex.GetPixel(x, y).a > 0.5f)
                    {
                        tex.SetPixel(x, y, facetLight);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), GAME_PPU);
        }

        /// <summary>
        /// Yüksek görünürlüklü ve belirgin Havada Uçan Siber Kuş (Bird)
        /// PPU: 200f -> Genişlik: 0.90f, Yükseklik: 0.48f
        /// </summary>
        public static Sprite CreateBirdSprite(Color color, int w = 180, int h = 96)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            ClearTexture(tex);

            Color goldGlow = new Color(1.0f, 0.84f, 0.35f);
            Color darkContour = Color.Lerp(color, Color.black, 0.35f);

            // 1. Kanatlar (Geniş aerodinamik yay)
            float cx = w * 0.45f;
            for (int x = 6; x < w - 16; x++)
            {
                float dx = (x - cx);
                float wingCurve = Mathf.Cos(dx / (w * 0.46f) * Mathf.PI * 0.5f);
                float wingTop = h * 0.45f + wingCurve * (h * 0.48f);
                float wingBottom = wingTop - (14f + wingCurve * 18f);

                for (int y = Mathf.Max(0, (int)wingBottom); y <= Mathf.Min(h - 1, (int)wingTop); y++)
                {
                    // Uçlara doğru altın sarısı parlama
                    float tipT = Mathf.Clamp01((y - wingBottom) / Mathf.Max(1f, wingTop - wingBottom));
                    Color c = (tipT > 0.70f) ? Color.Lerp(color, goldGlow, (tipT - 0.70f) / 0.30f) : color;
                    tex.SetPixel(x, y, c);
                }
            }

            // 2. Ana Gövde (Ortalanmış ve Tok)
            DrawRoundedRectOnto(tex, new Vector2(cx - 36, h * 0.20f), new Vector2(cx + 36, h * 0.78f), 22f, color);

            // 3. Keskin Gagası (Altın Sarısı #ffd166)
            int beakStartX = (int)(cx - 32);
            int beakLen = 22;
            int beakCenterY = (int)(h * 0.44f);
            for (int bx = 0; bx < beakLen; bx++)
            {
                int x = beakStartX - bx;
                int halfThick = (int)((1f - (float)bx / beakLen) * 7f);
                for (int y = beakCenterY - halfThick; y <= beakCenterY + halfThick; y++)
                {
                    if (x >= 0 && x < w && y >= 0 && y < h)
                    {
                        tex.SetPixel(x, y, goldGlow);
                    }
                }
            }

            // 4. Parlayan Menacing Siber Göz (Cyan & Beyaz Işıltı)
            int eyeX = (int)(cx - 14);
            int eyeY = (int)(h * 0.54f);
            DrawFilledCircleAA(tex, eyeX, eyeY, 6, Color.cyan);
            DrawFilledCircleAA(tex, eyeX, eyeY, 3, Color.white);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), GAME_PPU);
        }

        /// <summary>
        /// Modern Glassmorphic UI Kartı & Hap Butonlar için 9-slice uyumlu yuvarlatılmış dikdörtgen sprite üretir.
        /// </summary>
        public static Sprite CreateRoundedRectSprite(Color fillColor, Color borderColor, float borderWidth = 2f, int cornerRadius = 24, int w = 128, int h = 128)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            ClearTexture(tex);

            Vector2 min = Vector2.zero;
            Vector2 max = new Vector2(w, h);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Vector2 pt = new Vector2(x + 0.5f, y + 0.5f);
                    float d = SignedDistanceRoundedRect(pt, min, max, cornerRadius);

                    if (d <= -borderWidth)
                    {
                        tex.SetPixel(x, y, fillColor);
                    }
                    else if (d <= 0f)
                    {
                        float t = (d + borderWidth) / Mathf.Max(0.1f, borderWidth);
                        Color c = Color.Lerp(fillColor, borderColor, t);
                        tex.SetPixel(x, y, c);
                    }
                    else if (d <= 1.0f)
                    {
                        float alpha = (1.0f - d) * borderColor.a;
                        tex.SetPixel(x, y, new Color(borderColor.r, borderColor.g, borderColor.b, alpha));
                    }
                }
            }

            tex.Apply();
            Vector4 border = new Vector4(cornerRadius + 4, cornerRadius + 4, cornerRadius + 4, cornerRadius + 4);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        /// <summary>
        /// İki renkli dikey gökyüzü degrade (gradient sky) sprite'ı üretir.
        /// </summary>
        public static Sprite CreateGradientSkySprite(Color topColor, Color bottomColor, int w = 64, int h = 256)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / (h - 1);
                Color c = Color.Lerp(bottomColor, topColor, t);
                for (int x = 0; x < w; x++)
                {
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>
        /// Zemin görseli (Zemin ile gökyüzü arasında yapay/keskin sınır oluşturmayan,
        /// titreşimsiz, ızgarasız ve arka plan ile organik bütünleşen derin mat zemin).
        /// PPU: 100f -> Yükseklik: 4.0f, Genişlik: 5.12f, Pivot: (0.5f, 1.0f) Üst Kenar
        /// </summary>
        public static Sprite CreateGroundSprite(Color groundColor, int w = 512, int h = 400)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color topEdge = Color.Lerp(groundColor, Color.white, 0.10f);
            Color deepBase = Color.Lerp(new Color(0.04f, 0.05f, 0.09f), groundColor, 0.40f);

            for (int y = 0; y < h; y++)
            {
                Color c;
                if (y >= h - 2)
                {
                    // Oyuncunun bastığı yüzey için çok hafif, sakin temas çizgisi
                    c = topEdge;
                }
                else
                {
                    float t = (float)y / (h - 2);
                    c = Color.Lerp(deepBase, groundColor, t);
                }

                for (int x = 0; x < w; x++)
                {
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 1.0f), 100f);
        }

        public static Sprite CreateCircleSprite(Color color, int resolution = 64)
        {
            Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            ClearTexture(tex);
            float r = resolution * 0.44f;
            DrawFilledCircleAA(tex, resolution / 2, resolution / 2, (int)r, color);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), GAME_PPU);
        }

        /// <summary>
        /// index.html drawPowerUps(): Parlayan küre ve tepe parıltılı güçlendirme orbu
        /// PPU: 200f -> Çap: 0.44f
        /// </summary>
        public static Sprite CreatePowerUpOrb(Color color, string label = "", int resolution = 88)
        {
            Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            ClearTexture(tex);

            int cx = resolution / 2;
            int cy = resolution / 2;
            float r = resolution * 0.40f;

            // Dış yumuşak parıltı halesi (Glow aura)
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    if (d <= resolution * 0.48f)
                    {
                        float alpha = Mathf.Clamp01(1f - (d / (resolution * 0.48f))) * 0.45f;
                        tex.SetPixel(x, y, new Color(color.r, color.g, color.b, alpha));
                    }
                }
            }

            // Ana iç küre (Filled AA Circle)
            DrawFilledCircleAA(tex, cx, cy, (int)r, color);

            // İç tepe parlaklığı (Highlight reflection)
            for (int y = cy; y < cy + (int)(r * 0.8f); y++)
            {
                for (int x = cx - (int)(r * 0.6f); x <= cx + (int)(r * 0.6f); x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy + r * 0.35f));
                    if (d < r * 0.45f)
                    {
                        Color cur = tex.GetPixel(x, y);
                        float hl = (1f - d / (r * 0.45f)) * 0.40f;
                        tex.SetPixel(x, y, Color.Lerp(cur, Color.white, hl));
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), GAME_PPU);
        }

        public static Sprite CreateRingSprite(Color color, int resolution = 128, float innerRatio = 0.82f)
        {
            Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            ClearTexture(tex);
            float outerR = resolution * 0.46f;
            float innerR = outerR * innerRatio;
            Vector2 center = new Vector2(resolution * 0.5f, resolution * 0.5f);

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    if (d >= innerR && d <= outerR)
                    {
                        tex.SetPixel(x, y, color);
                    }
                    else if (d >= innerR - 1.5f && d <= outerR + 1.5f)
                    {
                        float alpha = color.a * 0.4f;
                        tex.SetPixel(x, y, new Color(color.r, color.g, color.b, alpha));
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), GAME_PPU);
        }

        public static Sprite CreateRectangleSprite(Color color, int width = 64, int height = 64)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++) tex.SetPixel(x, y, color);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 64f);
        }

        public static Sprite CreateTriangleSprite(Color color, int width = 64, int height = 96)
        {
            return CreateSpikeSprite(color, width, height);
        }

        public static Sprite CreateGoldCoinSprite(int resolution = 128)
        {
            Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            ClearTexture(tex);

            Vector2 center = new Vector2(resolution * 0.5f, resolution * 0.5f);
            float outerR = resolution * 0.46f;
            float rimR = resolution * 0.39f;
            float innerR = resolution * 0.34f;

            Color rimDark = new Color(0.68f, 0.42f, 0.04f, 1f);     // Derin bronz altın kenar
            Color goldTop = new Color(1.00f, 0.92f, 0.48f, 1f);     // Parlak tepe altın
            Color goldBottom = new Color(0.92f, 0.65f, 0.08f, 1f);  // Zengin alt altın
            Color innerRimCol = new Color(1.00f, 0.84f, 0.25f, 1f); // İç kabartma halka
            Color starCol = new Color(1.00f, 0.98f, 0.80f, 1f);     // Merkez yıldız / ışıltı

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    Vector2 pt = new Vector2(x + 0.5f, y + 0.5f);
                    float d = Vector2.Distance(pt, center);

                    if (d <= outerR)
                    {
                        if (d > rimR)
                        {
                            float edgeT = (d - rimR) / (outerR - rimR);
                            tex.SetPixel(x, y, Color.Lerp(goldBottom, rimDark, edgeT));
                        }
                        else if (d > innerR)
                        {
                            tex.SetPixel(x, y, innerRimCol);
                        }
                        else
                        {
                            float vertT = Mathf.Clamp01((y - (center.y - innerR)) / (innerR * 2f));
                            Color baseC = Color.Lerp(goldBottom, goldTop, vertT);

                            float dx = Mathf.Abs(x - center.x);
                            float dy = Mathf.Abs(y - center.y);
                            if ((dx + dy * 1.5f < 16f) || (dx * 1.5f + dy < 16f) || (dx < 6f && dy < 6f))
                            {
                                baseC = Color.Lerp(baseC, starCol, 0.75f);
                            }

                            if (pt.y > center.y + 4f && pt.x < center.x - 2f)
                            {
                                float glossD = Vector2.Distance(pt, new Vector2(center.x - 14f, center.y + 14f));
                                if (glossD < 18f)
                                {
                                    baseC = Color.Lerp(baseC, Color.white, (1f - glossD / 18f) * 0.45f);
                                }
                            }

                            tex.SetPixel(x, y, baseC);
                        }
                    }
                    else if (d <= outerR + 1.5f)
                    {
                        float a = (1f - (d - outerR) / 1.5f);
                        tex.SetPixel(x, y, new Color(rimDark.r, rimDark.g, rimDark.b, a));
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), 100f);
        }

        public static Sprite CreateTrophyIconSprite(int resolution = 128)
        {
            Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            ClearTexture(tex);

            Vector2 center = new Vector2(resolution * 0.5f, resolution * 0.5f);
            Color goldTop = new Color(1.0f, 0.92f, 0.45f, 1f);
            Color goldMid = new Color(0.96f, 0.72f, 0.12f, 1f);
            Color goldDark = new Color(0.70f, 0.45f, 0.05f, 1f);

            float r = resolution * 0.45f;
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    Vector2 pt = new Vector2(x + 0.5f, y + 0.5f);
                    float d = Vector2.Distance(pt, center);
                    if (d <= r)
                    {
                        float vertT = Mathf.Clamp01((y - (center.y - r)) / (r * 2f));
                        Color c = Color.Lerp(new Color(0.18f, 0.12f, 0.32f, 0.95f), new Color(0.28f, 0.18f, 0.48f, 0.95f), vertT);
                        if (d > r - 4f) c = Color.Lerp(c, goldTop, 0.9f);
                        tex.SetPixel(x, y, c);
                    }
                    else if (d <= r + 1.5f)
                    {
                        float a = 1f - (d - r) / 1.5f;
                        tex.SetPixel(x, y, new Color(goldTop.r, goldTop.g, goldTop.b, a * 0.6f));
                    }
                }
            }

            for (int y = 24; y <= 98; y++)
            {
                for (int x = 24; x <= 104; x++)
                {
                    float relX = x - center.x;
                    float relY = y - center.y;

                    bool inCup = (relY >= -6f && relY <= 30f && Mathf.Abs(relX) <= (14f + (relY + 6f) * 0.45f));
                    bool inStem = (relY >= -22f && relY < -6f && Mathf.Abs(relX) <= 6f);
                    bool inBase = (relY >= -32f && relY < -22f && Mathf.Abs(relX) <= (22f - (relY + 32f) * 0.6f));
                    bool inHandleL = (relY >= 4f && relY <= 26f && relX >= -34f && relX <= -16f && (Mathf.Abs(relX - (-25f)) > 3f || relY > 23f || relY < 7f));
                    bool inHandleR = (relY >= 4f && relY <= 26f && relX >= 16f && relX <= 34f && (Mathf.Abs(relX - 25f) > 3f || relY > 23f || relY < 7f));

                    if (inCup || inStem || inBase || inHandleL || inHandleR)
                    {
                        float cupT = Mathf.Clamp01((y - 24f) / 74f);
                        Color c = Color.Lerp(goldDark, goldTop, cupT);
                        if (relX < -2f && inCup) c = Color.Lerp(c, Color.white, 0.35f);
                        tex.SetPixel(x, y, c);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), 100f);
        }

        public static Sprite CreateScoreStarSprite(int resolution = 128)
        {
            Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            ClearTexture(tex);

            Vector2 center = new Vector2(resolution * 0.5f, resolution * 0.5f);
            Color mintTop = new Color(0.40f, 0.95f, 0.80f, 1f);
            Color mintMid = new Color(0.024f, 0.839f, 0.627f, 1f);
            Color mintDark = new Color(0.01f, 0.45f, 0.35f, 1f);

            float r = resolution * 0.45f;
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    Vector2 pt = new Vector2(x + 0.5f, y + 0.5f);
                    float d = Vector2.Distance(pt, center);
                    if (d <= r)
                    {
                        float vertT = Mathf.Clamp01((y - (center.y - r)) / (r * 2f));
                        Color c = Color.Lerp(new Color(0.05f, 0.15f, 0.22f, 0.95f), new Color(0.08f, 0.24f, 0.32f, 0.95f), vertT);
                        if (d > r - 4f) c = Color.Lerp(c, mintTop, 0.9f);
                        tex.SetPixel(x, y, c);
                    }
                    else if (d <= r + 1.5f)
                    {
                        float a = 1f - (d - r) / 1.5f;
                        tex.SetPixel(x, y, new Color(mintTop.r, mintTop.g, mintTop.b, a * 0.6f));
                    }
                }
            }

            for (int y = 28; y <= 100; y++)
            {
                for (int x = 28; x <= 100; x++)
                {
                    float dx = Mathf.Abs(x - center.x);
                    float dy = Mathf.Abs(y - center.y);
                    if ((dx + dy * 1.3f < 28f) || (dx * 1.3f + dy < 28f) || (dx < 12f && dy < 12f))
                    {
                        float t = 1f - Mathf.Max(dx, dy) / 28f;
                        Color c = Color.Lerp(mintMid, Color.white, t);
                        tex.SetPixel(x, y, c);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>
        /// Kilitli karakterler için yüksek kaliteli metalik/parlak asma kilit (padlock) ikonu üretir.
        /// </summary>
        public static Sprite CreateLockIconSprite(int resolution = 128)
        {
            Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            ClearTexture(tex);

            Vector2 center = new Vector2(resolution * 0.5f, resolution * 0.5f);

            // 1. Kilit halkası (Shackle - U şeklinde kavis ve bacaklar)
            float shackleCenterX = center.x;
            float shackleCenterY = resolution * 0.60f;
            float shackleOuterR = resolution * 0.22f;
            float shackleInnerR = resolution * 0.13f;
            float shackleBottomY = resolution * 0.42f;

            Color shackleCol = new Color(0.95f, 0.96f, 0.98f, 1f);
            Color shackleShadow = new Color(0.62f, 0.66f, 0.74f, 1f);

            for (int y = Mathf.FloorToInt(shackleBottomY); y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    Vector2 pt = new Vector2(x + 0.5f, y + 0.5f);
                    float dx = Mathf.Abs(pt.x - shackleCenterX);

                    bool inShackle = false;
                    float distToCenter = Vector2.Distance(pt, new Vector2(shackleCenterX, shackleCenterY));
                    if (pt.y >= shackleCenterY)
                    {
                        if (distToCenter <= shackleOuterR && distToCenter >= shackleInnerR) inShackle = true;
                    }
                    else
                    {
                        if (dx <= shackleOuterR && dx >= shackleInnerR && pt.y >= shackleBottomY) inShackle = true;
                    }

                    if (inShackle)
                    {
                        float t = (pt.x - (shackleCenterX - shackleOuterR)) / (shackleOuterR * 2f);
                        Color c = Color.Lerp(shackleShadow, shackleCol, Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI));
                        tex.SetPixel(x, y, c);
                    }
                }
            }

            // 2. Kilit gövdesi (Rounded Rect)
            Vector2 bodyMin = new Vector2(resolution * 0.20f, resolution * 0.12f);
            Vector2 bodyMax = new Vector2(resolution * 0.80f, resolution * 0.52f);
            float cornerR = resolution * 0.08f;

            Color bodyGoldTop = new Color(1.0f, 0.86f, 0.35f, 1f);
            Color bodyGoldBottom = new Color(0.88f, 0.58f, 0.08f, 1f);
            Color bodyBorder = new Color(0.68f, 0.40f, 0.04f, 1f);

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    Vector2 pt = new Vector2(x + 0.5f, y + 0.5f);
                    float d = SignedDistanceRoundedRect(pt, bodyMin, bodyMax, cornerR);
                    if (d <= 0f)
                    {
                        float vertT = (pt.y - bodyMin.y) / (bodyMax.y - bodyMin.y);
                        Color c = Color.Lerp(bodyGoldBottom, bodyGoldTop, vertT);

                        if (d > -2.5f) c = Color.Lerp(c, bodyBorder, 0.75f);
                        tex.SetPixel(x, y, c);
                    }
                    else if (d <= 1.2f)
                    {
                        float a = (1f - d / 1.2f);
                        tex.SetPixel(x, y, new Color(bodyBorder.r, bodyBorder.g, bodyBorder.b, a));
                    }
                }
            }

            // 3. Anahtar deliği (Keyhole)
            Vector2 keyholeCenter = new Vector2(shackleCenterX, resolution * 0.35f);
            float keyHoleCircleR = resolution * 0.055f;
            Color keyholeCol = new Color(0.14f, 0.08f, 0.04f, 0.95f);

            for (int y = Mathf.FloorToInt(resolution * 0.20f); y <= Mathf.CeilToInt(keyholeCenter.y + keyHoleCircleR + 1f); y++)
            {
                for (int x = Mathf.FloorToInt(shackleCenterX - keyHoleCircleR - 2f); x <= Mathf.CeilToInt(shackleCenterX + keyHoleCircleR + 2f); x++)
                {
                    Vector2 pt = new Vector2(x + 0.5f, y + 0.5f);
                    float dCircle = Vector2.Distance(pt, keyholeCenter);
                    bool isHole = false;
                    if (dCircle <= keyHoleCircleR) isHole = true;

                    float dx = Mathf.Abs(pt.x - shackleCenterX);
                    if (pt.y <= keyholeCenter.y && pt.y >= resolution * 0.20f)
                    {
                        float halfW = Mathf.Lerp(resolution * 0.04f, resolution * 0.022f, (pt.y - resolution * 0.20f) / (keyholeCenter.y - resolution * 0.20f));
                        if (dx <= halfW) isHole = true;
                    }

                    if (isHole)
                    {
                        tex.SetPixel(x, y, keyholeCol);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), 100f);
        }

        #region Helper Matematik Fonksiyonları
        private static void ClearTexture(Texture2D tex)
        {
            Color[] cols = new Color[tex.width * tex.height];
            for (int i = 0; i < cols.Length; i++) cols[i] = Color.clear;
            tex.SetPixels(cols);
        }

        private static float SignedDistanceRoundedRect(Vector2 p, Vector2 min, Vector2 max, float r)
        {
            Vector2 center = (min + max) * 0.5f;
            Vector2 halfSize = (max - min) * 0.5f - new Vector2(r, r);
            Vector2 d = new Vector2(Mathf.Abs(p.x - center.x), Mathf.Abs(p.y - center.y)) - halfSize;
            return Mathf.Min(Mathf.Max(d.x, d.y), 0f) + Vector2.Max(d, Vector2.zero).magnitude - r;
        }

        private static void DrawRoundedRectOnto(Texture2D tex, Vector2 min, Vector2 max, float r, Color color)
        {
            int startX = Mathf.Clamp(Mathf.FloorToInt(min.x), 0, tex.width - 1);
            int endX = Mathf.Clamp(Mathf.CeilToInt(max.x), 0, tex.width - 1);
            int startY = Mathf.Clamp(Mathf.FloorToInt(min.y), 0, tex.height - 1);
            int endY = Mathf.Clamp(Mathf.CeilToInt(max.y), 0, tex.height - 1);

            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    Vector2 pt = new Vector2(x + 0.5f, y + 0.5f);
                    float d = SignedDistanceRoundedRect(pt, min, max, r);
                    if (d <= 0f)
                    {
                        tex.SetPixel(x, y, color);
                    }
                    else if (d <= 1f)
                    {
                        float a = (1f - d) * color.a;
                        tex.SetPixel(x, y, new Color(color.r, color.g, color.b, a));
                    }
                }
            }
        }

        private static void DrawFilledCircleAA(Texture2D tex, int cx, int cy, int r, Color col)
        {
            for (int y = cy - r - 1; y <= cy + r + 1; y++)
            {
                for (int x = cx - r - 1; x <= cx + r + 1; x++)
                {
                    if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                        if (d <= r)
                        {
                            tex.SetPixel(x, y, col);
                        }
                        else if (d <= r + 1f)
                        {
                            float a = (1f - (d - r)) * col.a;
                            tex.SetPixel(x, y, new Color(col.r, col.g, col.b, a));
                        }
                    }
                }
            }
        }
        #endregion
    }
}
