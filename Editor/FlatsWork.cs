using UnityEditor;
using nadena.dev.modular_avatar.core;
using UnityEngine;
using UnityEditor.Animations;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using nadena.dev.ndmf.util;
using nadena.dev.ndmf;
using com.github.pandrabox.pandravase.runtime;
using static com.github.pandrabox.pandravase.editor.Util;
using System.Linq;
using VRC.SDK3.Avatars.Components;
using com.github.pandrabox.flatsplus.runtime;
using static com.github.pandrabox.flatsplus.editor.Global;
using static com.github.pandrabox.pandravase.editor.TextureUtil;
using System.Text.RegularExpressions;
using com.github.pandrabox.pandravase.editor;
using System.Diagnostics;
using System.Globalization;

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
            _tgt = _desc.GetComponentInChildren<T>().NullCheck();
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
                _prj = prj.NullCheck();
                _desc = prj.Descriptor.NullCheck();
                GetTgt();
                OnConstruct();
                LowLevelDebugPrint($"Complete work successfully in {stopwatch?.ElapsedMilliseconds ?? 0} ms", true, LogType.Log);
            }
            catch (Exception ex)
            {
                string stackTrace = ex.StackTrace.Replace(" at ", "\n   at ");
                LowLevelDebugPrint($"Failed work due to an error: {ex.Message} in {stopwatch?.ElapsedMilliseconds ?? 0} ms at {ex.StackTrace}　Exception Details: {ex.ToString()}", true, LogType.Error);
               
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