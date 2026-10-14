using UnityEditor;
using UnityEngine;

namespace ZiplaKac.Editor
{
    /// <summary>
    /// Assets/Art/Sprites ve Assets/Resources/Sprites klasörlerine eklenen tüm görsel varlıkları (PNG/JPG)
    /// otomatik olarak 2D Sprite, PPU 200 ve şeffaflık aktif olarak içe aktaran editör işlemcisi.
    /// </summary>
    public class SpriteImportProcessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (assetPath.Contains("Assets/Art/Sprites") || assetPath.Contains("Assets/Resources/Sprites"))
            {
                TextureImporter textureImporter = (TextureImporter)assetImporter;
                textureImporter.textureType = TextureImporterType.Sprite;
                textureImporter.alphaIsTransparency = true;
                textureImporter.mipmapEnabled = false;
                textureImporter.filterMode = FilterMode.Bilinear;

                TextureImporterSettings settings = new TextureImporterSettings();
                textureImporter.ReadTextureSettings(settings);

                string lower = assetPath.ToLowerInvariant();
                if (lower.Contains("bg_") || lower.Contains("ground_") || lower.Contains("background"))
                {
                    textureImporter.spritePixelsPerUnit = 200f;
                    if (lower.Contains("mountain") || lower.Contains("hill"))
                    {
                        settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                        settings.spritePivot = new Vector2(0.5f, 0.0f);
                    }
                    else if (lower.Contains("ground"))
                    {
                        settings.spriteAlignment = (int)SpriteAlignment.TopCenter;
                        settings.spritePivot = new Vector2(0.5f, 1.0f);
                    }
                    else
                    {
                        settings.spriteAlignment = (int)SpriteAlignment.Center;
                        settings.spritePivot = new Vector2(0.5f, 0.5f);
                    }
                }
                else if (lower.Contains("cactus") || lower.Contains("rock") || lower.Contains("spike"))
                {
                    textureImporter.spritePixelsPerUnit = 400f;
                    settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                    settings.spritePivot = new Vector2(0.5f, 0.0f);
                }
                else
                {
                    textureImporter.spritePixelsPerUnit = 400f;
                    settings.spriteAlignment = (int)SpriteAlignment.Center;
                    settings.spritePivot = new Vector2(0.5f, 0.5f);
                }

                textureImporter.SetTextureSettings(settings);
            }
        }
    }
}
