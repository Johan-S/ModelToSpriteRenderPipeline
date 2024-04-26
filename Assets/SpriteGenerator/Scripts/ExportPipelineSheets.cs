using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// [CreateAssetMenu(fileName = "NewFile", menuName = "NewExport", order = 0)]
public class ExportPipelineSheets : ScriptableObject {
   public TextAsset armor;
   public TextAsset helmet;
   public TextAsset shield;
   public TextAsset unit;
   public TextAsset weapon;

   public TextAsset animations;

   [Header("Dont use if you dont know")]
   public TextAsset[] multi_sheet_do_not_set;

   [Serializable]
   public class AnimationParsed {
      public AnimationParsed() {
         
      }
      public string animation_type;
      public string category;
      public string clip;

      public int[] capture_frame;
      public int[] time_ms;

      public AnimationParsed(string animation_type, string category) {
         this.animation_type = animation_type;
         this.category = category;
      }

      public override string ToString() {

         return $"{animation_type} {category}";
      }
   }


   public (AnimationParsed[] arr, Dictionary<(string, string), AnimationParsed> dict) GetGoodAnims () {
      Dictionary<(string, string), AnimationParsed> res = new();

      List<AnimationParsed> arr = new();

      var rows = animations.text.SplitLines().Where(x => x.Trim().Length > 0).ToArray();
      var cats = rows[0].Split("\t");
      var fields = rows[1].Split("\t");


      for (int i = 2; i < rows.Length; i++) {
         var row = rows[i].Split("\t");
         string type = row[0];
         for (int j = 1; j < cats.Length; j++) {
            string cat = cats[j];
            string field = fields[j];

            var data = row[j].Trim();
            if (data.IsNullOrEmpty()) continue;

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
      var names = rows[0].Split("\t").EnsureUniqueSymbols();

      var res = new Dictionary<string, Dictionary<string, string>>();

      foreach (var r in rows[1..]) {
         if (r.StartsWith("//")) continue;
         var v = r.Split("\t");

         res[v[1]] = names.Length.times().ToDictionary(i => names[i].Trim(), i => v[i].Trim());
      }

      return res;
   }

   public Dictionary<(string, string), AnimationParsed> animation_good;

   [NonSerialized]
   public AnimationParsed[] animation_arr;

   public Dictionary<string, Dictionary<string, string>> unit_data;
   public Dictionary<string, Dictionary<string, string>> shield_data;
   public Dictionary<string, Dictionary<string, string>> armor_data;
   public Dictionary<string, Dictionary<string, string>> helmet_data;

   public void InitData() {
      if (animations) (animation_arr, animation_good) = GetGoodAnims();

      unit_data = GetData(unit.text);
      shield_data = GetData(shield.text);
      armor_data = GetData(armor.text);
      helmet_data = GetData(helmet.text);
   }

   public void Dost() {
   }
}