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
        private Dictionary<string, (int Warnings, int Errors, int Exceptions)> _analyzeResults;

        // 一時ファイルパス
        private const string _tempLogFolder = "Temp/FlatsPlus";
        private string _tempLogPath = null;

        private ME_LogAnalyzer() { }

        // 初期化（エディター参照を保存）
        public void Initialize(ME_MainEditor editor)
        {
            _editor = editor;

            // 既存の一時ファイルをクリーンアップ
            CleanupTempFiles();

            AnalyzeLog();
        }

        // 一時ファイルのクリーンアップ
        private void CleanupTempFiles()
        {
            try
            {
                if (Directory.Exists(_tempLogFolder))
                {
                    string[] tempFiles = Directory.GetFiles(_tempLogFolder, "temp_log_*.txt");
                    foreach (string file in tempFiles)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"一時ファイルの削除に失敗しました: {file}, {ex.Message}");
                        }
                    }
                }
                else
                {
                    Directory.CreateDirectory(_tempLogFolder);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"一時ファイルのクリーンアップに失敗しました: {ex.Message}");
            }
        }

        // ログファイルの解析
        public void AnalyzeLog()
        {
            _lastBuild = DateTime.MinValue;
            _errorWorks = new List<string>();
            _errorUnknowns = new List<string>();
            _analyzeResults = null;

            if (!File.Exists(_logPath)) return;

            try
            {
                // オリジナルのログファイルを一時ファイルにコピー
                _tempLogPath = $"{_tempLogFolder}/temp_log_{DateTime.Now.Ticks}.txt";
                File.Copy(_logPath, _tempLogPath, true);

                // 一時ファイルから内容を読み込む
                _logContent = File.ReadAllText(_tempLogPath);
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

                // 一時ファイルを使用してログを解析
                _analyzeResults = Log.AnalyzeLog(_tempLogPath);
            }
            catch (IOException ex)
            {
                Debug.LogError($"ログファイルの読み込みに失敗しました: {ex.Message}");
                _logContent = $"ログファイルの読み込みエラー: {ex.Message}";
            }
            catch (Exception ex)
            {
                Debug.LogError($"ログの解析中にエラーが発生しました: {ex.Message}");
                _logContent = $"ログの解析エラー: {ex.Message}";
            }
            finally
            {
                // 処理が完了したら一時ファイルを削除
                if (_tempLogPath != null && File.Exists(_tempLogPath))
                {
                    try
                    {
                        File.Delete(_tempLogPath);
                        _tempLogPath = null;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"一時ファイルの削除に失敗しました: {ex.Message}");
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

            bool allFine = false;
            if (_analyzeResults != null && _analyzeResults.Count == 0)
            {
                allFine = true;
                EditorGUILayout.HelpBox(L("LogAnalyze/AllFine"), MessageType.Info);
            }

            if (_analyzeResults != null)
            {
                foreach (var item in _analyzeResults)
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

        // OnDestroy時に一時ファイルを確実に削除
        ~ME_LogAnalyzer()
        {
            if (_tempLogPath != null && File.Exists(_tempLogPath))
            {
                try
                {
                    File.Delete(_tempLogPath);
                }
                catch
                {
                    // デストラクタでは例外を無視
                }
            }
        }
    }
}