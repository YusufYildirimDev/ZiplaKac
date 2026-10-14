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
        /// Doğal orantılı Havada Uçan Siber Kuş (Bird)
        /// PPU: 200f -> Genişlik: 0.46f, Yükseklik: 0.32f
        /// </summary>
        public static Sprite CreateBirdSprite(Color color, int w = 92, int h = 64)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            ClearTexture(tex);

            // Ana gövde
            DrawRoundedRectOnto(tex, new Vector2(8, 8), new Vector2(w - 8, h - 16), 18f, color);

            // Kanat üstleri (Vurgulu)
            float cx = w * 0.48f;
            float wingBaseY = h - 22;
            for (int x = 12; x < w - 12; x++)
            {
                float dx = Mathf.Abs(x - cx);
                float wingTopY = wingBaseY + (1f - (dx / (w * 0.42f))) * 18f;
                for (int y = (int)wingBaseY; y <= (int)wingTopY; y++)
                {
                    if (x >= 0 && x < w && y >= 0 && y < h)
                    {
                        tex.SetPixel(x, y, Color.Lerp(color, Color.white, 0.35f));
                    }
                }
            }

            // Parlayan siber göz
            DrawFilledCircleAA(tex, (int)(w * 0.76f), (int)(h * 0.42f), 4, Color.cyan);
            DrawFilledCircleAA(tex, (int)(w * 0.77f), (int)(h * 0.43f), 1, Color.white);

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
