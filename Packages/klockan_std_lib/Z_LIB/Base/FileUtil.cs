using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class FileUtil {

   public static bool Exists(string path) {
      return File.Exists(path);
   }

   public static IEnumerable<string> GetFilesRecursively(string path, string pattern) {
      foreach (var f in System.IO.Directory.GetFiles(path, pattern)) yield return f;
      foreach (var d in System.IO.Directory.GetDirectories(path)) foreach (var f in GetFilesRecursively(d, pattern)) yield return f;
   }

   public static IEnumerable<string> GetDirectoriesRecursively(string path) {
      yield return path;
      foreach (var d in System.IO.Directory.GetDirectories(path)) {
         foreach (var f in GetDirectoriesRecursively(d)) yield return f;
      }
   }
}