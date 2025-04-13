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
        Texture2D[] _icosDoNotUseDirectly; //直接読まない！Icosから使って下さい
        Texture2D _versionTex;

        sealed protected override void OnConstruct()
        {
            GetMultiTool();
            GetStructure();
            GetVersionTexture();
            CreateTexture();
            //ReplacePackTexture();
            CreateMenu();
            CreateDBT();
        }
        private void GetMultiTool()
        {
            var multiTool = _prj.Descriptor.GetComponentInChildren<FPMultiTool>().NullCheck("MultiTool");
            var pos = _tgt.transform.Find("Obj/Head/Offset/CallPlate/Size10").NullCheck("CallPlate");
            multiTool.SetBone("Ico", pos);
        }
        /// <summary>
        /// _config.Ico_Texturesは度々nullになるので安全に取得する
        /// </summary>
        public Texture2D[] Icos
        {
            get
            {
                //正しく取得できている場合はそのまま返す
                if (_icosDoNotUseDirectly != null)
                {
                    if (_icosDoNotUseDirectly.All(t => t != null))
                    {
                        return _icosDoNotUseDirectly;
                    }
                }

                _icosDoNotUseDirectly = new Texture2D[8];

                try
                {
                    // _configから読める分を読む
                    if (!(_config == null || _config.Ico_Textures == null))
                    {
                        for (int i = 0; i < Math.Min(_icosDoNotUseDirectly.Length, _config.Ico_Textures.Length); i++)
                        {
                            _icosDoNotUseDirectly[i] = _config.Ico_Textures[i];
                        }
                    }

                    // ない分をデフォルトから読み込み
                    for (int i = 0; i < _icosDoNotUseDirectly.Length; i++)
                    {
                        if (_icosDoNotUseDirectly[i] == null)
                        {
                            string path = $"Packages/com.github.pandrabox.flatsplus/Assets/Ico/Ico/i{i + 1}.png";
                            _icosDoNotUseDirectly[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

                            if (_icosDoNotUseDirectly[i] == null) //まずあり得ないが念のためのチェック
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

                return _icosDoNotUseDirectly;
            }
        }
        private void GetVersionTexture()
        {
            using (var capture = new PanCapture(BGColor: new Color(1, 1, 1, 1), margin: 10, padding: 10, width: 170))
            {
                FlatsProject prj = new FlatsProject(_desc);
                _versionTex = capture.TextToImage($"{prj.ProjectName}\n\r{prj.VPMVersion}");
            }
            //_prj.DebugOutp(_versionTex);
        }
        private void CreateTexture()
        {
            Log.I.StartMethod();
            //追加するアイコンの定義
            List<Texture2D> textures = new List<Texture2D>(Icos);
            textures.RemoveAt(textures.Count - 1);
            textures.Add(_versionTex);

            //追加元テクスチャの取得
            FPMultiTool multiTool = _prj.Descriptor.GetComponentInChildren<FPMultiTool>().NullCheck("MultiTool");
            SkinnedMeshRenderer smr = multiTool.MultiMeshSMR;
            Texture2D multiTexture = smr.material.mainTexture as Texture2D;

            //アイコンをパックして戻す
            Texture2D packedTexture = PackTexture(multiTexture, textures, new Vector2(1024,1024), new Vector2(170,170), 5);
            smr.material.mainTexture = packedTexture;
            
            //_prj.DebugOutp(packedTexture);
        }
        private Texture2D PackTexture(Texture2D original, List<Texture2D> icos, Vector2 originalSize, Vector2 icoSize, int turnAroundCount)
        {
            Log.I.StartMethod();
            // 新しいテクスチャを作成
            Texture2D result = new Texture2D((int)originalSize.x, (int)originalSize.y, TextureFormat.RGBA32, false);

            // 元のテクスチャの内容をコピー
            Color[] originalPixels = original.GetPixels();
            result.SetPixels(originalPixels);

            // アイコンを配置する
            for (int i = 0; i < icos.Count; i++)
            {
                // アイコンがnullの場合はスキップ
                if (icos[i] == null)
                    continue;

                // アイコンの位置を計算
                int x, y;

                if (i < turnAroundCount)
                {
                    // 最初のturnAroundCount個は縦に並べる
                    x = 0;
                    y = i;
                }
                else
                {
                    // それ以降は右に進んで上に積み上げる
                    x = (i - turnAroundCount) / turnAroundCount + 1;
                    y = (i - turnAroundCount) % turnAroundCount;
                }

                Log.I.Info($@"Icon {i}: x={x}, y={y} ({icos[i].width}x{icos[i].height})");

                // アイコンのピクセル座標
                int pixelX = x * (int)icoSize.x;
                int pixelY = y * (int)icoSize.y;

                // テクスチャを読み込む
                Texture2D resizedIco = ResizeTexture(icos[i], (int)icoSize.x, (int)icoSize.y);
                Color[] icoPixels = resizedIco.GetPixels();

                // アイコンを描画（左下から配置）
                for (int icoY = 0; icoY < (int)icoSize.y; icoY++)
                {
                    for (int icoX = 0; icoX < (int)icoSize.x; icoX++)
                    {
                        // 元のテクスチャの対応する位置（左下からの座標）
                        int targetX = pixelX + icoX;
                        int targetY = pixelY + icoY;

                        // 範囲チェック
                        if (targetX >= 0 && targetX < originalSize.x && targetY >= 0 && targetY < originalSize.y)
                        {
                            // アイコンのピクセルを取得
                            int icoPixelIndex = icoY * (int)icoSize.x + icoX;
                            Color icoPixel = icoPixels[icoPixelIndex];

                            // 元のテクスチャの対応するピクセル位置
                            int targetPixelIndex = targetY * (int)originalSize.x + targetX;

                            // ピクセルを置換（RGBAすべて置換）
                            if (targetPixelIndex < originalPixels.Length)
                            {
                                result.SetPixel(targetX, targetY, icoPixel);
                            }
                        }
                    }
                }
            }

            // テクスチャの変更を適用
            result.Apply();
            return result;
        }
        //private void ReplacePackTexture()
        //{
        //    using (var capture = new PanCapture(BGColor: new Color(1, 1, 1, 1), margin: 10, padding: 10, width: 170))
        //    {
        //        FlatsProject prj = new FlatsProject(_desc);
        //        var strImg = capture.TextToImage($"{prj.ProjectName}\n\r{prj.VPMVersion}");

        //        List<Texture2D> textures = new List<Texture2D>(Icos);

        //        textures.Add(strImg);
        //        Texture2D packTexture = PackTexture(textures, 3, 170 * 3);
        //        try
        //        {
        //            _callPlane.GetComponent<Renderer>().material.mainTexture = packTexture;
        //        }
        //        catch (Exception ex)
        //        {
        //            Log.I.Exception(ex, "Textureの置換に失敗しました(設定先mainTextureの取得に失敗しました)");
        //        }
        //    }
        //}
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
        private void CreateDBT()
        {
            BlendTreeBuilder bb = new BlendTreeBuilder("IcoControl");
            AnimationClipsBuilder ac = new AnimationClipsBuilder();
            ac.Clip("OffPos")
                .Bind("Obj/Head/Offset/CallPlate/Size10", typeof(Transform), "m_LocalPosition.x").Const2F(0)
                .Bind("Obj/Head/Offset/CallPlate/Size10", typeof(Transform), "m_LocalPosition.y").Const2F(-3.75f)
                .Bind("Obj/Head/Offset/CallPlate/Size10", typeof(Transform), "m_LocalPosition.z").Const2F(.03289148f);
            ac.Clip("OnPos")
                .Bind("Obj/Head/Offset/CallPlate/Size10", typeof(Transform), "m_LocalPosition.x").Const2F(0)
                .Bind("Obj/Head/Offset/CallPlate/Size10", typeof(Transform), "m_LocalPosition.y").Const2F(0)
                .Bind("Obj/Head/Offset/CallPlate/Size10", typeof(Transform), "m_LocalPosition.z").Const2F(0);
            bb.RootDBT(() =>
            {
                bb.NName("RestoreIcoNo").Param("1").AddD(() =>
                {
                    bb.Param("FlatsPlus/Ico/IcoTypeB2").AddAAP("FlatsPlus/Ico/RestoredIcoNo", 4);
                    bb.Param("FlatsPlus/Ico/IcoTypeB1").AddAAP("FlatsPlus/Ico/RestoredIcoNo", 2);
                    bb.Param("FlatsPlus/Ico/IcoTypeB0").AddAAP("FlatsPlus/Ico/RestoredIcoNo", 1);
                });
                bb.NName("AppearIco").Param("1").Add1D("FlatPlus/Ico/LocalTypeB0", () => {
                    bb.Param(0).Add1D("FlatsPlus/Ico/RestoredIcoNo", () => {
                        for (int i = 0; i < 8; i++)
                        {
                            bb.Param(i).AddMotion(AnimIcoEnable(i));
                        }
                    });
                    bb.Param(1).Add1D("FlatPlus/Ico/LocalTypeB1", () =>
                    {
                        bb.Param(0).AddMotion(AnimIcoEnable(7));
                        bb.Param(1).AddMotion(AnimIcoEnable(8));
                    });
                });
                bb.NName("PosControl").Param("1").Add1D("FlatPlus/Ico/LocalTypeB0", () =>
                {
                    bb.Param(0).Add1D("FlatsPlus/Ico/RestoredIcoNo", () => {
                        bb.Param(0).AddMotion(ac.Outp("OffPos"));
                        bb.Param(1).AddMotion(ac.Outp("OnPos"));
                    });
                    bb.Param(1).AddMotion(ac.Outp("OnPos"));
                });
            });
            bb.Attach(_tgt.gameObject);
        }
        private AnimationClip AnimIcoEnable(int n)
        {
            string icoName = $"i{n}";
            AnimationClipBuilder ac = new AnimationClipBuilder(icoName);
            for (int i = 1; i < 9; i++)
            {
                ac.Bind("", typeof(Animator),FPMultiToolWork.GetParamName($"i{i}")).Const2F(i == n ? 0 : 1);
            }
            return ac.Outp();
        }
    }
}