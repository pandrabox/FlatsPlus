using com.github.pandrabox.flatsplus.editor;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;
using static com.github.pandrabox.pandravase.editor.Util;

namespace com.github.pandrabox.flatsplus.editor
{
    /// <summary>
    /// 椅子のデフォルト状態確認用デバッグツール
    /// NDMF処理後の椅子状態を確認するために使用
    /// </summary>
    public class ChairStateChecker
    {
        [MenuItem("PanDbg/椅子状態確認/1. FlatsPlus処理実行")]
        public static void RunFlatsPlusProcessing()
        {
            var avatar = GetTopAvatar();
            if (avatar == null)
            {
                EditorUtility.DisplayDialog("椅子状態確認", "アバターが見つかりません\nHierarchyにVRCAvatarDescriptorがあるか確認してください", "OK");
                return;
            }

            SetDebugMode(true);
            new FlatsPlusMain(avatar);

            EditorUtility.DisplayDialog("椅子状態確認", "FlatsPlus処理が完了しました\n次へ「2. 椅子状態確認」を実行してください", "OK");
        }

        [MenuItem("PanDbg/椅子状態確認/2. 椅子状態確認")]
        public static void CheckChairState()
        {
            var avatar = GetTopAvatar();
            if (avatar == null)
            {
                EditorUtility.DisplayDialog("椅子状態確認", "アバターが見つかりません", "OK");
                return;
            }

            string result = CheckChairs(avatar.gameObject);

            EditorUtility.DisplayDialog("椅子状態確認結果", result, "OK");
        }

        [MenuItem("PanDbg/椅子状態確認/3. 結果をファイルに保存")]
        public static void SaveChairStateToFile()
        {
            var avatar = GetTopAvatar();
            if (avatar == null)
            {
                EditorUtility.DisplayDialog("椅子状態確認", "アバターが見つかりません", "OK");
                return;
            }

            string result = CheckChairs(avatar.gameObject);
            string filePath = "Packages/com.github.pandrabox.flatsplus/ChairStateResult.txt";

            System.IO.File.WriteAllText(filePath, result);

            EditorUtility.DisplayDialog("椅子状態確認", $"結果を {filePath} に保存しました", "OK");
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 椅子の状態を確認する
        /// VRCStationコンポーネントを検索し、それと一緒にColliderがあるか、そしてそれがDisableになっていることを確認
        /// </summary>
        static string CheckChairs(GameObject root)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("=== 椅子状態確認結果 ===");
            sb.AppendLine();

            // VRCStationコンポーネントをすべて検索
            var vrcStations = root.GetComponentsInChildren<VRCStation>(true);
            sb.AppendLine($"VRCStationの総数: {vrcStations.Length}");
            sb.AppendLine();

            if (vrcStations.Length == 0)
            {
                sb.AppendLine("VRCStationが見つかりませんでした");
                return sb.ToString();
            }

            int colliderDisabledCount = 0;
            int totalColliderCount = 0;

            for (int i = 0; i < vrcStations.Length; i++)
            {
                var station = vrcStations[i];
                sb.AppendLine($"[VRCStation #{i + 1}]");
                sb.AppendLine($"  GameObject名: {station.gameObject.name}");
                sb.AppendLine($"  GameObjectパス: {GetTransformPath(station.transform)}");
                sb.AppendLine($"  GameObjectアクティブ: {station.gameObject.activeSelf}");

                // 同じGameObjectにColliderがあるか確認
                var collider = station.GetComponent<Collider>();
                if (collider != null)
                {
                    sb.AppendLine($"  Colliderあり: True (種類: {collider.GetType().Name})");
                    sb.AppendLine($"  Collider.Enabled: {collider.enabled}");
                    totalColliderCount++;

                    if (!collider.enabled)
                    {
                        colliderDisabledCount++;
                        sb.AppendLine($"  状態: ✓ 無効化されている（OK）");
                    }
                    else
                    {
                        sb.AppendLine($"  状態: ✗ 有効になっている（問題あり）");
                    }
                }
                else
                {
                    sb.AppendLine($"  Colliderあり: False");
                }
                sb.AppendLine();
            }

            // 結論
            sb.AppendLine("=== 結論 ===");
            bool isOk = vrcStations.Length >= 2 && colliderDisabledCount == totalColliderCount && totalColliderCount >= 2;
            if (isOk)
            {
                sb.AppendLine("✓ VRCStationは2個以上あり、各Colliderは無効化されています（OK）");
            }
            else
            {
                sb.AppendLine("✗ 問題があります：");
                if (vrcStations.Length < 2) sb.AppendLine("  - VRCStationが2個未満です");
                if (totalColliderCount < 2) sb.AppendLine("  - Colliderが2個未満です");
                if (colliderDisabledCount != totalColliderCount) sb.AppendLine("  - 一部のColliderが有効化されています");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Transformのパスを取得する
        /// </summary>
        static string GetTransformPath(Transform transform)
        {
            if (transform.parent == null)
                return "/" + transform.name;
            return GetTransformPath(transform.parent) + "/" + transform.name;
        }

        /// <summary>
        /// トップレベルのアバターを取得する
        /// </summary>
        static VRCAvatarDescriptor GetTopAvatar()
        {
            VRCAvatarDescriptor[] avatars = Object.FindObjectsOfType<VRCAvatarDescriptor>();
            if (avatars.Length == 0) return null;
            return avatars[0];
        }
    }
}
