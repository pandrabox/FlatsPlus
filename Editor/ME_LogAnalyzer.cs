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
    // ���O��͂�S������N���X
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

        // �ꎞ�t�@�C���p�X
        private const string _tempLogFolder = "Temp/FlatsPlus";
        private string _tempLogPath = null;

        private ME_LogAnalyzer() { }

        // �������i�G�f�B�^�[�Q�Ƃ�ۑ��j
        public void Initialize(ME_MainEditor editor)
        {
            _editor = editor;

            // �����̈ꎞ�t�@�C�����N���[���A�b�v
            CleanupTempFiles();

            AnalyzeLog();
        }

        // �ꎞ�t�@�C���̃N���[���A�b�v
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
                            Debug.LogWarning($"�ꎞ�t�@�C���̍폜�Ɏ��s���܂���: {file}, {ex.Message}");
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
                Debug.LogError($"�ꎞ�t�@�C���̃N���[���A�b�v�Ɏ��s���܂���: {ex.Message}");
            }
        }

        // ���O�t�@�C���̉��
        public void AnalyzeLog()
        {
            _lastBuild = DateTime.MinValue;
            _errorWorks = new List<string>();
            _errorUnknowns = new List<string>();
            _analyzeResults = null;

            if (!File.Exists(_logPath)) return;

            try
            {
                // �I���W�i���̃��O�t�@�C�����ꎞ�t�@�C���ɃR�s�[
                _tempLogPath = $"{_tempLogFolder}/temp_log_{DateTime.Now.Ticks}.txt";
                File.Copy(_logPath, _tempLogPath, true);

                // �ꎞ�t�@�C��������e��ǂݍ���
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

                // �ꎞ�t�@�C�����g�p���ă��O�����
                _analyzeResults = Log.AnalyzeLog(_tempLogPath);
            }
            catch (IOException ex)
            {
                Debug.LogError($"���O�t�@�C���̓ǂݍ��݂Ɏ��s���܂���: {ex.Message}");
                _logContent = $"���O�t�@�C���̓ǂݍ��݃G���[: {ex.Message}";
            }
            catch (Exception ex)
            {
                Debug.LogError($"���O�̉�͒��ɃG���[���������܂���: {ex.Message}");
                _logContent = $"���O�̉�̓G���[: {ex.Message}";
            }
            finally
            {
                // ����������������ꎞ�t�@�C�����폜
                if (_tempLogPath != null && File.Exists(_tempLogPath))
                {
                    try
                    {
                        File.Delete(_tempLogPath);
                        _tempLogPath = null;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"�ꎞ�t�@�C���̍폜�Ɏ��s���܂���: {ex.Message}");
                    }
                }
            }
        }

        // ��͌��ʂ̕\��
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

        // OnDestroy���Ɉꎞ�t�@�C�����m���ɍ폜
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
                    // �f�X�g���N�^�ł͗�O�𖳎�
                }
            }
        }
    }
}