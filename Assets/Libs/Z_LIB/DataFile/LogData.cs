using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading.Tasks;

public static class LogData {



   public static void PrintLogFile(string name, string content) {
      var path = Application.persistentDataPath;


      List<byte> bytes = new List<byte>();

      foreach (var c in content) {
         int ci = c;
         if (ci >= 0 && ci < 128) bytes.Add((byte)ci);
      }

      byte[] data = bytes.ToArray();

      string file_path = path + "/" + name;
      using (FileStream fs = new FileStream(file_path, FileMode.Create)) {

         fs.Write(data, 0, data.Length);
      }

   }
}