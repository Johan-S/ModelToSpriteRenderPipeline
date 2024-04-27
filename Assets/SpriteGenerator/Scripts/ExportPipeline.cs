using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using TMPro;

[DefaultExecutionOrder(20)]
public class ExportPipeline : MonoBehaviour {
   [Header("Debug Helpers")] public SimpleUnitTypeObject load_unit_on_play;
   [Header("Pipeline Toggles")] public bool idle_only;

   public bool only_atlas;

   public bool only_atlas_meta;

   public bool write_files = true;

   [Header("Export Pipeline Description")]
   public ExportPipelineSheets sheets_pipeline_descriptor;

   public ModelPartsBundle parts_bundle;
   public AnimationBundle animation_bundle;
   public AnimationBundle[] animation_bundle_extra;
   public int export_size = 64;

   public int atlas_sprites_per_row = 5;
   public string export_to_folder = "Gen";
   [Header("Bindings")] public Material material;

   public GameObject model;

   public SpriteCapturePipeline sprite_capture_pipeline;

   public TMP_Text progress_text;

   public string defaultRenderModelName = "Heavy Infantry";

   void Start() {
      if (progress_text) {
         progress_text.transform.parent.gameObject.SetActive(false);
      }

      InitSheets();
   }

   SimpleUnitTypeObject cur_loaded;

   void Update() {
      if (sprite_capture_pipeline.exporting) {
         cur_loaded = null;
         return;
      }


      if (load_unit_on_play != cur_loaded) {
         cur_loaded = load_unit_on_play;
         var u = parsed_pipeline_data.units.Find(x => x.raw_name == cur_loaded.name);
         if (u == null) {
            Debug.Log($"Didn't find {cur_loaded.name} in export data!");
            return;
         }

         SetActiveUnit(u);
      }
   }

   int output_i = 0;

   [Header("Debug")] public bool executed;

   public void ExecuteMyPipeline() {
      if (executed) return;


      IEnumerator SubPl() {
         yield return
            StartCoroutine(RunPipeline());
      }

      StartCoroutine(SubPl());
   }

   public class TimeBenchmark {
      Dictionary<string, double> times = new();

      double last_t = Time.realtimeSinceStartupAsDouble;
      double first_t = Time.realtimeSinceStartupAsDouble;

      public void Begin() {
         last_t = Time.realtimeSinceStartupAsDouble;
      }

      List<string> steps = new();

      public void Lap(string n) {
         var t = Time.realtimeSinceStartupAsDouble;

         if (!times.TryGetValue(n, out var res)) {
            res = 0;
            steps.Add(n);
         }

         res += t - last_t;
         last_t = t;

         times[n] = res;
      }


      public double LogTimes(int nc) {
         var dt = last_t - first_t;

         Debug.Log(
            $"Export Bench tot {dt / nc * 1000:0} ms  per render: {dt:0.0} / {nc} :\n{steps.join("\n", x => $"{x}: {times[x]}")}");

         return dt;
      }
   }

   TimeBenchmark time_benchmark;

   List<(string FileOutput, RectInt rect, Vector2 ground_center_pivot)> sprite_gen_meta = new();


   string SpriteGenMetaRow((string FileOutput, RectInt rect, Vector2 ground_center_pivot) d) {
      var r = d.rect;
      var p = d.ground_center_pivot;

      return $"{d.FileOutput}\t{r.x},{r.y},{r.width},{r.height}\t{p.x},{p.y}";
   }

   void FinalizeMetaFile() {
      var meta_rows = sprite_gen_meta.Select(SpriteGenMetaRow).ToArray();

      var mf = $"{export_to_folder}/test_atlas.spritemeta";
      File.WriteAllText(mf, meta_rows.join("\n"));

      // File.Copy(mf, $"{export_to_folder}/test_atlas.spritemeta", overwrite: true);
   }


   void FillMetaOnly() {
      var meta_rows = sprite_gen_meta.Select(SpriteGenMetaRow).ToArray();

      File.WriteAllText($"{export_to_folder}/atlas_meta.txt", meta_rows.join("\n"));
   }


   public void ShowUnitViewer() {
      if (export_tex_tot == null) {
         Debug.Log("Run pipeline first!");
         return;
      }

      OpenUnitViewer(export_tex_tot);
   }

   public UnitViewer unit_viewer_prefab;

   UnitViewer unit_viewer_running;

   void OpenUnitViewer(Texture2D export_tex) {
      if (unit_viewer_running) return;

      var meta_rows = sprite_gen_meta.Select(SpriteGenMetaRow).ToArray();

      GeneratedSpritesContainer.SetInstanceFromData(export_tex, meta_rows.join("\n"));


      EngineDataInit.SetEngineSheets(sheets_pipeline_descriptor);

      unit_viewer_running = Instantiate(unit_viewer_prefab);

      IEnumerator CloseAfterEsc() {
         yield return null;
         yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Escape));
         Destroy(unit_viewer_running.gameObject);
      }

      StartCoroutine(CloseAfterEsc());
   }

   void CopyAndDownsampleTo(Texture2D src, Texture2D dst, string FileOutput) {
      var cols = src.GetPixels();

      if (dst.width == src.width && dst.height == src.height) {
         dst.SetPixels(cols);
         dst.Apply();
         DumpExport(cols, FileOutput);
         return;
      }

      Color[] res = new Color[dst.width * dst.height];

      int sw = src.width, sh = src.height;
      int w = dst.width, h = dst.height;
      int ns = sw / w;


      var buff = new Color[ns * ns];

      void LoadBuff(int x, int y) {
         int s = x * ns + y * ns * sw;
         int oi = 0;
         for (int i = 0; i < ns; i++) {
            for (int j = 0; j < ns; j++) {
               buff[oi++] = cols[s + j + i * sw];
            }
         }
      }

      Color MergeColors(Color[] m) {
         Color res = default;
         foreach (var x in m) {
            if (x.a == 0) continue;
            res += x * (1f / m.Length);
         }

         return res;
      }

      Color MergeColors_Dumb(Color[] m) {
         Color res = default;
         float a = 1;
         float at = 0;
         foreach (var x in m) {
            a *= 1 - x.a;
            at += x.a;
         }

         if (at == 0) return m[0];

         a = 1 - a;
         float amag = a / at;
         foreach (var x in m) {
            var ea = x.a * amag;
            var r = x * ea;
            res += r;
         }

         res.a = a;
         return res;
      }


      for (int i = 0; i < h; i++) {
         for (int j = 0; j < w; j++) {
            LoadBuff(j, i);

            res[j + i * w] = MergeColors(buff);
         }
      }

      dst.SetPixels(res);
      dst.Apply();
      DumpExport(res, FileOutput);
   }

   void UpdateProgress() {
      if (progress_text) {
         progress_text.transform.parent.gameObject.SetActive(true);

         progress_text.text = $"{ei} / {out_sprite_count}";
      }
   }

   Vector2Int out_grid;
   int out_sprite_count;


   RectInt MoveMeta(string FileOutput) {
      var ei = sprite_gen_meta.Count;

      Vector2Int tid = new(ei % export_tex_sprites_w, ei / export_tex_sprites_w);
      var pos = tid;
      ei++;

      pos.y = out_grid.y - 1 - pos.y;


      var rect = new RectInt(pos.x * export_tex.width, pos.y * export_tex.height, export_tex.width, export_tex.height);

      sprite_gen_meta.Add((FileOutput, rect, GeneratedSpritesContainer.DEFAULT_PIVOT));

      return rect;
   }

   void DumpExport(Color[] res, string FileOutput) {
      ei++;

      var rect = MoveMeta(FileOutput);


      export_tex_tot.SetPixels(rect.x, rect.y, rect.width, rect.height,
         res);


      UpdateProgress();
   }

   int export_tex_sprites_w;
   Vector2Int tid => new(ei % export_tex_sprites_w, ei / export_tex_sprites_w);

   int ei = 0;


   IEnumerable<Renderer> GetChildRenders() {
      var sr = model.transform.GetChild(0);

      for (int i = 0; i < sr.childCount; i++) {
         var ch = sr.GetChild(i);
         var x = ch.GetComponent<Renderer>();

         if (x) {
            // Debug.Log($"render {sr.name} {x}");
            yield return x;
         }
      }
   }

   static string GetFileOutput(string out_name, string animation_category, int frame) {
      return out_name + "." + animation_category + "." + frame;
   }

   static string GetFileOutput(ParsedUnit pu, string animation_category, int frame) {
      return GetFileOutput(pu.out_name, animation_category, frame);
   }

   static IEnumerable<string> GetFileOutputs(ParsedUnit pu) {
      foreach (var an in pu.animations) {
         yield return GetFileOutput(pu, an.category, an.frame);
      }
   }


   void RunOutputImpl(GameObject model_prefab, ParsedUnit pu, AnimationWrap an, Material res_mat) {
      sprite_capture_pipeline.model.SetAnimationNow(an.clip, an.frame);
      output_i++;
      var FileOutput = GetFileOutput(model_prefab.name, an.category, an.frame);

      foreach (var x in GetChildRenders()) {
      }
      // Debug.Log($"Output {output_i}: {name}");

      var folder = export_to_folder;
      int mapped_mats = 0;


      foreach (var x in model.GetComponentsInChildren<Renderer>()) {
         if (x.transform.parent.parent == model.transform) {
            x.material = res_mat;
            mapped_mats++;
         }
      }


      sprite_capture_pipeline.RunPipeline();

      CopyAndDownsampleTo(sprite_capture_pipeline.result_rexture, export_tex, FileOutput);


      if (an.animation_type_object != null) {
         var cid = output_i - 1;
         (string FileOutput, RectInt rect, Vector2 ground_center_pivot) o = sprite_gen_meta[cid];
         Vector3 model_off = an.animation_type_object.model_offsetmodel_offset;
         Vector2 camera_d = sprite_capture_pipeline.camera_handle.OrthographicRectSize;

         if (model_off != default) {
            var sprite_pivot = GetAdjustedSpritePivot(o.ground_center_pivot, model_off, camera_d);
            sprite_gen_meta[cid] = (o.FileOutput, o.rect, sprite_pivot);
         }
      }


      var data = export_tex.EncodeToPNG();

      if (!only_atlas) {
         if (write_files) File.WriteAllBytes($"{folder}/{FileOutput}.png", data);
      }

      foreach (var x in model.GetComponentsInChildren<Renderer>()) {
         if (x.transform.parent.parent == model.transform) {
            x.material = res_mat;
         }
      }
   }

   public Vector2 GetAdjustedSpritePivot(Vector2 original_pivot, Vector3 model_offset, Vector2 cap_size) {
      Vector3 original_world_pos = original_pivot * cap_size;

      var new_world_pos = original_world_pos + model_offset;

      var new_pivot = new_world_pos / cap_size;

      return new_pivot;
   }

   static IEnumerable<string> GetThemes(string[] names, string[] data) {
      for (int i = 0; i < names.Length; i++) {
         if (names[i].Replace(" ", "_").StartsWith("Theme_Color")) {
            var val = data[i];
            if (val.IsNonEmpty()) yield return val;
         }
      }
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

   static string ToName(string s) {
      if (s.Length == 0) return "";
      return "_" + s.Replace("-", "").Replace("_", "").ToUpper();
   }

   static Material ApplyMaterialColor(List<(string field, Color color)> colors, Material material) {
      Material res_mat = Instantiate(material);

      foreach (var x in colors) {
         if (!res_mat.HasColor(x.field)) {
            Debug.Log($"Missing color in {material.name}, shader {material.shader.name}: '{x.field}'");
         }

         res_mat.SetColor(x.field, x.color);
         // Debug.Log($"{a.Name}: {key} {a.Value}, {col}");
      }

      return res_mat;
   }

   public float max_waits_per_frame = 0.35f;

   [NonSerialized] double rt;

   IEnumerable RunOutput_Parsed(GameObject model_prefab, ParsedUnit pu) {
      var folder = export_to_folder;
      if (!Directory.Exists(folder)) {
         Directory.CreateDirectory(folder);
      }

      Material res_mat = ApplyMaterialColor(pu.colors, pu.material);


      var body = pu.model_body.body_category;

      var model = sprite_capture_pipeline.model;


      foreach (AnimationWrap an in pu.animations) {
         if (an.animation_type_object != null && an.animation_type_object.looping_root) {
            var move_back = sprite_capture_pipeline.HandleLoopingRootMotion(an.clip, an.frame / 60f / an.clip.length);
            RunOutputImpl(model_prefab, pu, an, res_mat);
            move_back();
         } else {
            RunOutputImpl(model_prefab, pu, an, res_mat);
         }


         if (rt + max_waits_per_frame > Time.realtimeSinceStartupAsDouble) {
            continue;
         }

         yield return null;
         rt = Time.realtimeSinceStartupAsDouble;
      }
   }

   Texture2D export_tex;
   Texture2D export_tex_tot;

   static void ApplyTheme(Color theme_color, List<(string field, Color color)> rc, string theme_1, string theme_2,
      string theme_3) {
      if (theme_1.Length > 0) {
         var i = rc.FindIndex(x => x.field == theme_1);
         if (i == -1) {
            rc.Add((theme_1, theme_color));
         } else {
            var rct = Color.Lerp(rc[i].color, theme_color, 0.5f);
            var bl = Color.Lerp(rct, Color.black, 0.25f);
            rc[i] = (theme_1, bl);
         }
      }

      if (theme_2.Length > 0) {
         var i = rc.FindIndex(x => x.field == theme_2);
         if (i == -1) {
            rc.Add((theme_2, theme_color));
         } else {
            var rct = Color.Lerp(rc[i].color, theme_color, 0.75f);
            rc[i] = (theme_2, rct);
         }
      }

      if (theme_3.Length > 0) {
         var i = rc.FindIndex(x => x.field == theme_3);
         if (i == -1) {
            rc.Add((theme_3, theme_color));
         } else {
            var rct = Color.Lerp(rc[i].color, theme_color, 0.95f);
            rc[i] = (theme_3, rct);
         }
      }
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

   public void AddThemePartTo(ParsedPart pp, Transform parent, Color? theme_color) {
      var tr = Instantiate(pp.part_prefab, parent);
      if (pp.HasTheme && theme_color is Color tc) {
         var rc = pp.colors.ToList();
         Material last_mat = null;
         Material last_rmat = null;
         ApplyTheme(tc, rc, pp.theme_1, pp.theme_2, pp.theme_3);
         // rc = rc.Select(x => (x.field, Color.white)).ToList();
         foreach (var rend in tr.GetComponentsInChildren<Renderer>()) {
            var mat = rend.sharedMaterial;
            if (mat == last_mat) {
               rend.material = last_rmat;
            } else {
               last_mat = mat;
               last_rmat = ApplyMaterialColor(rc, MapMat(mat, pp.name));
               // Debug.Log($"Applied color to {tr.name}: {name}: {mat.name} {last_rmat.name}: {rc.join(", ")}");
               rend.material = last_rmat;
            }
         }
      }
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


   public class ParsedUnit {
      public string raw_name;
      public string out_name;
      public string model_name;

      public Material material => model_body.body_category?.material;
      public ModelDataStuff model_body;

      public bool no_gear => model_body.body_category.no_gear;

      public List<(string field, Color color)> colors = new();

      public Dictionary<string, string> slot_map = new();
      public Dictionary<string, string> skin_map = new();

      public List<AnimationWrap> animations = new();

      public int idle_animation_id;

      public Color? theme_color;
   }

   public class ParsedPipelineData {
      public int output_n;

      public Dictionary<string, ParsedPart> parts = new();

      public List<ParsedUnit> units = new();
   }

   Dictionary<string, Material> material_map;
   Material atoon_mat;

   Material MapMat(Material a, string context) {
      if (material_map == null) {
         atoon_mat = Resources.Load<Material>("BaseModels/Materials/Armors_Material_Toon");
         material_map = new();
         foreach (var m in Resources.LoadAll<Material>("BaseModels/Materials")) {
            if (m.shader != atoon_mat.shader) {
               material_map["PT_" + m.name] = atoon_mat;
               material_map["PT_" + m.name + " (Instance)"] = atoon_mat;
            } else {
               material_map["PT_" + m.name] = m;
               material_map["PT_" + m.name + " (Instance)"] = m;
            }
         }
      }

      if (a.shader != atoon_mat.shader) {
         LogMissing($"Trying to mix toon and normal shaders: {context}, {a.name}");
         return material_map.Get(a.name, atoon_mat);
         ;
      }

      return material_map.Get(a.name, a);
   }

   ParsedPart ParsePart(string name, string ob_name, List<(string field, Color color)> colors, List<string> themes) {
      if (name == "None" && ob_name == "") return null;
      var tr = parts_bundle.slotted_parts.Find(x => x.name == ob_name);
      if (!tr) {
         Debug.Log($"Missing part: {name}, '{ob_name}'");
         return null;
      }

      tr = Instantiate(tr, dummy_holder);
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
               last_rmat = ApplyMaterialColor(colors, MapMat(mat, name));
               // Debug.Log($"Applied color to {tr.name}: {name}: {mat.name} {last_rmat.name}: {colors.join(", ")}");
               rend.material = last_rmat;
            }
         }
      }

      parts.part_prefab = tr;
      return parts;
   }

   public class ParsedArmor {
      public string name;

      public string armor;
      public string legging;
      public string gauntlet;
      public string boots;

      public string theme_1;
      public string theme_2;
      public string theme_3;

      public List<(string field, Color color)> colors;
   }

   ParsedPipelineData GetParsed_Sheets() {
      ParsedPipelineData res = new();

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
               res.parts[pr.name] = pr;
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
               res.parts[pr.name] = pr;
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
               res.parts[pr.name] = pr;
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


            var atype = vals["AnimationType"].Replace("&", "_");
            if (atype.Trim().Length == 0) {
               Debug.LogError($"Skipping {name} due to lacking animation.");
               continue;
            }

            ParsedUnit pu = new ParsedUnit();
            pu.raw_name = name;
            pu.model_name = defaultRenderModelName;
            var helm = vals["Helmet"];

            if (helm.Length == 0) {
               // pu.model_name = "Archer";
            }

            var transo = model_mapping_by_body_type[vals["Anatomy"]];
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
            if (idle_only) anims = anims.Where(x => x.category == "Idle").Take(1).ToList();


            pu.animations.AddRange(anims);
            pu.idle_animation_id = pu.animations.FindIndex(x => x.category == "Idle");


            res.units.Add(pu);
         }
      }

      res.output_n = res.units.Sum(x => x.animations.Count);


      return res;
   }

   public static string GetExportUnitName(string orig_name) {
      return DataParsing.GetExportUnitName(orig_name);
   }

   public class AnimationSet {
      public string name;

      public List<AnimationWrap> res = new();
   }


   public static
      IEnumerable<ExportPipelineSheets.AnimationParsed> GetDirectAnimationsParsed() {
      var clips = Resources.Load<AnimationBundle>("WolfAnimations");

      var anim_objs = Resources.LoadAll<AnimationTypeObject>("DirectAnims/");

      var groups = anim_objs.GroupBy(x => x.animation_type.Replace("&", "_"));


      foreach (var g in groups) {
         var p = new AnimationSet();

         p.name = g.Key;

         foreach (var data in g) {
            AnimationClip clip = clips.animation_clips.Find(x => x.name == data.clip);
            var ap = new ExportPipelineSheets.AnimationParsed();

            ap.clip = data.clip;
            ap.animation_type = data.animation_type;
            ap.category = data.category;


            if (ap.time_ms.IsNullOrEmpty() && data.auto_frames_per_s > 0 && clip) {
               if (data.auto_frames_per_s > 0 && data.capture_frame.IsNullOrEmpty()) {
                  var len = clip.length;

                  var frames = Mathf.CeilToInt(len * data.auto_frames_per_s);

                  if (frames < 2) frames = 2;

                  ap.time_ms = new int[frames - 1];
                  ap.capture_frame = new int[frames - 1];

                  for (int i = 1; i < frames; i++) {
                     float time = i * len / (frames - 1);
                     float dt = time - (i - 1) * len / (frames - 1);

                     ap.time_ms[i - 1] = Mathf.RoundToInt(dt * 1000);
                     ap.capture_frame[i - 1] = Mathf.FloorToInt(time * 60);
                  }
               }
            } else {
               ap.time_ms = data.time_ms;
               ap.capture_frame = data.capture_frame;
            }

            yield return ap;
         }
      }
   }

   public IEnumerable<AnimationSet> GetDirectAnimationSets() {
      var anim_objs = Resources.LoadAll<AnimationTypeObject>("DirectAnims/");

      var groups = anim_objs.GroupBy(x => x.animation_type.Replace("&", "_"));


      foreach (var g in groups) {
         var p = new AnimationSet();

         p.name = g.Key;

         foreach (var data in g) {
            AnimationClip clip = GetAnimationClip(data.clip);
            if (clip == null) {
               Debug.LogError($"Missing clip: {data.clip}");
               continue;
            }

            if (data.auto_frames_per_s > 0 && data.capture_frame.IsNullOrEmpty()) {
               var len = clip.length;

               var frames = Mathf.CeilToInt(len * data.auto_frames_per_s);

               if (frames < 2) frames = 2;

               for (int i = 1; i < frames; i++) {
                  float time = i * len / (frames - 1);

                  p.res.Add(new(data.clip, clip, data.category, Mathf.FloorToInt(time * 60), data));
               }
            } else {
               foreach (var fr in data.capture_frame) {
                  p.res.Add(new(data.clip, clip, data.category, fr, data));
               }
            }
         }


         // Debug.Log($"an: {p.name}:\n\t{p.res.join("\n\t")}");


         // Debug.Log($"Group {g.Key}: {g.ToList().join(", ", x => x.category)}");
         yield return p;
      }
   }

   Dictionary<string, AnimationSet> GetanimationSets() {
      Dictionary<string, AnimationSet> res = new();
      var arr = sheets_pipeline_descriptor.animation_arr;
      var types = arr.Select(x => x.animation_type).Distinct();
      var cats = arr.Select(x => x.category).Distinct();

      foreach (var anim in types) {
         var p = new AnimationSet {
            name = anim.Replace("&", "_"),
         };

         foreach (var c in cats) {
            if (sheets_pipeline_descriptor.animation_good.TryGetValue((anim, c), out var data)) {
               AnimationClip clip = GetAnimationClip(data.clip);
               if (clip == null) {
                  Debug.LogError($"Missing clip: {data.clip}");
                  continue;
               }

               foreach (var fr in data.capture_frame) {
                  p.res.Add(new(data.clip, clip, data.category, fr, null));
               }
            }
         }

         res[p.name] = p;
      }


      var direct_anims = GetDirectAnimationSets().ToList();

      foreach (var p in direct_anims) {
         res[p.name] = p;
      }

      return res;
   }

   AnimationClip GetAnimationClip(string animation_name) {
      AnimationClip clip = animation_bundle.animation_clips.Find(x => x.name == animation_name);

      foreach (var a in animation_bundle_extra) {
         if (clip) break;
         clip = a.animation_clips.Find(x => x.name == animation_name);
      }

      return clip;
   }

   [SerializeField] Transform dummy_holder;


   public class ModelDataStuff {
      public string name;
      public Dictionary<string, string> slot_to_transform = new();
      public Dictionary<string, string> skins_to_transform = new();

      public ModelBodyCategory body_category;
   }

   GameObject PrepModelObject(ParsedUnit u) {
      GameObject model_prefab = Resources.Load<GameObject>($"BaseModels/{u.model_name}");
      var omodel_prefab = model_prefab;

      sprite_capture_pipeline.model.model_offset = u.model_body.body_category.model_offset;

      void RunLoadout(Action<GameObject> model_action) {
         model_prefab = omodel_prefab;

         // Debug.Log($"{model_name}: {model_prefab}");
         if (model_prefab) {
            var mod = sprite_capture_pipeline.model.ResetModel(model_prefab, model_action);


            model_prefab = mod;
         } else {
            Debug.LogError($"Missing model: {u.model_name}");
         }
      }

      RunLoadout(model => {
         var skinned_rend = model.GetComponentsInChildren<SkinnedMeshRenderer>()
            .Where(x => x.transform.parent == model.transform).ToArray();

         foreach (var kv in u.skin_map) {
            var sk = skinned_rend.Find(x => x.name == kv.Key);

            if (!sk) {
               Debug.Log($"Didn't find slot {kv.Key} on model {model.name}");
               continue;
            }

            if (kv.Value.Length == 0) sk.gameObject.SetActive(false);
            else {
               var mesh = parts_bundle.body_parts.Find(x => x.name == kv.Value);
               if (mesh == null) {
                  Debug.LogError($"Didn't find mesh {kv.Value}");
               } else {
                  sk.sharedMesh = mesh;
               }
            }
         }

         foreach (var kv in u.slot_map) {
            var slot = model.GetComponentsInChildren<Transform>().Find(x => x.name == kv.Key);
            if (!slot) {
               Debug.LogError($"Missing slot {kv.Key}");
               continue;
            }

            if (slot.childCount > 0) {
               var cur = slot.GetChild(0);
               var rend = cur.GetComponent<Renderer>();
               cur.gameObject.SetActive(false);
               if (kv.Value.Length == 0) {
                  // Debug.Log($"destroyed {cur.name}");
                  continue;
               }
            }

            if (kv.Value.Length == 0) {
               continue;
            }

            if (!parsed_pipeline_data.parts.TryGetValue(kv.Value, out var part_o)) {
               LogMissing($"Missing part METADATA {kv.Value} in parts bundle, from model {u.model_name}!");

               continue;
            }

            var part = part_o.part_prefab;
            if (!part) {
               LogMissing($"Missing part MODEL {kv.Value} in parts bundle, from model {u.model_name}!");
               return;
            }

            AddThemePartTo(part_o, slot, u.theme_color);
         }

         model.name = u.out_name;
      });
      return model_prefab;
   }

   public void SetActiveUnit(ParsedUnit u) {
      if (sprite_capture_pipeline.exporting) return;

      var model_prefab = PrepModelObject(u);

      sprite_capture_pipeline.model.animation_clip = u.animations[0].clip;
   }


   IEnumerator RunParsedPipeline(ParsedPipelineData parsed) {
      foreach (var u in parsed.units) {
         var model_prefab = PrepModelObject(u);
         foreach (var yield_st in RunOutput_Parsed(model_prefab, u)) {
            yield return yield_st;
         }
      }
   }

   HashSet<string> log_cache = new();

   void LogMissing(string msg) {
      if (log_cache.Add(msg)) Debug.Log(msg);
   }

   // Hello
   void SetHeavyInfantryNames(int abc) {
   }

   void SetArcherNames() {
   }

   ModelDataStuff MakeHeavyData() {
      var res = new ModelDataStuff();
      res.name = "Heavy Infantry";
      {
         string[] skinned_names = {
            "PT_Male_Armor_01_A_body",
            "PT_Male_Armor_01_A_boots",
            "PT_Male_Armor_01_A_cape",
            "PT_Male_Armor_01_A_gauntlets",
            "PT_Male_Armor_01_A_legs",
            "PT_Male_Armor_hair_01",
            "PT_Male_Armor_head_01",
            "PT_Male_Armor_01_A_helmet",
         };

         res.skins_to_transform["Body_Armor"] = "PT_Male_Armor_01_A_body";
         res.skins_to_transform["Leg_Armor"] = "PT_Male_Armor_01_A_legs";
         res.skins_to_transform["Gauntlets"] = "PT_Male_Armor_01_A_gauntlets";
         res.skins_to_transform["Boots"] = "PT_Male_Armor_01_A_boots";
         res.skins_to_transform["Cape"] = "PT_Male_Armor_01_A_cape";
         res.skins_to_transform["Helmet"] = "PT_Male_Armor_01_A_helmet";

         res.slot_to_transform["Main_Hand"] = "PT_Right_Hand_Weapon_slot";
         res.slot_to_transform["Off_Hand_Shield"] = "Shield";
         res.slot_to_transform["Off_Hand"] = "PT_Left_Hand_Weapon_slot";
         res.slot_to_transform["NewHelmet"] = "HelmetAttachment";
      }
      return res;
   }

   ModelDataStuff MakeArcherDAta() {
      var res = new ModelDataStuff();
      res.name = "Archer";
      {
         string[] skinned_names = {
            "PT_Male_Armor_03_A_body",
            "PT_Male_Armor_05_A_boots",
            "PT_Male_Armor_01_A_cape",
            "PT_Male_Armor_03_B_gauntlets",
            "PT_Male_Armor_03_A_legs",
            "PT_Male_Armor_hair_01",
            "PT_Male_Armor_head_01",
         };

         res.skins_to_transform["Body_Armor"] = "PT_Male_Armor_03_A_body";
         res.skins_to_transform["Leg_Armor"] = "PT_Male_Armor_03_A_legs";
         res.skins_to_transform["Gauntlets"] = "PT_Male_Armor_03_B_gauntlets";
         res.skins_to_transform["Boots"] = "PT_Male_Armor_05_A_boots";
         res.skins_to_transform["Cape"] = "PT_Male_Armor_01_A_cape";

         res.slot_to_transform["Main_Hand"] = "PT_Right_Hand_Weapon_slot";
         res.slot_to_transform["Off_Hand"] = "PT_Left_Hand_Weapon_slot";
         res.slot_to_transform["Off_Hand_Shield"] = "Shield";
         res.slot_to_transform["NewHelmet"] = "HelmetAttachment";
      }
      return res;
   }


   bool sheets_inited;

   void InitSheets() {
      if (!dummy_holder) dummy_holder = new GameObject("DummyHolder").transform;
      dummy_holder.gameObject.SetActive(false);
      if (sheets_inited) return;
      sheets_inited = true;
      sheets_pipeline_descriptor.InitData();
      sprite_gen_meta = new();
      model_mappings = new();

      animation_sets = GetanimationSets();

      if (!animation_sets.ContainsKey("Poleaxe")) {
         animation_sets["Poleaxe"] = animation_sets["Spearman"];
         // Debug.Log($"Missing animation Poleaxe");
      }

      if (!animation_sets.ContainsKey("Crossbow")) {
         // animation_sets["Crossbow"] = animation_sets["Brute"];
         // Debug.Log($"Missing animation Crossbow");
      }

      {
         var datas = new[] { MakeHeavyData(), MakeArcherDAta() };

         foreach (var x in datas) {
            model_mappings[x.name] = x;
         }

         model_mapping_by_body_type = new();

         var model_types = Resources.LoadAll<ModelBodyCategory>("");

         // Debug.Log($"Mods: {model_types.join(", ", x => x.name)}");
         foreach (var mt in model_types) {
            var res = new ModelDataStuff();
            res.name = mt.model_root_prefab.name;
            if (model_mappings.TryGetValue(res.name, out var cv)) {
               cv.body_category = mt;
               model_mapping_by_body_type[mt.bodyTypeName] = cv;
               // Debug.Log($"Duplicate model: {res.name}");
               continue;
            }

            res.body_category = mt;

            /*
            res.skins_to_transform["Body_Armor"] = "";
            res.skins_to_transform["Leg_Armor"] = "";
            res.skins_to_transform["Gauntlets"] = "";
            res.skins_to_transform["Boots"] = "";
            res.skins_to_transform["Cape"] = "";

            res.slot_to_transform["Main_Hand"] = "";
            res.slot_to_transform["Off_Hand"] = "";
            res.slot_to_transform["Off_Hand_Shield"] = "";
            res.slot_to_transform["NewHelmet"] = "";
            */
            model_mappings[res.name] = res;
            model_mapping_by_body_type[mt.bodyTypeName] = res;
         }
      }


      parsed_pipeline_data = GetParsed_Sheets();
   }

   ParsedPipelineData parsed_pipeline_data;


   Dictionary<string, ModelDataStuff> model_mapping_by_body_type;

   Dictionary<string, ModelDataStuff> model_mappings;
   Dictionary<string, AnimationSet> animation_sets;

   IEnumerator RunPipeline() {
      time_benchmark = new();


      export_tex = new Texture2D(export_size, export_size);


      export_tex_sprites_w = atlas_sprites_per_row;
      if (export_tex_sprites_w > parsed_pipeline_data.output_n) export_tex_sprites_w = parsed_pipeline_data.output_n;
      while (parsed_pipeline_data.output_n / export_tex_sprites_w > 2 * (export_tex_sprites_w + 5)) {
         export_tex_sprites_w = export_tex_sprites_w * 2;
      }

      out_sprite_count = parsed_pipeline_data.output_n;
      out_grid = new(export_tex_sprites_w,
         (out_sprite_count + export_tex_sprites_w - 1) / export_tex_sprites_w);

      if (only_atlas_meta) {
         foreach (var u in parsed_pipeline_data.units) {
            foreach (var FileOutput in GetFileOutputs(u)) {
               MoveMeta(FileOutput);
            }
         }

         FinalizeMetaFile();
         if (progress_text) {
            progress_text.transform.parent.gameObject.SetActive(false);
         }

         yield break;
      }

      UpdateProgress();

      Debug.Log($"grid: {parsed_pipeline_data.output_n}, {out_grid}");

      export_tex_tot = new Texture2D(export_size * out_grid.x, out_grid.y * export_size);

      export_tex_tot.SetPixels(new Color[export_tex_tot.width * export_tex_tot.height]);

      time_benchmark.Lap("Setup");

      sprite_capture_pipeline.time_benchmark = time_benchmark;

      yield return RunParsedPipeline(parsed_pipeline_data);

      export_tex_tot.Apply();

      var full_time = time_benchmark.LogTimes(out_sprite_count);

      var rb = export_tex_tot.EncodeToPNG();

      if (write_files) {
         File.WriteAllBytes($"{export_to_folder}/atlas.png", rb);
         FinalizeMetaFile();
      }


      if (progress_text) {
         progress_text.transform.parent.gameObject.SetActive(false);
      }

      CompleteJingle();
   }

   public void OpenOutputFolder() {

#if UNITY_EDITOR
      UnityEditor.EditorUtility.RevealInFinder($"{export_to_folder}/atlas.png");
#endif

   }

   void CompleteJingle() {

      var pcp = SoundManager.instance.production_confirmed;

      pcp.playOnAwake = false;
      var p = Instantiate(pcp);
      pcp.playOnAwake = true;

      var cl = SoundManager.instance.click;
      cl.playOnAwake = false;
      var c = Instantiate(cl);
      cl.playOnAwake = true;

      var dp = AudioSettings.dspTime;
      
      p.PlayScheduled(dp);
      c.PlayScheduled(dp + 0.165f);
      
      

   }
   
}