#if UNITY_EDITOR

using com.github.pandrabox.pandravase.editor;
using com.github.pandrabox.pandravase.runtime;
using UnityEngine;

namespace com.github.pandrabox.flatsplus.runtime
{
    public class FPMultiTool : PandraComponent
    {
        public void SetBone(string name, Transform tgt)
        {
            var multiObj = this.transform.Find($@"MultiTool/Root/{name}").NullCheck($@"MultiTool_SetBone_GetmultiObj_{name}");
            multiObj.SetParent(tgt, false);
            multiObj.localScale = Vector3.one;
            multiObj.localPosition = Vector3.zero;
            multiObj.localEulerAngles = new Vector3(90f, 0, 0);
        }
        public GameObject MultiMeshObj => this.transform.Find("MultiTool/MultiMesh")?.gameObject.NullCheck("MultiTool_GetMultiMeshObj");
        public SkinnedMeshRenderer MultiMeshSMR => MultiMeshObj.GetComponent<SkinnedMeshRenderer>().NullCheck("MultiTool_GetMultiMeshSMR");
    }
}

#endif