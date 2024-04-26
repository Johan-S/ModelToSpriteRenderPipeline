using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class DebugFine {

   static int log_level = detailed;

   const int err = 1;
   const int warn = 2;
   const int standard = 3;
   const int detailed = 4;
   
   public void SetLogLevelDetailed() {
      log_level = detailed;
   }

   public static void LogError(params object[] msg) {
      if (log_level >= err) {
         DebugEx.LogError(msg);
      }
   }

   public static void LogWarning(params object[] msg) {
      if (log_level >= warn) {
         DebugEx.LogWarning(msg);
      }
   }
   public static void Log(params object[] msg) {
      if (log_level >= standard) {
         DebugEx.Log(msg);
      }
   }

   public static void LogDetailed(params object[] msg) {
      if (log_level >= detailed) {
         DebugEx.Log(msg);
      }
   }
}