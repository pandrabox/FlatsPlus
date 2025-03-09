using com.github.pandrabox.flatsplus.runtime;
using com.github.pandrabox.pandravase.editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.PhysBone.Components;
using static com.github.pandrabox.pandravase.editor.Util;


namespace com.github.pandrabox.flatsplus.editor
{
#if PANDRADBG
    public class FPOnakaDebug
    {
        [MenuItem("PanDbg/FPOnaka")]
        public static void FPOnaka_Debug()
        {
            SetDebugMode(true);
            foreach (var a in AllAvatar)
            {
                var fp = new FlatsProject(a);
                new FPOnakaWork(fp);
            }
        }
        [MenuItem("PanDbg/FPOnaka_DBCreate")]
        public static void FPOnaka_Debug_DBCreate()
        {
            SetDebugMode(true);
            foreach (var a in AllAvatar)
            {
                var fp = new FlatsProject(a);
                new FPOnakaWork(fp, true);
            }
        }
    }
#endif
    public class FPOnakaWork : FlatsWork<FPOnaka>
    {
        public Transform _hips;
        public Transform _hips001;
        public Transform _onakaBase;
        public Transform _touchArea;
        private bool _dbCreateMode;

        public FPOnakaWork(FlatsProject fp, bool dbCreateMode = false) : base(fp)
        {
            _dbCreateMode = dbCreateMode;
        }

        private void GetObject()
        {
            _hips = _prj.HumanoidTransform(HumanBodyBones.Hips).NullCheck();
            _hips001 = _hips.Find("Hips.001").NullCheck();
            _onakaBase = new GameObject("OnakaBase").transform;
            _onakaBase.SetParent(_hips);
            _touchArea = new GameObject("TouchArea").transform;
            _touchArea.SetParent(_onakaBase);
        }

        sealed protected override void OnConstruct()
        {
            GetObject();
            VRCPhysBone legacyPB = _hips001.GetComponent<VRCPhysBone>();
            if (!_dbCreateMode && legacyPB != null) { GameObject.DestroyImmediate(legacyPB); }

            //OnakaBaseの作成
            _onakaBase.localPosition = new Vector3(0, _prj.OnakaY1, _prj.OnakaZ1);
            _onakaBase.localEulerAngles = Vector3.zero;
            _onakaBase.localScale = Vector3.one;

            //TouchAreaの作成
            _touchArea.localPosition = new Vector3(0, _prj.OnakaY2, _prj.OnakaZ2);
            _touchArea.localEulerAngles = Vector3.zero;
            _touchArea.localScale = Vector3.one;

#if PANDRADBG
            if (_dbCreateMode)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.SetParent(_touchArea, false);
                sphere.transform.localScale = Vector3.one * _prj.OnakaRadius * 2;
                //SphereのサイズはOnakaRadiusの2倍になるので注意（例えば1.8でちょうどいいならDBは0.9を入れる）
                sphere.GetComponent<Renderer>().material = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.github.pandrabox.flatsplus/Assets/Onaka/res/DevMat.mat");
                PositionConstraint positionConstraint = sphere.AddComponent<PositionConstraint>();
                positionConstraint.constraintActive = false;
                ConstraintSource source = new ConstraintSource { sourceTransform = _touchArea, weight = 1f };
                positionConstraint.AddSource(source);
                positionConstraint.translationAtRest = Vector3.zero;
                positionConstraint.locked = true;
                positionConstraint.constraintActive = true;
                return;
            }
#endif

            _hips001.SetParent(_touchArea, true);
            var pb = _onakaBase.gameObject.AddComponent<VRCPhysBone>();
            pb.pull = _tgt.Pull;
            pb.spring = _tgt.Spring;
            pb.gravity = _tgt.Gravity;
            pb.gravityFalloff = _tgt.GravityFallOff;
            pb.immobile = _tgt.Immobile;
            pb.limitType = VRCPhysBoneBase.LimitType.Angle;
            pb.maxAngleX = _tgt.LimitAngle;
            pb.radius = _prj.OnakaRadius * _tgt.RadiusTuning;
            pb.radiusCurve = new AnimationCurve(new Keyframe(_prj.OnakaCurveTop - .1f, 0), new Keyframe(_prj.OnakaCurveTop, 1), new Keyframe(_prj.OnakaCurveTop + .1f, 0));
            pb.allowGrabbing = VRCPhysBoneBase.AdvancedBool.False;
            pb.allowPosing = VRCPhysBoneBase.AdvancedBool.False;
            pb.immobileType = VRCPhysBoneBase.ImmobileType.World;
        }
    }
}