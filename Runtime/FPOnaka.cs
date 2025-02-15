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
        public float Pull = 0.5f;
        public float Spring = 0.9f;
        public float Gravity = 0.2f;
        public float GravityFallOff = 1f;
        public float Immobile = 0.7f;
        public float LimitAngle = 20f;
        public float RadiusTuning = 1f;
    }
}