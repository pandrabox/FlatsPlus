using System.IO;
using VRC.SDK3.Avatars.Components;

namespace com.github.pandrabox.flatsplus.editor
{
    public enum AvatarType
    {
        Undef, Flat, Flat2, Comodo, Fel, Heon, Kewf
    }
    public static class Global
    {
        public const string PRJNAME = "FlatsPlus";
        public const string FlatsPlusPath = "Packages/com.github.pandrabox.flatsplus";
        public const string FlatsPlusEditorPath = "Packages/com.github.pandrabox.flatsplus/Editor";
        public const string FlatsPlusRuntimePath = "Packages/com.github.pandrabox.flatsplus/Runtime";
        public const string FlatsPlusAssetsPath = "Packages/com.github.pandrabox.flatsplus/Assets";
        public const string LogFilePath = "Packages/com.github.pandrabox.flatsplus/Log/log.txt";
        public static StreamWriter FPStreamWriter;




        public enum FaceType
        {
            Mouth,
            Eye,
            Other,
            Ignore
        }

        public static FlatsProject FP(this VRCAvatarDescriptor desc)
        {
            return new FlatsProject(desc);
        }

        enum FlatsPlusFunction
        {
            Carry,
            DanceController,
            Emo,
            Explore,
            Hoppe,
            Ico,
            Light,
            MakeEmo,
            MeshSetting,
            Move,
            Onaka,
            Pen,
            Sleep,
            Tail,
            Link,
            Sync
        }
    }
}
