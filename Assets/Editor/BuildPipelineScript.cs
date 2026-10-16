using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ZiplaKac.Editor
{
    public static class BuildPipelineScript
    {
        public static void BuildIOS()
        {
            EnsureWorldSpritesImported();

            string[] scenes = new string[] { "Assets/Scenes/SampleScene.unity" };
            string buildPath = "iOS_Build";

            Debug.Log($"[BuildPipelineScript] Starting iOS build to '{buildPath}'...");

            PlayerSettings.companyName = "YusufYildirim";
            PlayerSettings.productName = "ZiplaKac";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.yusufyldrm.ziplakac");
            PlayerSettings.iOS.appleDeveloperTeamID = "29367Z6APG";
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildPath,
                target = BuildTarget.iOS,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            Debug.Log($"[BuildPipelineScript] Result: {summary.result}, Total Time: {summary.totalTime.TotalSeconds:F1}s, Errors: {summary.totalErrors}");

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("[BuildPipelineScript] iOS Build succeeded!");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[BuildPipelineScript] iOS Build failed with {summary.totalErrors} errors!");
                EditorApplication.Exit(1);
            }
        }

        public static void EnsureWorldSpritesImported()
        {
            Debug.Log("[BuildPipelineScript] Refreshing AssetDatabase...");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            string[] searchFolders = new[] { "Assets/Resources/Sprites/Worlds", "Assets/Art/Sprites/Worlds" };
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", searchFolders);
            Debug.Log($"[BuildPipelineScript] Found {guids.Length} world textures.");

            int reimported = 0;
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti != null)
                {
                    bool changed = false;
                    if (ti.textureType != TextureImporterType.Sprite)
                    {
                        ti.textureType = TextureImporterType.Sprite;
                        ti.spriteImportMode = SpriteImportMode.Single;
                        changed = true;
                    }
                    if (!ti.alphaIsTransparency)
                    {
                        ti.alphaIsTransparency = true;
                        changed = true;
                    }
                    if (ti.mipmapEnabled)
                    {
                        ti.mipmapEnabled = false;
                        changed = true;
                    }

                    string lower = path.ToLowerInvariant();
                    if (lower.Contains("/bg_"))
                    {
                        if (Mathf.Abs(ti.spritePixelsPerUnit - 192f) > 0.01f || ti.wrapMode != TextureWrapMode.Clamp)
                        {
                            ti.spritePixelsPerUnit = 192f;
                            ti.wrapMode = TextureWrapMode.Clamp;
                            changed = true;
                        }
                    }
                    else if (lower.Contains("/zemin_"))
                    {
                        if (Mathf.Abs(ti.spritePixelsPerUnit - 150f) > 0.01f || ti.wrapMode != TextureWrapMode.Repeat)
                        {
                            ti.spritePixelsPerUnit = 150f;
                            ti.wrapMode = TextureWrapMode.Repeat;
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        ti.SaveAndReimport();
                        reimported++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[BuildPipelineScript] Processed {reimported} textures into Sprites.");

            // Verify Resources.Load works
            string[] testKeys = new[] { "klasik", "ates", "buz", "zehir", "gece" };
            var names = new System.Collections.Generic.Dictionary<string, string[]>
            {
                { "klasik", new[] { "01_gunduz_cayiri", "02_altin_sonbahar", "03_yildizli_cayir_gecesi" } },
                { "ates",   new[] { "01_col_gunbatimi", "02_lav_vadisi", "03_kizil_gece" } },
                { "buz",    new[] { "01_karli_daglar", "02_buzul_alacakaranligi", "03_kutup_gecesi" } },
                { "zehir",  new[] { "01_yemyesil_orman", "02_toksik_sis", "03_radyoaktif_gece" } },
                { "gece",   new[] { "01_alacakaranlik_orman", "02_mor_nebula", "03_derin_uzay" } }
            };

            foreach (var k in testKeys)
            {
                string bgPath = $"Sprites/Worlds/bg_{k}_{names[k][0]}";
                Sprite spBg = Resources.Load<Sprite>(bgPath);
                string zeminPath = $"Sprites/Worlds/zemin_{k}_{names[k][0]}";
                Sprite spZemin = Resources.Load<Sprite>(zeminPath);

                Debug.Log($"[VERIFY_SPRITE] '{k}': bg={(spBg != null ? spBg.name : "NULL")} | zemin={(spZemin != null ? spZemin.name : "NULL")}");
            }
        }
    }
}
