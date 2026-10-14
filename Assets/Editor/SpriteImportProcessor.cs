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
                textureImporter.spriteImportMode = SpriteImportMode.Single;
                textureImporter.spritePixelsPerUnit = 400f;
                textureImporter.alphaIsTransparency = true;
                textureImporter.mipmapEnabled = false;
                textureImporter.filterMode = FilterMode.Bilinear;

                TextureImporterSettings settings = new TextureImporterSettings();
                textureImporter.ReadTextureSettings(settings);

                string lower = assetPath.ToLowerInvariant();
                if (lower.Contains("cactus") || lower.Contains("rock") || lower.Contains("spike"))
                {
                    settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                    settings.spritePivot = new Vector2(0.5f, 0.0f);
                }
                else
                {
                    settings.spriteAlignment = (int)SpriteAlignment.Center;
                    settings.spritePivot = new Vector2(0.5f, 0.5f);
                }

                textureImporter.SetTextureSettings(settings);
            }
        }
    }
}
