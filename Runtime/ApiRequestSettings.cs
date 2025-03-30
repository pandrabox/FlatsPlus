
#if UNITY_EDITOR
using System;
using UnityEngine;

namespace com.github.pandrabox.flatsplus.runtime
{
    [CreateAssetMenu(fileName = "ApiRequestSettings", menuName = "FlatsPlus/ApiRequestSettings")]
    public class ApiRequestSettings : ScriptableObject
    {
        public DateTime LastRequestTime;
        public string LastRequestTimeStr;
        public string LastLatestVersion;
        public string LastCurrentVersion;
        public bool LastUpdateAvailable;
        public bool HasError;
        public string ErrorMessage;

        private const string SettingsPath = "Packages/com.github.pandrabox.flatsplus/Assets/ApiRequestSettings.asset";

        public static ApiRequestSettings GetOrCreateSettings()
        {
            ApiRequestSettings settings = null;

            // エディタでのみ実行するコード
            settings = UnityEditor.AssetDatabase.LoadAssetAtPath<ApiRequestSettings>(SettingsPath);

            if (settings == null)
            {
                settings = CreateInstance<ApiRequestSettings>();
                settings.LastRequestTime = DateTime.MinValue;
                settings.LastRequestTimeStr = "";
                settings.LastLatestVersion = "";
                settings.LastCurrentVersion = "";
                settings.LastUpdateAvailable = false;

                // 設定フォルダが存在することを確認
                string directory = System.IO.Path.GetDirectoryName(SettingsPath);
                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                UnityEditor.AssetDatabase.CreateAsset(settings, SettingsPath);
                UnityEditor.AssetDatabase.SaveAssets();
            }
            return settings;
        }

        public static void SaveSettings(ApiRequestSettings settings)
        {
#if UNITY_EDITOR
            if (settings != null)
            {
                UnityEditor.EditorUtility.SetDirty(settings);
                UnityEditor.AssetDatabase.SaveAssets();
            }
#endif
        }
    }
}


#endif
