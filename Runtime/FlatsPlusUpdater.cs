using com.github.pandrabox.pandravase.runtime;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using static com.github.pandrabox.pandravase.editor.Localizer;

namespace com.github.pandrabox.flatsplus.runtime
{
    public class FlatsPlusUpdater
    {
        private static FlatsPlusUpdater Instance = new FlatsPlusUpdater();
        public static FlatsPlusUpdater I => Instance;
        private const string RepoUrl = "https://api.github.com/repos/pandrabox/FlatsPlus/tags";
        private const string PackageJsonPath = "Packages/com.github.pandrabox.flatsplus/package.json";
        private const string UpdaterPackagePath = "Packages/com.github.pandrabox.flatsplus/Assets/Updater/FlatsPlus.unitypackage";
        private static readonly HttpClient client = new HttpClient();

        private static string latestVersion = null;
        private static string currentVersion = null;
        private static bool updateAvailable = false;
        private static bool isChecking = false;

        // API呼び出し制限のための設定
        private const int MinimumRequestIntervalMinutes = 1; // 1分間隔
        private static ApiRequestSettings apiSettings;

        // 公開プロパティ
        public static bool UpdateAvailable => updateAvailable;
        public static string LatestVersion => latestVersion;
        public static string CurrentVersion => currentVersion;
        public static bool IsChecking => isChecking;

        private FlatsPlusUpdater()
        {
            EditorApplication.delayCall += () =>
            {
                LoadSettingsAndInitialize();
                CheckForUpdates();
            };
        }

        private static void LoadSettingsAndInitialize()
        {
            apiSettings = ApiRequestSettings.GetOrCreateSettings();

            // 保存されていた前回の結果を読み込む
            if (!string.IsNullOrEmpty(apiSettings.LastCurrentVersion))
            {
                latestVersion = apiSettings.LastLatestVersion;
                currentVersion = apiSettings.LastCurrentVersion;
                updateAvailable = apiSettings.LastUpdateAvailable;
            }
        }

        public static void CheckForUpdates()
        {
            if (isChecking) return;

            // 前回のリクエストからの経過時間をチェック
            TimeSpan elapsedTime = DateTime.Now - apiSettings.LastRequestTime;
            if (elapsedTime.TotalMinutes < MinimumRequestIntervalMinutes)
            {
                // 前回のリクエストから1分経過していない場合、保存された結果を使用
                Debug.Log($"前回のAPI呼び出しから{MinimumRequestIntervalMinutes}分経過していないため、保存されたデータを使用します。残り時間: {(MinimumRequestIntervalMinutes * 60) - elapsedTime.TotalSeconds}秒");
                return;
            }

            isChecking = true;
            CheckForUpdatesInternal();
        }

        private static async void CheckForUpdatesInternal()
        {
            try
            {
                // 現在のバージョンを取得
                currentVersion = GetCurrentVersion();
                if (string.IsNullOrEmpty(currentVersion))
                {
                    Debug.LogWarning("package.jsonからバージョン情報を取得できませんでした。");
                    apiSettings.HasError = true;
                    apiSettings.ErrorMessage = "package.jsonからバージョン情報を取得できませんでした。";
                    return;
                }

                // 最新のバージョンを取得
                latestVersion = await FetchLatestTag();
                if (string.IsNullOrEmpty(latestVersion))
                {
                    Debug.LogWarning("GitHubから最新バージョン情報を取得できませんでした。");
                    apiSettings.HasError = true;
                    apiSettings.ErrorMessage = "GitHubから最新バージョン情報を取得できませんでした。";
                    return;
                }

                // バージョン比較（シンプルな文字列比較）
                updateAvailable = IsNewerVersion(latestVersion, currentVersion);

                // 結果を保存
                apiSettings.LastRequestTime = DateTime.Now;
                apiSettings.LastRequestTimeStr = apiSettings.LastRequestTime.ToString("yyyy/MM/dd HH:mm:ss");
                apiSettings.LastLatestVersion = latestVersion;
                apiSettings.LastCurrentVersion = currentVersion;
                apiSettings.LastUpdateAvailable = updateAvailable;
                apiSettings.HasError = false;
                apiSettings.ErrorMessage = "";
                ApiRequestSettings.SaveSettings(apiSettings);

                // デバッグログ
                Debug.Log($"FlatsPlus - 現在のバージョン: {currentVersion}, 最新バージョン: {latestVersion}, 更新が必要: {updateAvailable}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"更新チェック中にエラーが発生しました: {ex.Message}");
                apiSettings.HasError = true;
                apiSettings.ErrorMessage = $"更新チェック中にエラーが発生しました: {ex.Message}";
                ApiRequestSettings.SaveSettings(apiSettings);
            }
            finally
            {
                isChecking = false;
                var flatsPlusEditors = Resources.FindObjectsOfTypeAll<editor.FlatsPlusEditor>();
                foreach (var editor in flatsPlusEditors)
                {
                    editor.Repaint();
                }
            }
        }

        private static string GetCurrentVersion()
        {
            try
            {
                var text = File.ReadAllText(PackageJsonPath);
                var match = System.Text.RegularExpressions.Regex.Match(text, "\"version\": *\"(\\d+\\.\\d+\\.\\d+)\"");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
                else
                {
                    Log.I.Warning("package.jsonからバージョン情報を取得できませんでした。");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log.I.Exception(ex, $"package.jsonからバージョン情報を取得できませんでした");
                return null;
            }
        }

        private static async Task<string> FetchLatestTag()
        {
            try
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("UnityEditor");
                var response = await client.GetStringAsync(RepoUrl);

                // GitHub API は配列を返すため、まずラッパーを作成します
                string wrappedJson = $"{{\"tags\": {response}}}";
                var tagsWrapper = JsonUtility.FromJson<TagList>(wrappedJson);

                if (tagsWrapper != null && tagsWrapper.tags.Length > 0)
                {
                    return tagsWrapper.tags[0].name;
                }
                else
                {
                    Debug.LogWarning("タグが見つかりませんでした。");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"GitHubからタグを取得できませんでした: {ex.Message}");
                return null;
            }
        }

        private static bool IsNewerVersion(string latestVersion, string currentVersion)
        {
            // バージョン文字列からvを削除（vx.y.z形式の場合）
            string latestClean = latestVersion.StartsWith("v") ? latestVersion.Substring(1) : latestVersion;
            string currentClean = currentVersion.StartsWith("v") ? currentVersion.Substring(1) : currentVersion;

            try
            {
                Version latest = new Version(latestClean);
                Version current = new Version(currentClean);

                return latest > current;
            }
            catch (Exception)
            {
                // バージョン解析に失敗した場合は、単純な文字列比較を行う
                return string.Compare(latestClean, currentClean, StringComparison.Ordinal) > 0;
            }
        }

        private static bool canReload => !isChecking && (DateTime.Now - apiSettings.LastRequestTime).TotalMinutes >= MinimumRequestIntervalMinutes;

        public void DrawUpdateInfo()
        {
            if (isChecking)
            {
                EditorGUILayout.HelpBox(L("Updater/Checking"), MessageType.Info);
                return;
            }

            if (apiSettings?.HasError == true && !string.IsNullOrEmpty(apiSettings.ErrorMessage))
            {
                EditorGUILayout.HelpBox($"{L("Updater/Error")}: {apiSettings.ErrorMessage}", MessageType.Error);
            }

            if (updateAvailable)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox($"{L("Updater/CanUseNewVersion")} (Ver.{latestVersion})", MessageType.Warning);
                if (GUILayout.Button($"{L("Updater/Update")}", GUILayout.Width(120), GUILayout.Height(EditorGUIUtility.singleLineHeight * 2)))
                {
                    ImportUpdater();
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox($@"{L("Updater/Newest")}(Ver.{currentVersion})", MessageType.Info);


                EditorGUI.BeginDisabledGroup(!canReload);
                if (GUILayout.Button($"{L("Updater/Reload")}", GUILayout.Width(120), GUILayout.Height(EditorGUIUtility.singleLineHeight * 2)))
                {
                    CheckForUpdates();
                }
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
            }
        }

        public static void ImportUpdater()
        {
            try
            {
                if (!File.Exists(UpdaterPackagePath))
                {
                    Debug.LogError($"アップデートパッケージが見つかりません: {UpdaterPackagePath}");
                    EditorUtility.DisplayDialog("エラー", "アップデートパッケージが見つかりませんでした。", "OK");
                    return;
                }

                AssetDatabase.ImportPackage(UpdaterPackagePath, true);
                Debug.Log("アップデートパッケージを開始しています...");
            }
            catch (Exception ex)
            {
                Debug.LogError($"アップデートパッケージのインポート中にエラーが発生しました: {ex.Message}");
                EditorUtility.DisplayDialog("エラー", $"アップデートに失敗しました: {ex.Message}", "OK");
            }
        }

        [Serializable]
        private class Tag
        {
            public string name;
        }

        [Serializable]
        private class TagList
        {
            public Tag[] tags;
        }

        [Serializable]
        private class PackageInfo
        {
            public string version;
        }
    }
}
