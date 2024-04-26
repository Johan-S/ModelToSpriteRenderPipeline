using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public class ProjectMigrationScripts {
   static string root_path => "Assets/GeneratedAssets/Resources/AtlasGen";

   
   [InitializeOnLoadMethod]
   public static void MigrateAtlas() {
      var full_path = root_path + "/atlas_meta.txt";

      string prev = "Demonolist";
      string res = "Demonologist";
      ReplaceAssetFile(full_path, prev, $"^{prev}\\b", res);
   }

   static void ReplaceAssetFile(string full_path, string prev, string regex, string res) {
      
      if (!File.Exists(full_path)) return;

      var txt = File.ReadAllText(full_path);
      if (!txt.Contains(prev)) return;

      string pattern_in = regex;

      var mc = Regex.Matches(txt, pattern_in, RegexOptions.Multiline);

      Debug.Log($"Trying to migrate {prev} -> {res}...");

      if (mc.Count > 0) {
         var rt = Regex.Replace(txt, pattern_in, res, RegexOptions.Multiline);
         if (!rt.Contains(prev)) {
            File.WriteAllText(full_path, rt);

            Debug.Log($"Migrated {mc.Count} spelling errors {prev} -> {res}");

            txt = rt;
         }
      }

      if (txt.Contains(prev)) {
         Debug.LogError($"Bad regex {mc.Count}: {pattern_in}, there are still cases of {prev}");
      }
   }
}