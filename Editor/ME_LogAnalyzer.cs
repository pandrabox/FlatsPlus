using com.github.pandrabox.pandravase.editor;
using com.github.pandrabox.pandravase.runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using static com.github.pandrabox.pandravase.editor.Localizer;

namespace com.github.pandrabox.flatsplus.editor
{
    // ログ解析を担当するクラス
    public class ME_LogAnalyzer
    {
        private static ME_LogAnalyzer _instance;
        public static ME_LogAnalyzer Instance => _instance ?? (_instance = new ME_LogAnalyzer());

        private DateTime _lastBuild;
        private List<string> _errorWorks = new List<string>();
        private List<string> _errorUnknowns = new List<string>();
        private const string _logPath = "Packages/com.github.pandrabox.flatsplus/Log/log.txt";
        private string _logContent;
        private ME_MainEditor _editor;

        private ME_LogAnalyzer() { }

        // 初期化（エディター参照を保存）
        public void Initialize(ME_MainEditor editor)
        {
            _editor = editor;
            AnalyzeLog();
        }

        // ログファイルの解析
        public void AnalyzeLog()
        {
            _lastBuild = DateTime.MinValue;
            _errorWorks = new List<string>();
            _errorUnknowns = new List<string>();
            if (!File.Exists(_logPath)) return;
            
            _logContent = File.ReadAllText(_logPath);
            if (_logContent.Length == 0) return;
            
            string[] lines = _logContent.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("@@FlatsPlusBuildStart@@"))
                {
                    var parts = line.Split(',');
                    if (parts.Length >= 1 && DateTime.TryParse(parts[0], out DateTime buildDateTime))
                    {
                        _lastBuild = buildDateTime;
                    }
                }
                else if (line.Contains("@@ERROR@@"))
                {
                    var parts = line.Split(',');
                    bool workError = false;
                    if (parts.Length >= 2)
                    {
                        var workName = parts[1];
                        if (workName.Length > 0)
                        {
                            _errorWorks.Add(workName);
                            workError = true;
                        }
                    }
                    if (!workError)
                    {
                        _errorUnknowns.Add(line);
                    }
                }
            }
        }

        // 解析結果の表示
        public void DrawAnalysisResults()
        {
            if (_lastBuild == DateTime.MinValue) return;
            if (_editor == null) return;

            _editor.ShowTitle(L("LogAnalyze/Title") + $@" ({_lastBuild.ToString()})");

            Dictionary<string, (int Warnings, int Errors, int Exceptions)> res = Log.AnalyzeLog(_logPath);
            bool allFine = false;
            if (res != null && res.Count == 0)
            {
                allFine = true;
                EditorGUILayout.HelpBox(L("LogAnalyze/AllFine"), MessageType.Info);
            }
            foreach (var item in res)
            {
                if (item.Value.Warnings > 0 || item.Value.Errors > 0 || item.Value.Exceptions > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine(L("LogAnalyze/PleaseCallMe"));
                    sb.AppendLine($@"{item.Key}");
                    if (item.Value.Warnings > 0) sb.AppendLine($@" - {L("LogAnalyze/Warning")}:{item.Value.Warnings}");
                    if (item.Value.Errors > 0 || item.Value.Exceptions > 0) sb.AppendLine($@" - {L("LogAnalyze/Error")}:{item.Value.Errors + item.Value.Exceptions}");
                    EditorGUILayout.HelpBox(sb.ToString(), MessageType.Error);
                }
            }
            
            const bool ISPREVIEWVERSION = true;
            if (!allFine || ISPREVIEWVERSION)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(L("LogAnalyze/CopyLog")))
                    {
                        EditorGUIUtility.systemCopyBuffer = _logContent;
                        EditorUtility.DisplayDialog(L("LogAnalyze/CopyCompleteTitle"), L("LogAnalyze/CopyCompleteMessage"), "OK");
                    }
                    if (GUILayout.Button(L("Editor/CloseProgressBar")))
                    {
                        PanProgressBar.Hide();
                    }
                }
            }
        }
    }
}
