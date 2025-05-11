using com.github.pandrabox.flatsplus.runtime;
using com.github.pandrabox.pandravase.editor;
using com.github.pandrabox.pandravase.runtime;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEngine;
using static com.github.pandrabox.pandravase.editor.Localizer;
using static com.github.pandrabox.pandravase.editor.Util;


namespace com.github.pandrabox.flatsplus.editor
{
#if PANDRADBG
    public class FPExploreDebug
    {
        [MenuItem("PanDbg/FPExplore")]
        public static void FPExplore_Debug()
        {
            SetDebugMode(true);
            foreach (var a in AllAvatar)
            {
                var fp = new FlatsProject(a);
                new FPExploreWork(fp);
            }
        }
    }
#endif

    public class FPExploreWork : FlatsWork<FPExplore>
    {
        public FPExploreWork(FlatsProject p) : base(p) { }

        private bool _colorDefined = false;

        sealed protected override void OnConstruct()
        {
            CreatePin();
            CreateLine();

            //Lightは別LightWorkで実行
        }

        private void CreatePin()
        {
            if (_config.D_Explore_Pin == false) return;
            Transform pinTransform = _tgt.FindEx("Pin").NullCheck();
            pinTransform.localPosition = new Vector3(0, _prj.PinY, 0);

            var ac = new AnimationClipsBuilder();
            ac.Clip("Color0").Bind("Pin", typeof(MeshRenderer), "material._Hue").Const2F(0);
            ac.Clip("Color1").Bind("Pin", typeof(MeshRenderer), "material._Hue").Const2F(1);
            ac.Clip("PinOff").IsVector3((x) => { x.Bind("Pin", typeof(Transform), "m_LocalScale.@a").Const2F(0); })
                .Bind("Pin", typeof(GameObject), "m_IsActive").Const2F(0);
            ac.Clip("PinOn").IsVector3((x) => { x.Bind("Pin", typeof(Transform), "m_LocalScale.@a").Const2F(999999); })
                .Bind("Pin", typeof(GameObject), "m_IsActive").Const2F(1);

            var bb = new BlendTreeBuilder("Explore");
            bb.RootDBT(() =>
            {
                bb.Param("1").Add1D("FlatsPlus/Explore/SW", () =>
                {
                    bb.Param(0).AddMotion(ac.Outp("PinOff"));
                    bb.Param(1).AddMotion(ac.Outp("PinOn"));
                });
                bb.Param("1").Add1D("FlatsPlus/Explore/ColorRx", () =>
                {
                    bb.Param(0).AddMotion(ac.Outp("Color0"));
                    bb.Param(1 + 1 / 9f).AddMotion(ac.Outp("Color1"));
                });
            });
            bb.Attach(_tgt.gameObject);

            new MenuBuilder(_prj).AddFolder("FlatsPlus", true).Ico("FlatsPlus").AddFolder(L("Menu/Explore"), true).Ico("Explore_Pin")
                .AddToggle("FlatsPlus/Explore/SW", L("Menu/Explore/Pin"), 1, ParameterSyncType.Bool, 0, false).SetMessage(L("Menu/Explore/Pin/Enable"), L("Menu/Explore/Pin/Disable")).Ico("Explore_Pin");

            DefineColor();

        }
        private void CreateLine()
        {
            if (_config.D_Explore_Line == false) return;

            new MenuBuilder(_prj).AddFolder("FlatsPlus", true).Ico("FlatsPlus").AddFolder(L("Menu/Explore"), true).Ico("Explore_Pin")
                .AddToggle("FlatsPlus/Pen/ExploreOverride", L("Menu/Explore/Path"), 1, ParameterSyncType.Bool, 0, false).SetMessage(L("Menu/Explore/Path/Enable"), L("Menu/Explore/Path/Disable")).Ico("Explore_Route")
                .AddButton("FlatsPlus/Pen/Clear", 1, ParameterSyncType.Bool, L("Menu/Explore/Path/Clear")).SetMessage(L("Menu/Explore/Path/Clear/Detail")).Ico("Erace");
            DefineColor();
        }

        private void DefineColor()
        {
            if (_colorDefined) return;
            new MenuBuilder(_prj).AddFolder("FlatsPlus", true).Ico("FlatsPlus").AddFolder(L("Menu/Explore"), true).Ico("Explore_Pin")
                .AddRadial("FlatsPlus/Explore/Color", L("Menu/Explore/Color")).SetMessage(L("Menu/Explore/Color/Detail")).Ico("PenColor");
            _prj.VirtualSync("FlatsPlus/Explore/Color", 3, PVnBitSync.nBitSyncMode.FloatMode, true);
            _colorDefined = true;
        }
    }
}