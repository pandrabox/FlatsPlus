using System;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using nadena.dev.modular_avatar.core;
using VRC.SDKBase;
using com.github.pandrabox.pandravase.runtime;

namespace com.github.pandrabox.flatsplus.runtime
{
    public class FPTail : PandraComponent
    {
        public float SwingPeriod = 1.5f;
        public float SwingAngle = 60;
        public float SizeMax = 1;
        public float SizeMin = 0.01f;
        public bool SizePerfectSync = false;
    }
}