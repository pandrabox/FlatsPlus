using com.github.pandrabox.flatsplus.editor;
using com.github.pandrabox.pandravase.editor;
using nadena.dev.ndmf;
using nadena.dev.ndmf.fluent;
using static com.github.pandrabox.pandravase.editor.Util;

[assembly: ExportsPlugin(typeof(PanPluginDefinition))]

namespace com.github.pandrabox.flatsplus.editor
{
    internal class PanPluginDefinition : Plugin<PanPluginDefinition>
    {
        public override string DisplayName => "FlatsPlus";
        public override string QualifiedName => "com.github.pandrabox.flatsplus";

        protected override void Configure()
        {
#if PANDRADBG
            SetDebugMode(true);
#endif
            Sequence seq;
            seq = InPhase(BuildPhase.Transforming).BeforePlugin("com.github.pandrabox.pandravase");
            seq.Run(FPHoppePass.Instance);
            seq.Run(FlatsPlusIcoPass.Instance);
            seq.Run(FlatsPlusMeshSettingPass.Instance);
            seq.Run(FlatsPlusLightPass.Instance);
            seq.Run(FPTailPass.Instance);
            seq.Run(FPOnakaPass.Instance);
            seq.Run(FPExplorePass.Instance);
            seq.Run(FPPenPass.Instance);
            seq.Run(FPEmoPass.Instance);
            seq.Run(FPSleepPass.Instance);
            seq.Run(FPMakeEmoPass.Instance);
            seq = InPhase(BuildPhase.Transforming).BeforePlugin("nadena.dev.modular-avatar");
        }
    }
}
