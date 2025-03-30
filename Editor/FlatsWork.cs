using com.github.pandrabox.flatsplus.runtime;
using com.github.pandrabox.pandravase.editor;
using com.github.pandrabox.pandravase.runtime;
using System;
using System.Diagnostics;
using VRC.SDK3.Avatars.Components;


namespace com.github.pandrabox.flatsplus.editor
{
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
        /// <summary>
        /// FlatsPlusオブジェクトにおける設定値
        /// </summary>
        protected FlatsPlus _config;
        protected object[] _args;
        public FlatsWorkBase(FlatsProject prj, params object[] args)
        {
            Log.I.SetKeyWord(GetType().Name);
            Log.I.StartMethod($@"@@START@@{GetType().Name}を開始します。");
            Stopwatch stopwatch = null;
            try
            {
                _args = args;
                stopwatch = Stopwatch.StartNew();
                _prj = prj.NullCheck("_prj");
                _config = prj.Config.NullCheck("_config");
                _desc = prj.Descriptor.NullCheck("_desc");
                GetTgt();
                OnConstruct();
                Log.I.EndMethod($"@@SUCCESS@@ Complete work successfully in {stopwatch?.ElapsedMilliseconds ?? 0} ms");
            }
            catch (Exception ex)
            {
                Log.I.Exception(ex);
            }
            finally
            {
                if (stopwatch != null && stopwatch.IsRunning) stopwatch.Stop();
            }
            Log.I.EndMethod();
            Log.I.ReleaseKeyWord();
        }
        protected virtual void GetTgt() { }
        protected abstract void OnConstruct();
    }
}