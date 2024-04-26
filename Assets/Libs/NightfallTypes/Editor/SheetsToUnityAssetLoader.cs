using System;
using System.CodeDom;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using Object = UnityEngine.Object;

[ScriptedImporter(1, "sheets")]
public class SheetsToUnityAssetLoader : ScriptedImporter {

   bool NightfallHeaderRow(string[] row) {
      var name = row[0];
      if (name == "Unit_Number") return true;
      return name.EndsWith("_ID");
   }
   

   // [CustomEditor(typeof(SheetsToUnityAssetLoader))]
   public class MyEdit : ScriptedImporterEditor {

      public override void OnEnable() {
         base.OnEnable();
         
         var t = (SheetsToUnityAssetLoader)target;

         var file_data = File.ReadAllText(t.assetPath);

         var rows = file_data.SplitLines();

         var rowd = EngineDataInit.ParseGenericSheetRows(rows).ToArray();
         column_count = 0;
         htype_name = "UnknownType";
         header_data = "";
         if (rowd.IsNonEmpty()) {
            htype = rowd[0].htype;
            column_count = rowd[0].hdrs?.Length ?? rowd[0].row.Length;

            htype_name = htype?.Split("_")?.Get(0) ?? "UnknownType";

            old_row_count = rows.Length;
            header_data = GetHeaderText();
         }
      }
      string header_data;

      public static IEnumerable<string> GetHeaderRows(IEnumerable<string> rows) {
         string[] header = null;
         string htype = "";
         foreach (var row_string in rows) {
            if (row_string.Trim().Length == 0) {
               yield return row_string;
               continue;
            }
            if (row_string.FastStartsWith("#")) {
               if (row_string.StartsWith("#hdr\t", out var hdr_row)) {
                  header = hdr_row.Trim().Split("\t").Trim();
                  yield return row_string;
                  continue;
               }

               htype = row_string.Substring(1).Trim();
               header = null;
               yield return row_string;
               continue;
            }

            var row = row_string.Split("\t").Trim();


            if (EngineDataInit.NightfallHeaderRow(row)) {
               var old_htype = htype;
               htype = row[0].Replace("_Number", "_ID").Replace(" ", "_");
               // header = row;
               var new_header = row_string.Trim().Split("\t").Trim();
               if (htype == old_htype && htype == "Animation_Type") {
                  header = header.Zip(new_header, (s, t) => $"{s}\t{t}").ToArray();

               } else {
                  header = new_header;
               }
               
               yield return row_string;
               continue;
            }
            yield break;
         }
      }

      int header_row_count;

      string GetHeaderText() {
         
         var t = (SheetsToUnityAssetLoader)target;

         var file_data = File.ReadAllText(t.assetPath);

         var hdr_rows = GetHeaderRows(file_data.SplitLines());

         header_row_count = hdr_rows.Count();

         return hdr_rows.join("\n") + "\n";
      }

      string htype_name;

      string htype;
      int column_count;

      Texture2D bg;

      string cdata;

      bool sheet_data;

      string[][] rows;

      void ParseCopyData(string cdata) {
         text_to_write = null;
         sheet_data = false;
         rows = null;
         
         if (cdata.Trim().Length == 0) return;


         var tab_lines = cdata.SplitLines().Where(x => x.Trim().Length > 0).ToArray();


         string[][] rowsp = tab_lines.map(x => x.Split("\t"));


         int h = rowsp.Length;
         int w = rowsp[0].Length;
         
         if (rowsp.Any(x => x.Length != w)) return;
         
         if (w < 2) return;


         rows = rowsp;

         sheet_data = true;
         
         
         var cr = rows.map(x => x.join("\t")).ToList();
         text_to_write = "\n" + cr.join("\n") + "\n";

      }

      string text_to_write;

      Font cur_font;

      public string old_full_file_data;

      int old_row_count;

      public override void OnInspectorGUI() {
         base.OnInspectorGUI();
         
         EditorGUILayout.LabelField($"Detected Sheet Type: {htype_name}, with {column_count} columns");

         var t = (SheetsToUnityAssetLoader)target;

         var clip = GUIUtility.systemCopyBuffer;
         if (clip != cdata) {
            cur_font = GUI.skin.font;
            cdata = clip;

            ParseCopyData(cdata);
         }

         bool right_format = sheet_data && rows[0].Length == column_count;
         
         EditorGUI.BeginDisabledGroup(!right_format);

         /*
         if (GUILayout.Button($"Append Clipboard Data with {rows?.Length} rows", GUILayout.Height(30))) {
            old_full_file_data = File.ReadAllText(t.assetPath);
            File.AppendAllText(t.assetPath, text_to_write);
         }
         */
         if (GUILayout.Button($"Replace {old_row_count - header_row_count}  With {rows?.Length} Clipboard Data", GUILayout.Height(30))) {

            
            File.WriteAllText(t.assetPath, header_data + text_to_write);
            
            AssetDatabase.Refresh();

            old_row_count = (header_data + text_to_write).Where(x => x == '\n').Count - header_row_count;

         }
         
         EditorGUI.EndDisabledGroup();

         if (sheet_data) {

            if (!right_format) {
               
               EditorGUILayout.HelpBox(new ($"Found data with {rows[0].Length} columns, but {htype_name} has {column_count} columns!"), MessageType.Warning);
            } else {
               
               EditorGUILayout.HelpBox(new ($"Found data fiting {htype_name} with {column_count} columns!"), MessageType.Info);
            }

            float labe_h = (rows.Length - 1) * (cur_font.lineHeight + 1) +
                           EditorGUIUtility.singleLineHeight;
            EditorGUILayout.LabelField($"Detected sheet data, {rows[0].Length} x {rows.Length}");
            
            
            EditorGUILayout.LabelField(cdata, GUILayout.Height(labe_h));
         } else {
            EditorGUILayout.HelpBox(new ("Copy Data from Google Sheets to start!"), MessageType.Info);
         }
      }
   }
 
   public override void OnImportAsset(AssetImportContext ctx) {
      
      
      

      var data = File.ReadAllText(ctx.assetPath);

      var fn = Regex.Split(ctx.assetPath, @"[./]")[^2];


      TextAsset ta = new TextAsset(data);
      ta.name = fn;
      
      var rows = data.SplitLines(skip_empty:true);
 
      var fl = new LoadDataFlow();
      
      EngineDataInit.AddGenericSheetRows(fl, rows);

      fl.lazy.ForEach(x => x(fl.gear_data));

      var d = fl.gear_data;
      
      var spo = d.magic_spells.map(sp => {
         var sa = new SpellTypeObject();
         Std.CopyShallowDuckTyped(sp, sa);
         sa.name = sp.name;
         return sa;
      });
      
      var weps = d.melee_weapons.map(sp => {
         var sa = new WeaponTypeObject();
         Std.CopyShallowDuckTyped(sp, sa);
         sa.name = sp.name;
         return sa;
      });
      var arm = d.armors.map(sp => {
         var sa = new ArmorTypeObject();
         Std.CopyShallowDuckTyped(sp, sa);
         sa.name = sp.name;
         return sa;
      });
      var sh = d.shields.map(sp => {
         var sa = new ShieldTypeObject();
         Std.CopyShallowDuckTyped(sp, sa);
         sa.name = sp.name;
         return sa;
      });
      var helms = d.helmets.map(sp => {
         var sa = new HelmetTypeObject();
         Std.CopyShallowDuckTyped(sp, sa);
         sa.name = sp.name;
         return sa;
      });
      
      
      var simple_spells = d.simple_spells.map(sp => {
         var sa = new SimpleSpellTypeObject();
         Std.CopyShallowDuckTyped(sp, sa);
         sa.name = sp.name;
         return sa;
      });
      
      var simple_buffs = d.simple_buffs.map(sp => {
         var sa = new SimpleBuffTypeObject();
         Std.CopyShallowDuckTyped(sp, sa);
         sa.name = sp.name;
         return sa;
      });
      
      var animations = d.parsed_animations.map(sp => {
         var sa = new AnimationTypeObject();
         Std.CopyShallowDuckTyped(sp, sa);
         sa.name = $"{sa.animation_type}__{sa.category}";
         return sa;
      });
      var fi = typeof(SimpleUnitTypeObject).GetFields().Where(x => x.FieldType == typeof(string) && !x.IsStatic).ToDictionary(x => x.Name, x => x);
      
      var units = d.units.map(sp => {
         var sa = new SimpleUnitTypeObject();
         var (htype, row, headers) = fl.row_back_map[sp];

         foreach (var (r, h) in row.Zip(headers)) {
            fi.Get(h)?.SetValue(sa, r);
         }
         Std.CopyShallowDuckTyped(sp, sa);
         sa.name = sp.name;
         return sa;
      });

      var simple_container = ScriptableObject.CreateInstance<SimpleUnityTypeContainer>();

      simple_container.name = ta.name;
      simple_container.armor = new(arm);
      simple_container.shield = new(sh);
      simple_container.weapon = new(weps);
      simple_container.helmet = new(helms);
      simple_container.animation = new(animations);
      simple_container.spell = new(spo);
      simple_container.simpleSpell = new(simple_spells);
      simple_container.simpleBuff = new(simple_buffs);
      simple_container.simpleUnit = new(units);
      
      ctx.AddObjectToAsset("simple_container", simple_container);
      
      ctx.AddObjectToAsset("text asset", ta);
      
      ctx.SetMainObject(ta);
      
      foreach (var (i, sp) in spo.enumerate()) {
         ctx.AddObjectToAsset($"spell_{i}", sp);
      }
      
      foreach (var (i, sp) in weps.enumerate()) {
         ctx.AddObjectToAsset($"weapon_{i}", sp);
      }
      
      foreach (var (i, sp) in arm.enumerate()) {
         ctx.AddObjectToAsset($"armor_{i}", sp);
      }
      
      foreach (var (i, sp) in sh.enumerate()) {
         ctx.AddObjectToAsset($"shield_{i}", sp);
      }
      
      foreach (var (i, sp) in helms.enumerate()) {
         ctx.AddObjectToAsset($"helmet_{i}", sp);
      }
      foreach (var (i, sp) in simple_spells.enumerate()) {
         ctx.AddObjectToAsset($"simple_spell_{i}", sp);
      }
      foreach (var (i, sp) in simple_buffs.enumerate()) {
         ctx.AddObjectToAsset($"simple_buff_{i}", sp);
      }
      foreach (var (i, sp) in animations.enumerate()) {
         ctx.AddObjectToAsset($"animation_{i}", sp);
      }
      foreach (var (i, sp) in units.enumerate()) {
         ctx.AddObjectToAsset($"unit_{i}", sp);
      }
   }
}