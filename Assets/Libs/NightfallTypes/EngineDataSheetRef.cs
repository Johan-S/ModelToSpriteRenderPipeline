using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class EngineDataSheetRef : MonoBehaviour, IEngineDataPart {
   public TextAsset sheet;

   public int name_row_id;


   public int n_columns_after = 4;

   public void AddTypesTo(LoadDataFlow flow) {
      if (sheet == null) {
         Debug.LogError($"Sheet not set in {this}", this);
         return;
      }

      var lines = sheet.GetLines();
      EngineDataInit.AddGenericSheetRows(flow, lines);
   }

#if UNITY_EDITOR

   [CustomEditor(typeof(EngineDataSheetRef))]
   public class DisplayParsedRowsEditor : UnityEditor.Editor {

      public class ParsedData {
         public string type;

         public string name;

         public string[] stats;
      }
      [Serializable]
      public class ParsedSubData {
         public string type;

         public List<string> name = new();

         public string row_num_str;

         public string name_str;
         
         public List<string>[] stat_list;

         public string[] stat_list_strs;
      }

      public ParsedSubData[] parsed_names;

      public EngineDataSheetRef t;
      public TextAsset sheet;
      public int last_rowid;

      public int last_n_columns_after;


      public long last_len;

      void MaybeUpdate() {
         sheet = t.sheet;
         last_n_columns_after = t.n_columns_after;
         last_rowid = t.name_row_id;
         if (!sheet) {
            last_len = 0;
            parsed_names = new ParsedSubData[0];
            return;
         }


         var parsed = EngineDataInit.ParseGenericSheetRows(sheet.GetLines()).ToArray();

         int max_col = parsed[0].row.Length;
         
         int cafter = Mathf.Min(last_n_columns_after, max_col -  last_rowid);
         int stat_range_s = last_rowid + 1;
         int stat_range_e = last_rowid + 1 + cafter;
         last_len = t.sheet.dataSize;
         
         var pn = parsed.map(x => new ParsedData{type = x.htype, name = x.row.Get(t.name_row_id, ""), stats = x.row[stat_range_s..stat_range_e]}).filter(x => x.name.Length > 0).Group(x => x.type).Sorted(x => x.Key);

         parsed_names = pn.map(x => new ParsedSubData { type = x.Key, name = x.Value.Map(s => s.name), stat_list = cafter.times(i => x.Value.Map(s => s.stats[i])).ToArray()});

         foreach (var p in parsed_names) {
            p.row_num_str = p.name.enumerate().join("\n", x => x.i.ToString());
            p.name_str = p.name.join("\n");
            p.stat_list_strs = p.stat_list.map(x => x.join("\n"));
         }
      }

      public override void OnInspectorGUI() {
         t = (EngineDataSheetRef)target;
         if (!sheet || t.sheet != sheet || t.name_row_id != last_rowid || last_len != t.sheet.dataSize || t.n_columns_after != last_n_columns_after) {
            MaybeUpdate();
         }

         base.OnInspectorGUI();


         foreach (var pn in parsed_names) {
            EditorGUILayout.LabelField($"Parsed {pn.type}", EditorStyles.boldLabel);

            var names = pn.name;

            var cur_font = GUI.skin.font;

            float labe_h = (names.Count - 1) * (cur_font.lineHeight + 1) +
                         EditorGUIUtility.singleLineHeight;
            
            using (new EditorGUILayout.HorizontalScope()) {
               var h = GUILayout.Height(EditorGUIUtility.singleLineHeight);
               var ew = GUILayout.ExpandWidth(false);
               EditorGUILayout.Space(40, false);

               using (new EditorGUILayout.VerticalScope(GUILayout.Width(40))) {
                  EditorGUILayout.SelectableLabel(pn.row_num_str,
                     GUILayout.Height(labe_h));
               }

               using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(false), GUILayout.Width(250))) {
                  EditorGUILayout.SelectableLabel(pn.name_str,
                     GUILayout.Height(labe_h), GUILayout.ExpandWidth(false));
               }
               EditorGUILayout.Space(20, false);

               foreach (var st in pn.stat_list_strs) {
                  EditorGUILayout.Space(10, false);
                  
                  using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(false), GUILayout.Width(80))) {
                     EditorGUILayout.SelectableLabel(st,
                        GUILayout.Height(labe_h), GUILayout.ExpandWidth(false));
                  }
               }
               EditorGUILayout.Space(0, true);

            }
         }

      }
   }

#endif
}