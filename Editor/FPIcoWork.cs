using com.github.pandrabox.flatsplus.runtime;
using com.github.pandrabox.pandravase.editor;
using com.github.pandrabox.pandravase.runtime;
using nadena.dev.modular_avatar.core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static com.github.pandrabox.pandravase.editor.Localizer;
using static com.github.pandrabox.pandravase.editor.TextureUtil;
using static com.github.pandrabox.pandravase.editor.Util;

namespace com.github.pandrabox.flatsplus.editor
{
#if PANDRADBG
    public class FlatsPlusIcoDebug
    {
        [MenuItem("PanDbg/FlatsPlusIco")]
        public static void FlatsPlusIco_Debug()
        {
            SetDebugMode(true);
            var fp = new FlatsProject(TopAvatar);
            new FPIcoWork(fp);
        }
    }
#endif

    public class FPIcoWork : FlatsWork<FPIco>
    {
        public FPIcoWork(FlatsProject fp) : base(fp) { }

        GameObject _callPlane;
        Texture2D[] _icos;

        sealed protected override void OnConstruct()
        {
            GetStructure();
            ReplacePackTexture();
            CreateMenu();
        }


        /// <summary>
        /// _config.Ico_Texturesは度々nullになるので安全に取得する
        /// </summary>
        public Texture2D[] Icos
        {
            get
            {
                //正しく取得できている場合はそのまま返す
                if (_icos != null)
                {
                    if (_icos.All(t => t != null))
                    {
                        return _icos;
                    }
                }

                _icos = new Texture2D[8];

                try
                {
                    // _configから読める分を読む
                    if (!(_config == null || _config.Ico_Textures == null))
                    {
                        for (int i = 0; i < Math.Min(_icos.Length, _config.Ico_Textures.Length); i++)
                        {
                            _icos[i] = _config.Ico_Textures[i];
                        }
                    }

                    // ない分をデフォルトから読み込み
                    for (int i = 0; i < _icos.Length; i++)
                    {
                        if (_icos[i] == null)
                        {
                            string path = $"Packages/com.github.pandrabox.flatsplus/Assets/Ico/Ico/i{i + 1}.png";
                            _icos[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

                            if (_icos[i] == null) //まずあり得ないが念のためのチェック
                            {
                                Log.I.Warning($"Could not load default texture at path: {path}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.I.Exception(ex, "_icos getter でエラーが発生しました");
                }

                return _icos;
            }
        }

        private void ReplacePackTexture()
        {
            using (var capture = new PanCapture(BGColor: new Color(1, 1, 1, 1), margin: 10, padding: 10, width: 170))
            {
                FlatsProject prj = new FlatsProject(_desc);
                var strImg = capture.TextToImage($"{prj.ProjectName}\n\r{prj.VPMVersion}");

                List<Texture2D> textures = new List<Texture2D>(Icos);

                textures.Add(strImg);
                Texture2D packTexture = PackTexture(textures, 3, 170 * 3);
                try
                {
                    _callPlane.GetComponent<Renderer>().material.mainTexture = packTexture;
                }
                catch (Exception ex)
                {
                    Log.I.Exception(ex, "Textureの置換に失敗しました(設定先mainTextureの取得に失敗しました)");
                }
            }
        }
        private void CreateMenu()
        {
            MenuBuilder mb = new MenuBuilder(_prj);
            mb.AddFolder("FlatsPlus", true).AddFolder(L("Menu/Ico"));
            string name;
            Texture2D[] iconTextures = Icos;
            for (int i = 1; i < 9; i++)
            {
                // 配列のインデックスは0始まり、iは1始まり
                int index = i - 1;
                Texture2D currentIco = iconTextures[index];
                if (i < 7)
                {
                    name = (i).ToString();
                    mb.AddToggle($"FlatsPlus/Ico/IcoType", name, i, ParameterSyncType.Int).SetIco(currentIco);
                }
                else if (i == 7)
                {
                    name = L("Menu/Ico/Resonance");
                    mb.AddToggle($"FlatsPlus/Ico/IcoType", name, i, ParameterSyncType.Int).SetMessage("Menu/Ico/Resonance/Detail".LL()).SetIco(currentIco);
                }
                else if (_config.Ico_VerView && i == 8)
                {
                    name = L("Menu/Ico/VerView");
                    mb.AddButton("FlatsPlus/Ico/IcoType", i, ParameterSyncType.Int, name).SetMessage("Menu/Ico/VerView/Detail".LL()).SetIco(currentIco);
                }
            }
        }

        /// <summary>
        /// プレハブの構造確認・取得
        /// </summary>
        /// <returns></returns>
        private bool GetStructure()
        {
            Transform Offset = _tgt.transform.Find("Obj/Head/Offset").NullCheck("Offset");
            _callPlane = Offset.GetComponentsInChildren<Transform>(true).FirstOrDefault(child => child.name == "CallPlate").gameObject.NullCheck("_callPlane");
            return true;
        }
    }
}