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
            PlayerSettings.productName = "Hoppzy";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.yusufyldrm.ziplakac");
            PlayerSettings.iOS.appleDeveloperTeamID = "5A4A2VUMDM";
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = true;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            ConfigureAppIcons();

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
                SyncAppIconToXcodeBuild(buildPath);
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[BuildPipelineScript] iOS Build failed with {summary.totalErrors} errors!");
                EditorApplication.Exit(1);
            }
        }

        public static void BuildAndroidAAB()
        {
            EnsureWorldSpritesImported();

            string[] scenes = new string[] { "Assets/Scenes/SampleScene.unity" };
            string buildPath = "Builds/Android/Hoppzy.aab";
            string dir = System.IO.Path.GetDirectoryName(buildPath);
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            Debug.Log($"[BuildPipelineScript] Starting Android AAB build to '{buildPath}'...");

            PlayerSettings.companyName = "YusufYildirim";
            PlayerSettings.productName = "Hoppzy";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.yusufyldrm.ziplakac");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = true;

            ConfigureAppIcons();

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            Debug.Log($"[BuildPipelineScript] Android Result: {summary.result}, Errors: {summary.totalErrors}");

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("[BuildPipelineScript] Android AAB Build succeeded!");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[BuildPipelineScript] Android Build failed with {summary.totalErrors} errors!");
                EditorApplication.Exit(1);
            }
        }

        private static void ConfigureAppIcons()
        {
            string masterIconPath = "Assets/Art/AppIcon/app_icon_1024.png";
            TextureImporter ti = AssetImporter.GetAtPath(masterIconPath) as TextureImporter;
            if (ti != null)
            {
                bool changed = false;
                if (!ti.isReadable) { ti.isReadable = true; changed = true; }
                if (ti.maxTextureSize < 1024) { ti.maxTextureSize = 1024; changed = true; }
                if (ti.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    ti.textureCompression = TextureImporterCompression.Uncompressed;
                    changed = true;
                }
                if (changed)
                {
                    ti.SaveAndReimport();
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                }
            }

            Texture2D iconTex = AssetDatabase.LoadAssetAtPath<Texture2D>(masterIconPath);
            if (iconTex != null)
            {
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.iOS, new Texture2D[] { iconTex });
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, new Texture2D[] { iconTex });
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new Texture2D[] { iconTex });
                Debug.Log("[BuildPipelineScript] Assigned app icon to iOS, Android and Unknown target groups.");
            }
            else
            {
                Debug.LogWarning($"[BuildPipelineScript] Could not load master icon from {masterIconPath}");
            }
        }

        private static void SyncAppIconToXcodeBuild(string buildPath)
        {
            try
            {
                string sourceDir = "Assets/Art/AppIcon/AppIcon.appiconset";
                string targetDir = System.IO.Path.Combine(buildPath, "Unity-iPhone/Images.xcassets/AppIcon.appiconset");

                if (!System.IO.Directory.Exists(sourceDir))
                {
                    Debug.LogWarning($"[BuildPipelineScript] Source icon set not found at {sourceDir}");
                    return;
                }

                if (!System.IO.Directory.Exists(targetDir))
                {
                    System.IO.Directory.CreateDirectory(targetDir);
                }

                foreach (string srcFile in System.IO.Directory.GetFiles(sourceDir))
                {
                    if (srcFile.EndsWith(".meta")) continue;
                    string destFile = System.IO.Path.Combine(targetDir, System.IO.Path.GetFileName(srcFile));
                    System.IO.File.Copy(srcFile, destFile, true);
                }
                Debug.Log($"[BuildPipelineScript] Successfully synced AppIcon.appiconset to {targetDir}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BuildPipelineScript] Failed syncing AppIcon.appiconset: {ex.Message}");
            }
        }

        public static void EnsureWorldSpritesImported()
        {
            Debug.Log("[BuildPipelineScript] Refreshing AssetDatabase...");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            string[] searchFolders = new[] { "Assets/Resources/Sprites", "Assets/Art/Sprites" };
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", searchFolders);
            Debug.Log($"[BuildPipelineScript] Found {guids.Length} total game textures.");

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
                    else
                    {
                        // Kiyafetler, Obstacles, Heroes: PPU 400
                        if (Mathf.Abs(ti.spritePixelsPerUnit - 400f) > 0.01f || ti.wrapMode != TextureWrapMode.Clamp)
                        {
                            ti.spritePixelsPerUnit = 400f;
                            ti.wrapMode = TextureWrapMode.Clamp;
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

            // Verify Resources.Load works for Worlds, Obstacles, and Kiyafetler
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

                string obsKaya = $"Sprites/Obstacles/engel_{k}_kaya";
                Sprite spKaya = Resources.Load<Sprite>(obsKaya);

                string kiyPelerin = $"Sprites/Kiyafetler/kiyafet_{k}_pelerin";
                Sprite spPelerin = Resources.Load<Sprite>(kiyPelerin);

                Debug.Log($"[VERIFY_SPRITE] '{k}': bg={(spBg != null ? "OK" : "NULL")} | zemin={(spZemin != null ? "OK" : "NULL")} | obs={(spKaya != null ? "OK" : "NULL")} | kiy={(spPelerin != null ? "OK" : "NULL")}");
            }
        }
    }
}
