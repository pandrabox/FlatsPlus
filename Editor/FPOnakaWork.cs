using com.github.pandrabox.flatsplus.runtime;
using com.github.pandrabox.pandravase.editor;
using com.github.pandrabox.pandravase.runtime;
using System.Linq;
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
        private bool _dbCreateMode;

        public FPOnakaWork(FlatsProject fp, bool dbCreateMode = false) : base(fp)
        {
            _dbCreateMode = dbCreateMode;
        }

        /// <summary>
        /// Hips/Hips.001をHips/OnakaBase/TouchArea/Hips.001に組み替える
        /// </summary>
        sealed protected override void OnConstruct()
        {
            ProcessForMainAvator();
            ProcessForClothes();
        }


        /// <summary>
        /// アバターそのものへの処置
        /// </summary>
        private void ProcessForMainAvator()
        {
            Log.I.StartMethod();
            GetObject();
            RemoveLegacyPhysBone();
            (Transform onakaBase, Transform touchArea) = CreateOnakaConstruct(_hips);
            ForDev_DBCreate(touchArea);
            _hips001.SetParent(touchArea, true);
            ApplyHips001(onakaBase);
            Log.I.EndMethod();
        }

        /// <summary>
        /// 衣装対応
        /// </summary>
        private void ProcessForClothes()
        {
            Log.I.StartMethod();
            if(_hips==null || _hips001 == null) return;
            var clothesHips001 = _prj.RootObject.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Hips.001" && t != _hips001).ToList();
            foreach(var cHips001 in clothesHips001)
            {
                var cHips = cHips001.parent;
                if (cHips.name != "Hips")
                {
                    Log.I.Warning($"Hips.001の親がHipsではありません。{cHips.name}になっています。");
                    continue;
                }
                _hips = cHips;
                _hips001 = cHips001;
                RemoveLegacyPhysBone();
                (Transform onakaBase, Transform touchArea) = CreateOnakaConstruct(_hips);
                _hips001.SetParent(touchArea, true);

                Transform referenceInfo = _hips?.parent?.parent;
                if (referenceInfo != null)
                {
                    Log.I.Info($@"衣装{referenceInfo.name}のOnakaを処理しました");
                }
            }
            Log.I.EndMethod();
        }

        private void GetObject()
        {
            _hips = _prj.HumanoidTransform(HumanBodyBones.Hips).NullCheck();
            _hips001 = _hips.Find("Hips.001").NullCheck();
        }

        private void RemoveLegacyPhysBone()
        {
            VRCPhysBone legacyPB = _hips001.GetComponent<VRCPhysBone>();
            if (!_dbCreateMode && legacyPB != null)
            {
                GameObject.DestroyImmediate(legacyPB);
            }
        }

        private (Transform, Transform) CreateOnakaConstruct(Transform Hips)
        {
            //OnakaBaseの作成
            Transform onakaBase;
            onakaBase = new GameObject("OnakaBase").transform;
            onakaBase.SetParent(_hips);
            onakaBase.localPosition = new Vector3(0, _prj.OnakaY1, _prj.OnakaZ1);
            onakaBase.localEulerAngles = Vector3.zero;
            onakaBase.localScale = Vector3.one;

            //TouchAreaの作成
            Transform touchArea;
            touchArea = new GameObject("TouchArea").transform;
            touchArea.SetParent(onakaBase);
            touchArea.localPosition = new Vector3(0, _prj.OnakaY2, _prj.OnakaZ2);
            touchArea.localEulerAngles = Vector3.zero;
            touchArea.localScale = Vector3.one;

            return (onakaBase, touchArea);
        }

        private void ForDev_DBCreate(Transform touchArea)
        {

#if PANDRADBG
            if (_dbCreateMode)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.SetParent(touchArea, false);
                sphere.transform.localScale = Vector3.one * _prj.OnakaRadius * 2;
                //SphereのサイズはOnakaRadiusの2倍になるので注意（例えば1.8でちょうどいいならDBは0.9を入れる）
                sphere.GetComponent<Renderer>().material = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.github.pandrabox.flatsplus/Assets/Onaka/res/DevMat.mat");
                PositionConstraint positionConstraint = sphere.AddComponent<PositionConstraint>();
                positionConstraint.constraintActive = false;
                ConstraintSource source = new ConstraintSource { sourceTransform = touchArea, weight = 1f };
                positionConstraint.AddSource(source);
                positionConstraint.translationAtRest = Vector3.zero;
                positionConstraint.locked = true;
                positionConstraint.constraintActive = true;
                return;
            }
#endif
        }
        private void ApplyHips001(Transform onakaBase)
        {
            var pb = onakaBase.gameObject.AddComponent<VRCPhysBone>();
            pb.pull = _config.D_Onaka_Pull;
            pb.spring = _config.D_Onaka_Spring;
            pb.gravity = _config.D_Onaka_Gravity;
            pb.gravityFalloff = _config.D_Onaka_GravityFallOff;
            pb.immobile = _config.D_Onaka_Immobile;
            pb.limitType = VRCPhysBoneBase.LimitType.Angle;
            pb.maxAngleX = _config.D_Onaka_LimitAngle;
            pb.radius = _prj.OnakaRadius * _config.D_Onaka_RadiusTuning;
            pb.radiusCurve = new AnimationCurve(new Keyframe(_prj.OnakaCurveTop - .1f, 0), new Keyframe(_prj.OnakaCurveTop, 1), new Keyframe(_prj.OnakaCurveTop + .1f, 0));
            pb.allowGrabbing = VRCPhysBoneBase.AdvancedBool.False;
            pb.allowPosing = VRCPhysBoneBase.AdvancedBool.False;
            pb.immobileType = VRCPhysBoneBase.ImmobileType.World;
        }
    }
}