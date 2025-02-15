using System;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using nadena.dev.modular_avatar.core;
using VRC.SDKBase;
using com.github.pandrabox.pandravase.runtime;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Security;

namespace com.github.pandrabox.flatsplus.runtime
{
    public class FPOnaka : PandraComponent
    {
        public float Pull = 0.2f;
        public float Spring = 0.1f;
        public float Gravity = 0.1f;
        public float GravityFallOff = 1f;
        public float Immobile = 0.5f;
        public float LimitAngle = 7f;
        public float RadiusTuning = 1f;
    }
}