﻿using System;

namespace FUtility
{
    public class Log
    {
        public static string modName = typeof(Log).Assembly.GetName().Name.Replace("Merged", "");
        private static string prefix = $"[{modName}]: ";

        public static void SetName(string name)
        {
            prefix = $"[{name}]: ";
        }

        public static void PrintVersion()
        {
            var v = typeof(Log).Assembly.GetName().Version.ToString();
            Info($"Loaded version {v}");
        }

        public static void Info(object arg)
        {
            try
            {
                Debug.Log(prefix + arg.ToString());
            }
            catch (Exception e)
            {
                Warn(e);
            }
        }

        public static void Warning(object arg)
        {
            try
            {
                Debug.LogWarning(prefix + arg.ToString());
            }
            catch (Exception e)
            {
                Warn(e);
            }
        }

        public static void Debuglog(object arg)
        {
#if DEBUG
            try
            {
                Debug.Log(prefix + " (debug) " + arg.ToString());
            }
            catch (Exception e)
            {
                Warn(e);
            }
#endif
        }

        public static void Error(object arg)
        {
            try
            {
                Debug.LogError(prefix + arg.ToString());
            }
            catch (Exception e)
            {
                Warn(e);
            }
        }

        private static void Warn(Exception e)
        {
        }
    }
}