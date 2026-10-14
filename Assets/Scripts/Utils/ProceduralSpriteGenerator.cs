using UnityEngine;

namespace ZiplaKac.Utils
{
    /// <summary>
    /// Modern HTML5 referansına (index.html) birebir sadık kalarak, yumuşak kenarlı (anti-aliased)
    /// sevimli küp karakter, kalkan halkası, kaktüs, sivri diken, kaya, kuş ve modern UI kartları (9-slice rounded rect) üreten sınıf.
    /// </summary>
    public static class ProceduralSpriteGenerator
    {
        /// <summary>
        /// index.html drawPlayer(): roundRect(x, y, w, h, 8) + shadowBlur: 14, shadowColor: glow + 2 sevimli göz
        /// </summary>
        public static Sprite CreatePlayerSprite(Color bodyColor, Color glowColor, int size = 96)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            ClearTexture(tex);

            float padding = size * 0.16f; // Işıma (glow) payı
            float rectW = size - padding * 2f;
            float rectH = size - padding * 2f;
            float cornerRadius = rectW * 0.24f; // 8px / 34px oranı

            Vector2 min = new Vector2(padding, padding);
            Vector2 max = new Vector2(size - padding, size - padding);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pt = new Vector2(x + 0.5f, y + 0.5f);
                    float d = SignedDistanceRoundedRect(pt, min, max, cornerRadius);

                    if (d <= 0f)
                    {
                        // Gövde içi
                        tex.SetPixel(x, y, bodyColor);
                    }
                    else if (d <= padding * 0.95f)
                    {
                        // Yumuşak dış ışıma (glow halo)
                        float t = d / (padding * 0.95f);
                        float alpha = Mathf.Pow(1f - t, 2.0f) * 0.85f;
                        tex.SetPixel(x, y, new Color(glowColor.r, glowColor.g, glowColor.b, alpha));
                    }
                }
            }

            // Sevimli Gözler (index.html: w*0.62, h*0.38 ve w*0.82, h*0.38)
            int eyeY = Mathf.RoundToInt(max.y - rectH * 0.38f);
            int eye1X = Mathf.RoundToInt(min.x + rectW * 0.62f);
            int eye2X = Mathf.RoundToInt(min.x + rectW * 0.82f);
            int eyeRadius = Mathf.Max(2, Mathf.RoundToInt(rectW * 0.08f));
            Color eyeColor = new Color(0.10f, 0.10f, 0.18f, 1f); // #1a1a2e

            DrawFilledCircleAA(tex, eye1X, eyeY, eyeRadius, eyeColor);
            DrawFilledCircleAA(tex, eye2X, eyeY, eyeRadius, eyeColor);

            // Göz parıltısı (Highlight)
            int hlOffset = Mathf.Max(1, eyeRadius / 2);
            tex.SetPixel(eye1X, eyeY + hlOffset, Color.white);
            tex.SetPixel(eye2X, eyeY + hlOffset, Color.white);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        }

        /// <summary>
        /// index.html drawObstacles(): roundRect stem (5px) + sol ve sağ dal
        /// </summary>
        public static Sprite CreateCactusSprite(Color color, int w = 64, int h = 96)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            ClearTexture(tex);

            float stemW = w * 0.44f;
            float cx = w * 0.5f;
            float cornerRadius = 6f;

            // 1. Ana Gövde (Stem)
            Vector2 stemMin = new Vector2(cx - stemW * 0.5f, 0f);
            Vector2 stemMax = new Vector2(cx + stemW * 0.5f, h);
            DrawRoundedRectOnto(tex, stemMin, stemMax, cornerRadius, color);

            // 2. Sol Kol (Left Branch)
            float leftY = h * 0.28f;
            float leftH = h * 0.38f;
            float armThick = stemW * 0.32f;
            DrawRoundedRectOnto(tex, new Vector2(cx - stemW * 0.5f - w * 0.22f, leftY), new Vector2(cx - stemW * 0.45f, leftY + armThick), 3f, color);
            DrawRoundedRectOnto(tex, new Vector2(cx - stemW * 0.5f - w * 0.22f, leftY), new Vector2(cx - stemW * 0.5f - w * 0.22f + armThick, leftY + leftH), 3f, color);

            // 3. Sağ Kol (Right Branch)
            float rightY = h * 0.44f;
            float rightH = h * 0.36f;
            DrawRoundedRectOnto(tex, new Vector2(cx + stemW * 0.45f, rightY), new Vector2(cx + stemW * 0.5f + w * 0.22f, rightY + armThick), 3f, color);
            DrawRoundedRectOnto(tex, new Vector2(cx + stemW * 0.5f + w * 0.22f - armThick, rightY), new Vector2(cx + stemW * 0.5f + w * 0.22f, rightY + rightH), 3f, color);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 64f);
        }

        /// <summary>
        /// index.html drawObstacles(): spiketall: roundRect taban (4px) + sivri tepe üçgeni
        /// </summary>
        public static Sprite CreateSpikeSprite(Color color, int w = 52, int h = 100)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            ClearTexture(tex);

            float baseH = h * 0.70f;

            // Alt taban yuvarlak dikdörtgen
            DrawRoundedRectOnto(tex, new Vector2(4, 0), new Vector2(w - 4, baseH), 6f, color);

            // Üst sivri üçgen
            for (int y = (int)baseH - 2; y < h; y++)
            {
                float t = (float)(y - (baseH - 2)) / Mathf.Max(1, (h - (baseH - 2)));
                float halfWidthAtY = (w * 0.5f - 4) * (1f - t);
                int startX = Mathf.RoundToInt(w * 0.5f - halfWidthAtY);
                int endX = Mathf.RoundToInt(w * 0.5f + halfWidthAtY);

                for (int x = startX; x <= endX; x++)
                {
                    if (x >= 0 && x < w && y >= 0 && y < h)
                    {
                        Color c = Color.Lerp(color, Color.white, t * 0.35f);
                        tex.SetPixel(x, y, c);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 64f);
        }

        /// <summary>
        /// index.html drawObstacles(): rock: roundRect(o.x, oy, o.w, o.h, 8)
        /// </summary>
        public static Sprite CreateRockSprite(Color color, int w = 72, int h = 48)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            ClearTexture(tex);

            DrawRoundedRectOnto(tex, new Vector2(3, 0), new Vector2(w - 3, h - 3), 12f, color);

            for (int x = 12; x < w - 12; x++)
            {
                int y = h - 6;
                tex.SetPixel(x, y, Color.Lerp(color, Color.white, 0.25f));
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 64f);
        }

        /// <summary>
        /// index.html drawObstacles(): bird: roundRect(10px) + kanatlar
        /// </summary>
        public static Sprite CreateBirdSprite(Color color, int w = 72, int h = 48)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            ClearTexture(tex);

            // Gövde
            DrawRoundedRectOnto(tex, new Vector2(6, 6), new Vector2(w - 6, h - 14), 14f, color);

            // Kanatlar
            float cx = w * 0.5f;
            float wingBaseY = h - 16;
            for (int x = 10; x < w - 10; x++)
            {
                float dx = Mathf.Abs(x - cx);
                float wingTopY = wingBaseY + (1f - (dx / (w * 0.4f))) * 14f;
                for (int y = (int)wingBaseY; y <= (int)wingTopY; y++)
                {
                    if (x >= 0 && x < w && y >= 0 && y < h)
                    {
                        tex.SetPixel(x, y, Color.Lerp(color, Color.white, 0.2f));
                    }
                }
            }

            DrawFilledCircleAA(tex, (int)(w * 0.75f), (int)(h * 0.45f), 3, Color.white);
            DrawFilledCircleAA(tex, (int)(w * 0.77f), (int)(h * 0.45f), 1, Color.black);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 64f);
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
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 64f, 0, SpriteMeshType.FullRect, border);
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
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 64f);
        }

        /// <summary>
        /// Zemin görseli (Üstünde parlak vurgu çizgisi olan zemin)
        /// </summary>
        public static Sprite CreateGroundSprite(Color groundColor, int w = 512, int h = 128)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color topStripe = Color.Lerp(groundColor, Color.white, 0.22f);

            for (int y = 0; y < h; y++)
            {
                Color c = (y >= h - 4) ? topStripe : groundColor;
                for (int x = 0; x < w; x++)
                {
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 64f);
        }

        public static Sprite CreateCircleSprite(Color color, int resolution = 64)
        {
            Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            ClearTexture(tex);
            float r = resolution * 0.44f;
            DrawFilledCircleAA(tex, resolution / 2, resolution / 2, (int)r, color);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), 64f);
        }

        /// <summary>
        /// index.html drawPowerUps(): Parlayan küre ve tepe parıltılı güçlendirme orbu
        /// </summary>
        public static Sprite CreatePowerUpOrb(Color color, string label = "", int resolution = 64)
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
                        float hl = (1f - d / (r * 0.45f)) * 0.35f;
                        tex.SetPixel(x, y, Color.Lerp(cur, Color.white, hl));
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), 64f);
        }

        public static Sprite CreateRingSprite(Color color, int resolution = 64, float innerRatio = 0.75f)
        {
            Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            ClearTexture(tex);
            float outerR = resolution * 0.48f;
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
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), 64f);
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
