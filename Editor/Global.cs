using com.github.pandrabox.pandravase.editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRC.SDK3.Avatars.Components;

namespace com.github.pandrabox.flatsplus.editor
{
    public static class Global
    {
        public const string PROJECT_NAME = "FlatsPlus";
        public const ProjectTypes PROJECT_TYPE = ProjectTypes.VPM;
        public static PandraProject FlatsPlusProject(VRCAvatarDescriptor desc) => new PandraProject(desc, PROJECT_NAME, PROJECT_TYPE);
        public enum AvatarType
        {
            Undef, Flat, Flat2, Comodo, Fel, Heon, Kewf
        }
    }
}
