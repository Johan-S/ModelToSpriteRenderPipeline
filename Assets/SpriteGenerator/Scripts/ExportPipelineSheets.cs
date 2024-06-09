using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// [CreateAssetMenu(fileName = "NewFile", menuName = "NewExport", order = 0)]
public class ExportPipelineSheets : ScriptableObject {


   public string output_name = "new_sprites";
   
   [Header("Sheets")]
   public TextAsset armor;
   public TextAsset helmet;
   public TextAsset shield;
   public TextAsset unit;
   public TextAsset weapon;

   public TextAsset animations;

   [Header("Dont use if you dont know")]
   public TextAsset[] multi_sheet_do_not_set;


   public (Shared.AnimationParsed[] arr, Dictionary<(string, string), Shared.AnimationParsed> dict) GetGoodAnims () {
      Dictionary<(string, string), Shared.AnimationParsed> res = new();

      List<Shared.AnimationParsed> arr = new();

      var rows = animations.text.SplitLines().Where(x => x.Trim().Length > 0).ToArray();
      Debug.Log($"rows: {rows.join("\n")}");
      var cats = rows[0].Split("\t");
      var fields = rows[1].Split("\t");


      for (int i = 2; i < rows.Length; i++) {
         var row = rows[i].Split("\t");
         string type = row[0];
         for (int j = 1; j < cats.Length; j++) {
            string cat = cats[j];
            string field = fields[j];

            var data = row[j].Trim();
            if (data.IsEmpty()) continue;

            var key = (type, cat);


            if (!res.TryGetValue(key, out var tc)) {
               tc = res[key] = new(type, cat);
               arr.Add(tc);
            }
            
            if (field == "Name") tc.clip = data;
            if (field == "Frames") tc.capture_frame = data.Split(",").Select(x => int.Parse(x.Trim())).ToArray();
            if (field == "DurationMS") tc.time_ms = data.Split(",").Select(x => int.Parse(x.Trim())).ToArray();

         }
      }


      return (arr.ToArray(), res);
   }


   public Dictionary<string, Dictionary<string, string>> GetData(string tsv) {
      var rows = tsv.SplitLines().Where(x => x.Trim().Length > 0).ToArray();
      if (rows.Length == 0) return new();
      var names = rows[0].Split("\t").EnsureUniqueSymbols();

      var res = new Dictionary<string, Dictionary<string, string>>();

      foreach (var r in rows[1..]) {
         if (r.StartsWith("//")) continue;
         var v = r.Split("\t");

         res[v[1]] = names.Length.times().ToDictionary(i => names[i].Trim(), i => v[i].Trim());
      }

      return res;
   }

   public Dictionary<(string, string), Shared.AnimationParsed> animation_good;

   [NonSerialized]
   public Shared.AnimationParsed[] animation_arr;

   public Dictionary<string, Dictionary<string, string>> unit_data;
   public Dictionary<string, Dictionary<string, string>> shield_data;
   public Dictionary<string, Dictionary<string, string>> armor_data;
   public Dictionary<string, Dictionary<string, string>> helmet_data;

   public void InitData() {
      if (animations) (animation_arr, animation_good) = GetGoodAnims();

      unit_data = GetData(unit ? unit.text : "");
      shield_data = GetData(shield.text);
      armor_data = GetData(armor.text);
      helmet_data = GetData(helmet.text);
   }

   public void Dost() {
   }
}