using UnityEngine;

namespace ZiplaKac.Utils
{
    /// <summary>
    /// Modern, stilize ve detaylı 2D oyun sprite'larını çalışma zamanında oluşturan prosedürel görsel üretici.
    /// Sevimli karakter yüzü, kollu kaktüs, sivri dikenler, uçan kuşlar ve parlayan güçlendirmeler içerir.
    /// </summary>
    public static class ProceduralSpriteGenerator
    {
        public static Sprite CreatePlayerSprite(Color bodyColor, Color glowColor, int size = 64)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = size * 0.42f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pt = new Vector2(x, y);
                    float d = Vector2.Distance(pt, center);

                    if (d <= radius)
                    {
                        // Ana gövde
                        tex.SetPixel(x, y, bodyColor);
                    }
                    else if (d <= radius + 3)
                    {
                        // Yumuşak parıltı kenarlığı
                        float a = 1f - (d - radius) / 3f;
                        tex.SetPixel(x, y, new Color(glowColor.r, glowColor.g, glowColor.b, a * 0.6f));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            // Sevimli Gözler (Sağa bakan iki tatlı nokta)
            int eyeY = (int)(size * 0.55f);
            int eye1X = (int)(size * 0.62f);
            int eye2X = (int)(size * 0.82f);
            Color eyeColor = new Color(0.1f, 0.1f, 0.18f, 1f);

            DrawFilledCircle(tex, eye1X, eyeY, 3, eyeColor);
            DrawFilledCircle(tex, eye2X, eyeY, 3, eyeColor);

            // Göz Parıltısı (Highlight)
            tex.SetPixel(eye1X, eyeY + 1, Color.white);
            tex.SetPixel(eye2X, eyeY + 1, Color.white);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        }

        public static Sprite CreateCactusSprite(Color color, int w = 48, int h = 72)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            ClearTexture(tex);

            int cx = w / 2;
            int stemWidth = 12;

            // Ana gövde sütunu
            for (int y = 0; y < h; y++)
            {
                for (int x = cx - stemWidth / 2; x <= cx + stemWidth / 2; x++)
                {
                    tex.SetPixel(x, y, color);
                }
            }

            // Sol kol
            int arm1Y = (int)(h * 0.4f);
            for (int x = cx - stemWidth / 2 - 10; x <= cx - stemWidth / 2; x++)
            {
                for (int y = arm1Y; y < arm1Y + 8; y++) tex.SetPixel(x, y, color);
            }
            for (int x = cx - stemWidth / 2 - 10; x < cx - stemWidth / 2 - 4; x++)
            {
                for (int y = arm1Y; y < arm1Y + 22; y++) tex.SetPixel(x, y, color);
            }

            // Sağ kol
            int arm2Y = (int)(h * 0.55f);
            for (int x = cx + stemWidth / 2; x <= cx + stemWidth / 2 + 10; x++)
            {
                for (int y = arm2Y; y < arm2Y + 8; y++) tex.SetPixel(x, y, color);
            }
            for (int x = cx + stemWidth / 2 + 4; x <= cx + stemWidth / 2 + 10; x++)
            {
                for (int y = arm2Y; y < arm2Y + 20; y++) tex.SetPixel(x, y, color);
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 64f);
        }

        public static Sprite CreateSpikeSprite(Color color, int w = 48, int h = 90)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            ClearTexture(tex);

            int cx = w / 2;

            for (int y = 0; y < h; y++)
            {
                float t = (float)y / h;
                int currentWidth = Mathf.RoundToInt((1f - t) * (w * 0.5f));

                for (int x = cx - currentWidth; x <= cx + currentWidth; x++)
                {
                    // Uca doğru gradyan parlama
                    Color c = Color.Lerp(color, Color.white, t * 0.35f);
                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 64f);
        }

        public static Sprite CreateRockSprite(Color color, int w = 64, int h = 42)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            ClearTexture(tex);

            Vector2 center = new Vector2(w * 0.5f, 0f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x - center.x) / (w * 0.48f);
                    float ny = y / (float)h;

                    if (nx * nx + ny * ny <= 1f)
                    {
                        // Kaya çatlak & gölge efekti
                        Color c = (y > h * 0.6f) ? Color.Lerp(color, Color.black, 0.2f) : color;
                        tex.SetPixel(x, y, c);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 64f);
        }

        public static Sprite CreateBirdSprite(Color color, int w = 64, int h = 48)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            ClearTexture(tex);

            Vector2 center = new Vector2(w * 0.5f, h * 0.5f);

            // Kuş gövdesi (Elips)
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x - center.x) / (w * 0.35f);
                    float ny = (y - center.y) / (h * 0.25f);

                    if (nx * nx + ny * ny <= 1f)
                    {
                        tex.SetPixel(x, y, color);
                    }
                }
            }

            // Kanatlar
            for (int i = 0; i < 14; i++)
            {
                int wx = (int)(center.x - 8 + i);
                int wy = (int)(center.y + 6 + (14 - i) * 0.8f);
                if (wx >= 0 && wx < w && wy >= 0 && wy < h)
                {
                    tex.SetPixel(wx, wy, Color.Lerp(color, Color.white, 0.3f));
                    tex.SetPixel(wx, wy + 1, Color.Lerp(color, Color.white, 0.3f));
                }
            }

            // Göz
            DrawFilledCircle(tex, (int)(center.x + 12), (int)(center.y + 2), 2, Color.white);
            tex.SetPixel((int)(center.x + 13), (int)(center.y + 2), Color.black);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 64f);
        }

        public static Sprite CreateCircleSprite(Color color, int resolution = 64)
        {
            Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            float radius = resolution * 0.45f;
            Vector2 center = new Vector2(resolution * 0.5f, resolution * 0.5f);

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist <= radius)
                    {
                        tex.SetPixel(x, y, color);
                    }
                    else if (dist <= radius + 2)
                    {
                        float a = 1f - (dist - radius) / 2f;
                        tex.SetPixel(x, y, new Color(color.r, color.g, color.b, a * 0.5f));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), 64f);
        }

        public static Sprite CreateRingSprite(Color color, int resolution = 64, float innerRatio = 0.75f)
        {
            Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            float outerR = resolution * 0.48f;
            float innerR = outerR * innerRatio;
            Vector2 center = new Vector2(resolution * 0.5f, resolution * 0.5f);

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d >= innerR && d <= outerR)
                    {
                        tex.SetPixel(x, y, color);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
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

        private static void ClearTexture(Texture2D tex)
        {
            Color[] cols = new Color[tex.width * tex.height];
            for (int i = 0; i < cols.Length; i++) cols[i] = Color.clear;
            tex.SetPixels(cols);
        }

        private static void DrawFilledCircle(Texture2D tex, int cx, int cy, int r, Color col)
        {
            for (int y = cy - r; y <= cy + r; y++)
            {
                for (int x = cx - r; x <= cx + r; x++)
                {
                    if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                    {
                        if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r)
                        {
                            tex.SetPixel(x, y, col);
                        }
                    }
                }
            }
        }
    }
}
