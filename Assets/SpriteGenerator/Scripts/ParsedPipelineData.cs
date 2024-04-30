using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using TMPro;
using static ExportPipeline;

public class ParsedPipelineData {
   public readonly ExportPipelineSheets sheets_pipeline_descriptor;


   public int output_n;

   public Dictionary<string, ParsedPart> parts = new();

   public List<ParsedUnit> units = new();

   public ModelPartsBundle parts_bundle => pipeline.parts_bundle;

   readonly ExportPipeline pipeline;
   Dictionary<string, AnimationSet> animation_sets => pipeline.animation_sets;


   public ParsedPipelineData(ExportPipelineSheets sheets_pipeline_descriptor, ExportPipeline pipeline) {
      this.pipeline = pipeline;
      this.sheets_pipeline_descriptor = sheets_pipeline_descriptor;

      init();
   }


   void init() {
      string[] GetRows(string text) {
         return text.SplitLines().Where(x => x.Trim().Length > 0).ToArray();
      }

      var unit_rows = GetRows(sheets_pipeline_descriptor.unit.text);
      var shield_rows = GetRows(sheets_pipeline_descriptor.shield.text);
      var armor_rows = GetRows(sheets_pipeline_descriptor.armor.text);
      var helmet_rows = GetRows(sheets_pipeline_descriptor.helmet.text);
      var weapon_rows = GetRows(sheets_pipeline_descriptor.weapon.text);

      foreach (var unit_row in unit_rows) {
         // Debug.Log($"ur: {unit_row}");
      }

      {
         var names = shield_rows[0].Split("\t");

         var sn = names.ToList().IndexOf("Sprite_Name");


         foreach (var part_str in shield_rows[1..]) {
            var data = part_str.Split("\t");


            var name = data[1];
            var model = data[sn];


            var cols = GetColors(names, data).ToList();

            // Debug.Log($"Colors: {cols.join(", ")}");

            var pr = ParsePart(name, model, cols, GetThemes(names, data).ToList());

            if (pr != null) {
               parts[pr.name] = pr;
            }
         }
      }
      {
         var names = helmet_rows[0].Split("\t");

         var sn = names.ToList().IndexOf("Sprite_Name");


         foreach (var part_str in helmet_rows[1..]) {
            var data = part_str.Split("\t");
            var name = data[1];
            var model = data[sn];

            // Debug.Log($"Name: {name}, {name.Length}");

            var cols = GetColors(names, data).ToList();

            var pr = ParsePart(name, model, cols, GetThemes(names, data).ToList());
            if (pr != null) {
               parts[pr.name] = pr;
            }
         }
      }
      Dictionary<string, ParsedArmor> armor_to_model_map = new();
      {
         var names = armor_rows[0].Split("\t");

         var sn = names.ToList().IndexOf("Armor_Sprite_Name");


         foreach (var part_str in armor_rows[1..]) {
            var data = part_str.Split("\t");


            var pa = new ParsedArmor();

            pa.name = data[1];
            pa.armor = data[sn];
            pa.gauntlet = data[sn + 1];
            pa.legging = data[sn + 2];
            pa.boots = data[sn + 3];

            pa.colors = GetColors(names, data).ToList();

            var vals = sheets_pipeline_descriptor.armor_data[pa.name];

            pa.theme_1 = ToName(vals["Theme Color 1"]);
            pa.theme_2 = ToName(vals["Theme Color 2"]);
            pa.theme_3 = ToName(vals["Theme Color 3"]);

            if (pa.theme_1.Length > 0) {
               //  Debug.Log($"{pa.name} {pa.theme_1}");
            }

            if (pa.theme_2.Length > 0) {
               // Debug.Log($"{pa.name} {pa.theme_2}");
            }

            if (pa.theme_3.Length > 0) {
               // Debug.Log($"{pa.name} {pa.theme_3}");
            }

            armor_to_model_map[pa.name] = pa;
         }
      }
      {
         var names = weapon_rows[0].Split("\t");

         var sn = names.ToList().IndexOf("Sprite_Name");


         foreach (var part_str in weapon_rows[1..]) {
            var data = part_str.Split("\t");


            var name = data[1];
            var model = data[sn];

            var cols = GetColors(names, data).ToList();

            var pr = ParsePart(name, model, cols, GetThemes(names, data).ToList());
            if (pr != null) {
               parts[pr.name] = pr;
            }
         }
      }

      {
         var dict = sheets_pipeline_descriptor.unit_data;
         var names = unit_rows[0].Split("\t");


         foreach (var part_str in unit_rows[1..]) {
            if (part_str.StartsWith("//")) continue;
            var data = part_str.Split("\t");
            var name = data[1];

            var vals = dict[name];


            var an_type = vals["AnimationType"];
            var atype = DataParsing.NormalizeAnimationName(an_type);
            if (atype.Trim().Length == 0) {
               Debug.LogError($"Skipping {name} due to lacking animation.");
               continue;
            }

            ParsedUnit pu = new ParsedUnit();
            pu.animation_type = an_type;
            pu.raw_name = name;
            pu.model_name = pipeline.defaultRenderModelName;
            var helm = vals["Helmet"];

            if (helm.Length == 0) {
               // pu.model_name = "Archer";
            }

            var transo = pipeline.model_mapping_by_body_type[vals["Anatomy"]];
            pu.model_body = transo;
            pu.model_name = transo.name;

            // Debug.Log($"Transo name {transo.name}");


            {
               var armor_name = vals["Armor"];

               if (armor_to_model_map.TryGetValue(armor_name, out var armor)) {
                  var rc = armor.colors.ToList();

                  if (ColorUtility.TryParseHtmlString(vals["Theme Color"], out var theme_color)) {
                     pu.theme_color = theme_color;

                     ApplyTheme(theme_color, rc, armor.theme_1, armor.theme_2, armor.theme_3);
                  }

                  pu.colors.AddRange(rc);

                  var maps = new[] {
                     ("Body_Armor", armor.armor),
                     ("Leg_Armor", armor.legging),
                     ("Gauntlets", armor.gauntlet),
                     ("Boots", armor.boots),
                  };

                  foreach (var (slot, arm) in maps) {
                     if (transo.skins_to_transform.TryGetValue(slot, out var value1)) {
                        pu.skin_map[value1] = arm;
                     } else {
                        Debug.LogError($"Missing skin {slot} in {transo.name}");
                     }
                  }
               }
            }


            // Debug.Log($"{armor.name}: {armor.colors.join(" ")}");


            pu.out_name = GetExportUnitName(name);


            var w1 = vals["Weapon_Primary"];
            var w2 = vals["Weapon_Secondary"];

            var sh = vals["Shield"];

            if (!pu.no_gear) {
               pu.slot_map[transo.slot_to_transform["Main_Hand"]] = w1;
               pu.slot_map[transo.slot_to_transform["Off_Hand"]] = w2;
               pu.slot_map[transo.slot_to_transform["Off_Hand_Shield"]] = sh;

               if (transo.name != "Archer") {
                  pu.slot_map[transo.slot_to_transform["NewHelmet"]] = helm;
               } else {
               }
            }
            // if (idle_only) atype = "Idle";

            var animation_set = animation_sets[atype];

            var anims = animation_set.res;
            if (pipeline.idle_only) anims = anims.Where(x => x.category == "Idle").Take(1).ToList();


            pu.animations.AddRange(anims);
            pu.idle_animation_id = pu.animations.FindIndex(x => x.category == "Idle");


            units.Add(pu);
         }
      }

      output_n = units.Sum(x => x.animations.Count);
   }

   static IEnumerable<(string field, Color color)> GetColors(string[] names, string[] data) {
      for (int i = 0; i < names.Length; i++) {
         if (names[i].Contains("_Color")) {
            var cn = names[i];

            var val = data[i].Trim();
            if (val.Length == 0) continue;
            if (ColorUtility.TryParseHtmlString(val, out Color col)) {
               // col.a = 1;
               yield return (ToName(cn), col);
            } else {
               Debug.Log($"Bad color: {cn} has {val}");
            }
         }
      }
   }

   ParsedPart ParsePart(string name, string ob_name, List<(string field, Color color)> colors, List<string> themes) {
      if (name == "None" && ob_name == "") return null;
      var tr = parts_bundle.slotted_parts.Find(x => x.name == ob_name);
      if (!tr) {
         Debug.Log($"Missing part: {name}, '{ob_name}'");
         return null;
      }

      tr = GameObject.Instantiate(tr, pipeline.dummy_holder);
      Material last_mat = null;
      Material last_rmat = null;

      var pname = name;

      var parts = new ParsedPart() {
         name = pname,
      };
      parts.colors.AddRange(colors);

      parts.theme_1 = ToName(themes.Get(0, ""));
      parts.theme_2 = ToName(themes.Get(1, ""));
      parts.theme_3 = ToName(themes.Get(2, ""));

      if (colors.Count > 0) {
         // Debug.Log($"Add colors to {name}, {colors.Count}: {colors.join(" | ")}");

         foreach (var rend in tr.GetComponentsInChildren<Renderer>()) {
            var mat = rend.sharedMaterial;
            if (mat == last_mat) {
               rend.material = last_rmat;
            } else {
               last_mat = mat;
               last_rmat = ApplyMaterialColor(colors, pipeline.MapMat(mat, name));
               // Debug.Log($"Applied color to {tr.name}: {name}: {mat.name} {last_rmat.name}: {colors.join(", ")}");
               rend.material = last_rmat;
            }
         }

         if (last_rmat) {
            var rs = new GameObject($"_ _ ITEM {name} _ {ob_name}").AddComponent<SpriteCaptureResultHolder>();
            rs.used_material = last_rmat;
         }
      }


      parts.part_prefab = tr;
      return parts;
   }
}

public class ParsedUnit {
   public string raw_name;
   public string out_name;
   public string model_name;

   public Material material => model_body.body_category?.material;
   public BodyModelData model_body;

   public bool no_gear => model_body.body_category.no_gear;

   public List<(string field, Color color)> colors = new();

   public Dictionary<string, string> slot_map = new();
   public Dictionary<string, string> skin_map = new();

   public List<AnimationWrap> animations = new();

   public string animation_type;

   public int idle_animation_id;

   public Color? theme_color;
}

public class ParsedPart {
   public string name;
   public List<(string field, Color color)> colors = new();

   public Transform part_prefab;

   public string theme_1;
   public string theme_2;
   public string theme_3;

   public bool HasTheme => theme_1.IsNonEmpty() || theme_2.IsNonEmpty() || theme_3.IsNonEmpty();
}

public class BodyModelData {
   public string name;
   public Dictionary<string, string> slot_to_transform = new();
   public Dictionary<string, string> skins_to_transform = new();

   public ModelBodyCategory body_category;
}

public class AnimationWrap {
   public AnimationWrap(string name, AnimationClip clip, string category, int frame,
      AnimationTypeObject animation_type_object) {
      this.name = name;
      this.clip = clip;
      this.category = category;
      this.frame = frame;
      this.animation_type_object = animation_type_object;
   }


   public string name;
   public AnimationClip clip;
   public string category;
   public int frame;

   public AnimationTypeObject animation_type_object;
}