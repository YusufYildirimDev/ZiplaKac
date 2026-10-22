#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace ZiplaKac.Editor
{
    public static class IOSPostProcessBuild
    {
        [PostProcessBuild(100)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS) return;

            Debug.Log($"[IOSPostProcessBuild] Post-process çalıştırılıyor: {pathToBuiltProject}");

            try
            {
                // 1. Xcode Proje Ayarları & Framework Bağlantıları
                string projPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
                PBXProject proj = new PBXProject();
                proj.ReadFromFile(projPath);

                string targetGuid = proj.GetUnityMainTargetGuid();
                string frameworkTargetGuid = proj.GetUnityFrameworkTargetGuid();

                proj.AddFrameworkToProject(targetGuid, "AppTrackingTransparency.framework", false);
                proj.AddFrameworkToProject(targetGuid, "AdSupport.framework", false);
                proj.AddFrameworkToProject(targetGuid, "Security.framework", false);

                if (!string.IsNullOrEmpty(frameworkTargetGuid))
                {
                    proj.AddFrameworkToProject(frameworkTargetGuid, "AppTrackingTransparency.framework", false);
                    proj.AddFrameworkToProject(frameworkTargetGuid, "AdSupport.framework", false);
                    proj.AddFrameworkToProject(frameworkTargetGuid, "Security.framework", false);
                }

                proj.WriteToFile(projPath);
                Debug.Log("[IOSPostProcessBuild] AppTrackingTransparency, AdSupport & Security frameworkleri bağlandı.");

                // 2. Info.plist Güncellemesi
                string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
                PlistDocument plist = new PlistDocument();
                plist.ReadFromFile(plistPath);
                PlistElementDict rootDict = plist.root;

                // ATT İzin Açıklaması (App Store Zorunlu)
                rootDict.SetString("NSUserTrackingUsageDescription",
                    "Bu izin, ilgi alanlarınıza özel ödüllü reklamlar sunmak ve oyun deneyiminizi geliştirmek için kullanılır.");

                // AdMob Uygulama Kimliği (GADApplicationIdentifier)
                rootDict.SetString("GADApplicationIdentifier", "ca-app-pub-3940256099942544~145869519");

                // SKAdNetwork Tanımlayıcıları
                PlistElementArray skAdNetworkItems = rootDict.CreateArray("SKAdNetworkItems");
                string[] skAdNetworkIds = new string[]
                {
                    "cstr6suwn9.skadnetwork", // Google AdMob
                    "4fzdc2evr5.skadnetwork",
                    "2fnua5tdw4.skadnetwork",
                    "ydx93a7ass.skadnetwork",
                    "p78axxw29g.skadnetwork",
                    "v72qych544.skadnetwork",
                    "6xzpu9s2p8.skadnetwork",
                    "mlmmfzh3r3.skadnetwork",
                    "c6kg9kv798.skadnetwork",
                    "wzmmz9fp6w.skadnetwork",
                    "yclnxrl5pm.skadnetwork",
                    "t38b2kh725.skadnetwork",
                    "7ug5zh24hu.skadnetwork",
                    "gta9lk7p23.skadnetwork",
                    "v4nxqhlyqp.skadnetwork",
                    "47vhws6wlr.skadnetwork",
                    "cp8zw746q7.skadnetwork",
                    "y5ghdn5j9k.skadnetwork",
                    "n6fk4nfna4.skadnetwork",
                    "v9wttpbfk9.skadnetwork",
                    "n38lu8286q.skadnetwork",
                    "4dzt52r2t5.skadnetwork",
                    "ludvb6z3bs.skadnetwork"
                };

                foreach (var id in skAdNetworkIds)
                {
                    PlistElementDict item = skAdNetworkItems.AddDict();
                    item.SetString("SKAdNetworkIdentifier", id);
                }

                // Ekran Yönü (Orientation) Doğrulaması: Dikey mod garantisi
                PlistElementArray orientations = rootDict["UISupportedInterfaceOrientations"] as PlistElementArray;
                if (orientations == null)
                {
                    orientations = rootDict.CreateArray("UISupportedInterfaceOrientations");
                }
                orientations.values.Clear();
                orientations.AddString("UIInterfaceOrientationPortrait");
                orientations.AddString("UIInterfaceOrientationPortraitUpsideDown");

                // iPad yön ayarları
                PlistElementArray ipadOrientations = rootDict["UISupportedInterfaceOrientations~ipad"] as PlistElementArray;
                if (ipadOrientations == null)
                {
                    ipadOrientations = rootDict.CreateArray("UISupportedInterfaceOrientations~ipad");
                }
                ipadOrientations.values.Clear();
                ipadOrientations.AddString("UIInterfaceOrientationPortrait");
                ipadOrientations.AddString("UIInterfaceOrientationPortraitUpsideDown");

                plist.WriteToFile(plistPath);
                Debug.Log("[IOSPostProcessBuild] Info.plist ayarları başarıyla tamamlandı.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[IOSPostProcessBuild] Hata oluştu: {ex.Message}");
            }
        }
    }
}
#endif
