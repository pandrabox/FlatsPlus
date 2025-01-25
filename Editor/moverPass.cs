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
using static com.github.pandrabox.pandravase.runtime.Util;
using System.Linq;
using VRC.SDK3.Avatars.Components;
using com.github.pandrabox.flatsplus.runtime;


namespace com.github.pandrabox.pandravase.editor
{
    /// <summary>
    /// 任意のPlayableLayerを指定したControllerで置換する
    /// </summary>
    internal class MoverPass : Pass<MoverPass>
    {
        protected override void Execute(BuildContext ctx)
        {
            new MoverMain(ctx.AvatarDescriptor);
        }
    }
    public class MoverMain
    {
        private PandraProject _prj;
        public MoverMain(VRCAvatarDescriptor desc)
        {
            Debug.LogWarning("MoverMain");
            _prj = new PandraProject(desc, "flatsplus", ProjectTypes.VPM);
            // ターゲットの取得
            Mover[] components = _prj.RootObject.GetComponentsInChildren<Mover>(true);
            //なければ終了
            if (components.Length == 0)
            {
                _prj.DebugPrint("NothingToDo");
                return;
            }
            _prj.DebugPrint("SomethingToDo");
            ////重複してたら警告
            //if(components.Length > 1)
            //{
            //    _prj.DebugPrint("Moverが2つ以上存在します。これは予期せぬ事象です", false);
            //}
            //////実処理 (指定のとおりに設定、DefaultフラグをOFF)
            ////foreach (var component in components)
            ////{
            ////    var index = _prj.PlayableIndex(component.LayerType);
            ////    _prj.BaseAnimationLayers[index].animatorController = component.controller;
            ////    _prj.BaseAnimationLayers[index].isDefault = false;
            ////}
        }
    }
}