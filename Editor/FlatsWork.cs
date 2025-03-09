using com.github.pandrabox.pandravase.editor;
using com.github.pandrabox.pandravase.runtime;
using System;
using System.Diagnostics;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using static com.github.pandrabox.pandravase.editor.Util;

namespace com.github.pandrabox.flatsplus.editor
{
#if FLATSPLUS_SampleOnly
    public class SampleWork : FlatsWork<FPEmo>
    {
        public SampleWork(FlatsProject prj) : base(prj) { }

        protected override void OnWork()
        {
            base.OnWork();
            LowLevelDebugPrint($"SampleWork Start");
        }
    }
#endif

    public abstract class FlatsWork<T> : FlatsWorkBase where T : PandraComponent
    {
        public FlatsWork(FlatsProject prj, params object[] args) : base(prj, args) { }

        protected T _tgt;

        protected override void GetTgt()
        {
            _tgt = _desc.GetComponentInChildren<T>().NullCheck("_tgt");
        }
    }

    public abstract class FlatsWorkBase
    {
        protected VRCAvatarDescriptor _desc;
        protected FlatsProject _prj;
        protected object[] _args;
        public FlatsWorkBase(FlatsProject prj, params object[] args)
        {
            Stopwatch stopwatch = null;
            try
            {
                PanLog.CurrentClassName = GetType().Name;
                _args = args;
                LowLevelDebugPrint($"Flats Plus Work Started");
                stopwatch = Stopwatch.StartNew();
                _prj = prj.NullCheck("_prj");
                _desc = prj.Descriptor.NullCheck("_desc");
                GetTgt();
                OnConstruct();
                LowLevelDebugPrint($"@@SUCCESS@@ Complete work successfully in {stopwatch?.ElapsedMilliseconds ?? 0} ms", true, LogType.Log);
            }
            catch (Exception ex)
            {
                AppearError(ex);
            }
            finally
            {
                PanLog.CurrentClassName = "";
                if (stopwatch != null && stopwatch.IsRunning) stopwatch.Stop();
            }
        }
        protected virtual void GetTgt() { }
        protected abstract void OnConstruct();
    }
}