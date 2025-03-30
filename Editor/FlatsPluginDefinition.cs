using com.github.pandrabox.flatsplus.editor;
using nadena.dev.ndmf;
using nadena.dev.ndmf.fluent;
using static com.github.pandrabox.pandravase.editor.Util;

[assembly: ExportsPlugin(typeof(FlatsPluginDefinition))]

namespace com.github.pandrabox.flatsplus.editor
{
    internal class FlatsPluginDefinition : Plugin<FlatsPluginDefinition>
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
            seq.Run(FlatsPlusPass.Instance);
        }
    }
}
