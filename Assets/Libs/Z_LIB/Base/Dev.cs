using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

public static class Dev {

   public enum LogLevel {
       INFO,
       STANDARD,
       WARNING,
       ERROR,
   }

   public static LogLevel cur_level = LogLevel.INFO;

   public static void LogInfo(string t, params object[] o) {
      if (cur_level <= LogLevel.INFO) {
         Debug.LogFormat(t, o);
      }
   }
   public static void Log(string t, params object[] o) {
      if (cur_level <= LogLevel.STANDARD) {
         Debug.LogFormat(t, o);
      }
   }
   public static void LogWarning(string t, params object[] o) {
      if (cur_level <= LogLevel.WARNING) {
         Debug.LogFormat(t, o);
      }
   }
   public static void LogError(string t, params object[] o) {
      if (cur_level <= LogLevel.ERROR) {
         Debug.LogFormat(t, o);
      }
   }
}