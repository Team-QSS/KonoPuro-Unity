﻿using _root.Script.Utils.SingleTon;
using UnityEngine;

namespace _root.Script.Utils
{
    public class Debugger : SingleMono<Debugger>
    {
        [SerializeField] private bool debug = true;

        private void Start()
        {
            Application.runInBackground = true;
        }

        public static void Log(object msg)
        {
            if (Instance.debug) Debug.unityLogger.Log(msg);
        }

        public static void Error(string tag, object msg)
        {
            if (Instance.debug) Debug.unityLogger.LogError(tag, msg);
        }

        public static bool IsDebug()
        {
            return Instance.debug;
        }
    }
}