﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shared;
using UnityEngine;
using TMPro;
using Unity.Profiling;
using UnityEngine.Events;
using UnityEngine.Serialization;
using MetaRow = GeneratedSpritesContainer.MetaRow;
using Object = UnityEngine.Object;

[DefaultExecutionOrder(20)]
public class ExportPipeline : MonoBehaviour {
   void OnValidate() {
      tag = "ExportPipeline";
   }

   public ModelHandle model;


   [Header("Export Pipeline Description")]
   public string export_sheet_name = "atlas";

   public ExportPipelineSheets sheets_pipeline_descriptor;

   [FormerlySerializedAs("override_units")]
   public UnitTypeForRender[] extra_units_to_render;

   public int export_size = 64;

   [Header("Exported Atlas Name")] public string prepend_to_sprite_name;
   public string append_to_atlas_name;

   public const string defalt_export_dir = "../NightfallRogue/Packages/nightfall_sprites/Resources";


   [Header("Debug Helpers")] public bool export_when_done;
   public UnitTypeForRender load_unit_on_play;
   [Header("Pipeline Toggles")] public bool idle_only;

   public static bool export_override;

   public bool only_atlas;

   public bool only_atlas_meta;

   public bool write_files = true;

   [Header("Not very useful now")] public ModelPartsBundle parts_bundle;

   public int atlas_sprites_per_row = 5;
   public string export_to_folder = "Gen";


   [Header("Unit Filter")] [Header("Bindings")]
   public AnimationManager animation_manager;

   public SpriteCapturePipeline sprite_capture_pipeline;

   public TMP_Text progress_text;

   public string defaultRenderModelName = "Heavy Infantry";

   public SpriteRenderDetails[] default_shot_types = new SpriteRenderDetails[] {
      new SpriteRenderDetails() {
         pitch_angle = 10,
         sprite_pixel_scale = 2,
         yaw_angle = 30,
      }
   };

   void OnEnable() {
      if (model) sprite_capture_pipeline.model = model;
      else model = sprite_capture_pipeline.model;
      if (!model) {
         Debug.LogError("Please set model!");
         enabled = false;
      }
      sprite_capture_pipeline.size = export_size * sprite_capture_pipeline.export_resolution_downscale;
   }

   void Start() {
      if (progress_text) {
         progress_text.transform.parent.gameObject.SetActive(false);
      }

      InitSheets();
      if (to_visit) AnnotatedUI.Visit(to_visit, this).alwaysRevisit = true;
   }

   UnitTypeForRender cur_loaded;

   void Update() {
      if (sprite_capture_pipeline.exporting) {
         cur_loaded = null;
         return;
      }


      if (load_unit_on_play != cur_loaded) {
         cur_loaded = load_unit_on_play;
         var u = parsed_pipeline_data.units.Find(x => x.out_name == cur_loaded.export_name);
         if (u == null) {
            Debug.Log($"Didn't find {cur_loaded.export_name} in export data!");
            return;
         }

         SetActiveUnit(u);
      }
   }

   int output_i = 0;

   [Header("Debug")] public bool executed;

   public void ExecuteMyPipeline() {
      if (executed) return;

      executed = true;
      sprite_capture_pipeline.exporting = true;

      IEnumerator SubPl() {
         yield return
            StartCoroutine(RunPipeline());


         sprite_capture_pipeline.exporting = false;
      }

      export_files_action = () => {
         export_when_done = true;
         export_files_action = null;
      };

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

   List<MetaRow> sprite_gen_meta = new();


   int _ResultHolderTicker;

   public SpriteCaptureResultHolder NewResultHolder(string name) {
      var a = new GameObject(name).AddComponent<SpriteCaptureResultHolder>();
      a.transform.parent = dummy_holder.transform;
      a.transform.position = new Vector3((_ResultHolderTicker % 10) * 2 + 10, _ResultHolderTicker / 10);
      _ResultHolderTicker++;
      return a;
   }

   string SpriteGenMetaRow(MetaRow d) {
      var r = d.rect;
      var p = d.pivot;

      return $"{prepend_to_sprite_name}{d.file_name}\t{r.x},{r.y},{r.width},{r.height}\t{p.x},{p.y}";
   }

   public UnitViewer unit_viewer_prefab;

   UnitViewer unit_viewer_running;


   public Action start_gen {
      get {
         if (executed) return null;
         return () => { this.ExecuteMyPipeline(); };
      }
   }

   public Action view_sprites => () => { OpenUnitViewer(); };

   public Action export_files_action;
   public Action write_files_action;

   public UnityEvent onPipelineDone;

   public UnityEvent omExportDone;

   void OnDone() {
      onPipelineDone?.Invoke();
      write_files_action = () => {
         write_files_action = null;
         WriteFiles(export_to_folder);
      };
      export_files_action = () => {
         export_when_done = false;
         export_files_action = null;
         WriteFiles(defalt_export_dir);
      };
      if (export_when_done) {
         export_files_action();
      }
   }


   public void ShowUnitViewer() {
      if (export_tex_tot == null) {
         Debug.Log("Run pipeline first!");
         return;
      }

      OpenUnitViewer();
   }

   List<GeneratedSpritesContainer.UnitCats> genned_unit_cats = new();

   UnitViewer.UnitTypeDetails ParseUntP2(ParsedUnit pu, GeneratedSpritesContainer.UnitCats cats) {
      var r = new UnitViewer.UnitTypeDetails();

      r.name = pu.out_name;
      r.sprite = cats.idle_sprite;
      r.animation_sprites =
         Sprites.GetAnimationSprites(pu.out_name, AnimationSubsystem.animations_parsed, pu.animation_type,
            cats).ToArray();

      return r;
   }

   void OpenUnitViewer() {
      if (unit_viewer_running) return;

      {
         export_tex_tot.Apply();
         var catzip = parsed_pipeline_data.units.Map(x => ParseUntP2(x, x.result));

         unit_viewer_running = Instantiate(unit_viewer_prefab);

         IEnumerator CloseAfterEsc() {
            yield return null;
            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Escape));
            Destroy(unit_viewer_running.gameObject);
         }

         StartCoroutine(CloseAfterEsc());


         unit_viewer_running.SetUnits(catzip);
         return;
      }
   }

   void CopyAndDownsampleTo(Texture2D src, Texture2D dst, string FileOutput, bool mirror) {
      var cols = src.GetPixels();

      if (dst.width == src.width && dst.height == src.height) {
         dst.SetPixels(cols);
         dst.Apply();
         // DumpExport(cols, FileOutput);
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

      if (mirror) {
         for (int i = 0; i < h; i++) {
            for (int j = 0; j < w; j++) {
               LoadBuff(j, i);

               res[w - 1 - j + i * w] = MergeColors(buff);
            }
         }
      } else {
         for (int i = 0; i < h; i++) {
            for (int j = 0; j < w; j++) {
               LoadBuff(j, i);

               res[j + i * w] = MergeColors(buff);
            }
         }
      }


      dst.SetPixels(res);
      dst.Apply();
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

      sprite_gen_meta.Add(new(FileOutput, rect, GeneratedSpritesContainer.DEFAULT_PIVOT));

      return rect;
   }

   public List<Sprite> tot_sprites = new();

   static readonly ProfilerMarker _m_DumpExport = Std.Profiler<ExportPipeline>("DumpExport");

   void DumpExport(RenderTexture res, string FileOutput) {
      using var _m = _m_DumpExport.Auto();
      ei++;

      var rect = MoveMeta(FileOutput);
      RenderTexture.active = export_text_tot_r;
      var scale = new Vector2(1f / export_text_tot_r.width, 1f / export_text_tot_r.height);
      Graphics.CopyTexture(res, 0, 0, 0, 0, rect.width, rect.height, export_text_tot_r, 0, 0, rect.x, rect.y);

      Vector2 pix = new(export_tex_tot.width, export_tex_tot.height);
      var spr = GeneratedSpritesContainer.MakeSprite(export_tex_tot, $"{prepend_to_sprite_name}{FileOutput}", rect,
         new Vector2(0.5f, 0.15f));

      tot_sprites.Add(spr);

      UpdateProgress();
   }

   void DumpExport_Old(Color[] res, string FileOutput) {
      ei++;

      var rect = MoveMeta(FileOutput);


      export_tex_tot.SetPixels(rect.x, rect.y, rect.width, rect.height,
         res);

      Vector2 pix = new(export_tex_tot.width, export_tex_tot.height);
      var spr = GeneratedSpritesContainer.MakeSprite(export_tex_tot, $"{prepend_to_sprite_name}{FileOutput}", rect,
         new Vector2(0.5f, 0.15f));

      tot_sprites.Add(spr);

      UpdateProgress();
   }

   int export_tex_sprites_w;
   Vector2Int tid => new(ei % export_tex_sprites_w, ei / export_tex_sprites_w);

   int ei = 0;

   static readonly ProfilerMarker _m_RunOutputImpl = Std.Profiler<ExportPipeline>("RunOutputImpl");

   void RunOutputImpl(GeneratedSprite sprite_to_generate, Material res_mat) {
      ParsedUnit pu = sprite_to_generate.pu;
      AnimationWrap an = sprite_to_generate.an;
      SpriteRenderDetails shot_type = sprite_to_generate.shot_type;
      using var _m = _m_RunOutputImpl.Auto();
      var model = sprite_capture_pipeline.model;
      bool mirror = pu.model_body.mirror_render;
      if (an.animation_type_object && an.animation_type_object.mirror_render) mirror = !mirror;
      if (mirror) {
         var mrot = model.transform.localRotation.eulerAngles;
         mrot.y *= -1;
         model.transform.localRotation = Quaternion.Euler(mrot);
      }

      sprite_capture_pipeline.mirror_output = mirror;

      if (an.animation_type_object != null && an.animation_type_object.looping_root) {
         sprite_capture_pipeline.HandleLoopingRootMotion(an.clip, an.frame / 60f / an.clip.length);
      }

      sprite_capture_pipeline.model.SetAnimationNow(an.clip, an.frame);
      output_i++;
      var FileOutput = sprite_to_generate.name;
      // Debug.Log($"Output {output_i}: {name}");

      var folder = export_to_folder;
      int mapped_mats = 0;


      foreach (var x in model.GetComponentsInChildren<Renderer>()) {
         if (x.transform.parent.parent == model.transform) {
            x.material = res_mat;
            mapped_mats++;
         }
      }

      /*
       * 
      if (an.animation_type_object) {
         model.transform.localPosition += an.animation_type_object.model_root_pos;
         sprite_capture_pipeline.model.render_obj.transform.localRotation = an.animation_type_object.model_root_rot;
      } else {
         sprite_capture_pipeline.model.render_obj.transform.localRotation = Quaternion.identity;
      }
       */

      {
         sprite_capture_pipeline.model.render_obj.transform.localRotation =
            Quaternion.Euler(0, mirror ? -shot_type.yaw_angle : shot_type.yaw_angle, 0);
      }

      sprite_capture_pipeline.relative_model_height_for_shading = 1;

      if (pu.model_body.body_category) {
         var shading_height = pu.model_body.body_category.relative_model_height_for_shading;
         if (shading_height > 0) {
            sprite_capture_pipeline.relative_model_height_for_shading = shading_height;
         }
      }

      sprite_capture_pipeline.RunPipeline();

      DumpExport(sprite_capture_pipeline.downsampled_render_result, FileOutput);
      // CopyAndDownsampleTo(sprite_capture_pipeline.result_rexture, export_tex, FileOutput,
      //    mirror: mirror);


      if (an.animation_type_object != null) {
         var cid = output_i - 1;
         var o = sprite_gen_meta[cid];
         Vector3 model_off = an.animation_type_object.model_offsetmodel_offset;
         Vector2 camera_d = sprite_capture_pipeline.camera_handle.OrthographicRectSize;

         if (model_off != default) {
            var sprite_pivot = GetAdjustedSpritePivot(o.pivot, model_off, camera_d);
            sprite_gen_meta[cid] = new(o.file_name, o.rect, sprite_pivot);
         }
      }


      if (!only_atlas) {
         export_tex.ReadPixelsFrom(sprite_capture_pipeline.downsampled_render_result);
         var data = export_tex.EncodeToPNG();
         if (write_files) {
            Std.EnsureLocalDir($"{folder}/{export_sheet_name}");
            File.WriteAllBytes($"{folder}/{export_sheet_name}/{FileOutput}.png", data);
         }
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

   public static IEnumerable<string> GetThemes(string[] names, string[] data) {
      for (int i = 0; i < names.Length; i++) {
         if (names[i].Replace(" ", "_").StartsWith("Theme_Color")) {
            var val = data[i];
            if (val.IsNonEmpty()) yield return val;
         }
      }
   }


   public static string ToName(string s) {
      if (s.Length == 0) return "";
      return "_" + s.Replace("-", "").Replace("_", "").ToUpper();
   }

   public static Material ApplyMaterialColor(List<(string field, Color color)> colors, Material material) {
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
   public float max_waits_per_frame_viewer_open = 0.05f;

   [NonSerialized] double rt;

   IEnumerable RunOutput_Parsed(ParsedUnit pu) {
      int sprites_in = tot_sprites.Count;

      var folder = export_to_folder;
      if (!Directory.Exists(folder)) {
         Directory.CreateDirectory(folder);
      }

      Material res_mat = ApplyMaterialColor(pu.colors, pu.material);

      foreach (var sg in pu.sprites_to_generate) {
         var mb = sprite_capture_pipeline.GetMoveBackFunk();
         RunOutputImpl(sg, res_mat);

         mb();


         if (rt + (unit_viewer_running ? this.max_waits_per_frame_viewer_open : max_waits_per_frame) >
             Time.realtimeSinceStartupAsDouble) {
            continue;
         }

         yield return null;
         rt = Time.realtimeSinceStartupAsDouble;
      }

      {
         var holder = NewResultHolder($"_ UNIT {pu.out_name}");

         holder.used_material = res_mat;
      }

      var sc = GeneratedSpritesContainer.GetUnitCats(tot_sprites.SubArray(sprites_in, tot_sprites.Count));

      Debug.Assert(sc.Count == 1, $"Singe unit should get since unit cats, but got: {sc.Count} ");

      GeneratedSpritesContainer.UnitCats cat = sc.Values.First();
      PushNewRes(pu, cat);
   }

   List<GeneratedSpritesContainer.UnitCats> unit_cats_list = new();

   void PushNewRes(ParsedUnit pu, GeneratedSpritesContainer.UnitCats cat) {
      unit_cats_list.Add(cat);
      pu.result = cat;

      if (unit_viewer_running) {
         export_tex_tot.Apply();
         unit_viewer_running.AddUnit(ParseUntP2(pu, pu.result));
      }
   }

   Texture2D export_tex;
   public RenderTexture export_text_tot_r;
   public Texture2D export_tex_tot;

   public static void ApplyTheme(Color theme_color, List<(string field, Color color)> rc, string theme_1,
      string theme_2,
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


   Dictionary<string, Material> material_map;
   Material atoon_mat;

   public Material MapMat(Material a, string context) {
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
      }

      return material_map.Get(a.name, a);
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


   public static string GetExportUnitName(string orig_name) {
      return Sprites.GetExportUnitName(orig_name);
   }

   [SerializeField] public Transform dummy_holder;


   ModelBodyRoot PrepModelObject(ParsedUnit u) {
      GameObject model_prefab = u.model_body.body_category?.model_root_prefab?.gameObject ??
                                Resources.Load<GameObject>($"BaseModels/{u.model_name}");
      var omodel_prefab = model_prefab;

      var model_body_prefab = model_prefab.GetComponent<ModelBodyRoot>();


      if (!model_body_prefab) {
         Debug.Log($"missing body root for: {model_prefab.name}");
      }

      model_body_prefab = null;

      sprite_capture_pipeline.model.model_offset = u.model_body.body_category.model_offset;

      void RunLoadout(Action<GameObject> model_action) {
         model_prefab = omodel_prefab;

         // Debug.Log($"{model_name}: {model_prefab}");
         if (model_prefab) {
            var mod = sprite_capture_pipeline.model.ResetModel(model_prefab, model_action);

            sprite_capture_pipeline.model.animation_clip = u.animations.First().clip;

            model_prefab = mod;
         } else {
            Debug.LogError($"Missing model: {u.model_name}");
         }
      }

      RunLoadout(model => {
         var model_body = model.GetComponent<ModelBodyRoot>();
         model_body_prefab = model_body;
         if (!model_body) {
            return;
         }

         var skinned_rend = model_body.renderers.Where(x => x.transform.parent == model.transform).ToArray();

         foreach (var kv in u.skin_map) {
            var sk_raw = skinned_rend.Find(x => x.name == kv.Key);

            var sk = (SkinnedMeshRenderer)sk_raw;

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
            Transform slot = null;

            if (kv.Key is "Main_Hand") {
               slot = model_body.Main_Hand;
            } else if (kv.Key is "Off_Hand_Shield") {
               slot = model_body.Off_Hand_Shield;
            } else if (kv.Key is "Off_Hand") {
               slot = model_body.Off_Hand;
            } else if (kv.Key is "NewHelmet") {
               slot = model_body.NewHelmet;
            }

            if (!slot) {
               Debug.LogError($"Missing slot {kv.Key} for unit {u.out_name}, model: {u.model_name}");
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

         // model.name = u.out_name;
      });
      return model_body_prefab;
   }

   public void SetActiveUnit(ParsedUnit pu) {
      if (sprite_capture_pipeline.exporting) return;

      var model_body = PrepModelObject(pu);

      Material res_mat = ApplyMaterialColor(pu.colors, pu.material);

      foreach (var x in model.GetComponentsInChildren<Renderer>()) {
         if (x.transform.parent.parent == model.transform && !x.transform.parent.name.EndsWith($"(Clone)")) {
            x.material = res_mat;
         }
      }

      if (pu.model_body.body_category) {
         var r = pu.model_body.body_category.relative_model_height_for_shading;
         sprite_capture_pipeline.relative_model_height_for_shading = r == 0 ? 1 : r;
      }

      sprite_capture_pipeline.model.animation_clip = pu.animations[0].clip;
   }


   IEnumerator RunParsedPipeline(ParsedPipelineData parsed) {
      foreach (var u in parsed.units) {
         var model = PrepModelObject(u);
         foreach (var yield_st in RunOutput_Parsed(u)) {
            yield return yield_st;
         }
      }
   }

   HashSet<string> log_cache = new();

   void LogMissing(string msg) {
      if (log_cache.Add(msg)) Debug.Log(msg);
   }

   BodyModelData MakeHeavyData() {
      var res = new BodyModelData();
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


   bool sheets_inited;

   void InitSheets() {
      if (!dummy_holder) dummy_holder = new GameObject("DummyHolder").transform;
      if (sheets_inited) return;
      sheets_inited = true;
      sheets_pipeline_descriptor.InitData();
      sprite_gen_meta = new();
      model_mappings = new();

      if (!animation_manager) {
         var prefab = Resources.Load<AnimationManager>("AnimationManager");
         animation_manager = Instantiate(prefab, transform);
      }

      animation_manager.Init();


      {
         var datas = new[] { MakeHeavyData() };

         foreach (var x in datas) {
            model_mappings[x.name] = x;
         }

         model_mapping_by_body_type = new();

         var model_types = Resources.LoadAll<ModelBodyCategory>("");

         // Debug.Log($"Mods: {model_types.join(", ", x => x.name)}");
         foreach (var mt in model_types) {
            var init = MakeModelData(mt);
         }
      }


      parsed_pipeline_data = new(sheets_pipeline_descriptor, this);
      // if (this.generate_only.IsNonEmpty()) {
      //    parsed_pipeline_data.units.Filter(x =>
      //       generate_only.Exists(ut => ut.name == x.raw_name || ut.Unit_Name == x.raw_name));
      //    parsed_pipeline_data.output_n = parsed_pipeline_data.units.Sum(x => x.animations.Count);
      // }
   }

   public BodyModelData MakeModelData(ModelBodyCategory mt) {
      if (!mt.model_root_prefab) {
         throw new Exception($"Model: {mt.name} lacks root prefab! {mt.model_root_prefab}");
      }

      if (!model_mappings.TryGetValue(mt.model_root_prefab.name, out var res)) {
         res = new BodyModelData();
         res.name = mt.model_root_prefab.name;

         res.body_category = mt;

         // res.skins_to_transform["Body_Armor"] = "";

         // res.slot_to_transform["Main_Hand"] = "";
         model_mappings[res.name] = res;
         model_mapping_by_body_type[mt.bodyTypeName] = res;
         res.material = mt.material;
         res.mirror_render = mt.mirror_render;
      } else {
         if (!res.material) res.material = mt.material;
      }

      res.body_category ??= mt;
      model_mapping_by_body_type[mt.bodyTypeName] = res;
      // Debug.Log($"Duplicate model: {res.name}");
      return res;
   }

   ParsedPipelineData parsed_pipeline_data;


   public Dictionary<string, BodyModelData> model_mapping_by_body_type;

   public Dictionary<string, BodyModelData> model_mappings;

   public RectTransform to_visit;


   IEnumerator RunPipeline() {
      if (write_files) {
         Std.EnsureLocalDir(export_to_folder);
      }

      time_benchmark = new();


      dummy_holder.gameObject.SetActive(false);
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
            foreach (var sg in u.sprites_to_generate) {
               MoveMeta(sg.name);
            }
         }

         if (progress_text) {
            progress_text.transform.parent.gameObject.SetActive(false);
         }

         if (write_files) {
            WriteMetaFile(export_to_folder);
         }

         yield break;
      }

      UpdateProgress();

      Debug.Log($"grid: {parsed_pipeline_data.output_n}, {out_grid}");

      export_tex_tot = new Texture2D(export_size * out_grid.x, out_grid.y * export_size);

      export_text_tot_r = export_tex_tot.GetRenderTextureFor();

      export_text_tot_r.Clear(default);

      time_benchmark.Lap("Setup");

      sprite_capture_pipeline.time_benchmark = time_benchmark;

      yield return RunParsedPipeline(parsed_pipeline_data);
      export_tex_tot.ReadPixelsFrom(export_text_tot_r);
      export_tex_tot.Apply();

      var full_time = time_benchmark.LogTimes(out_sprite_count);


      if (write_files) {
         WriteFiles(export_to_folder);
      }


      if (progress_text) {
         progress_text.transform.parent.gameObject.SetActive(false);
      }

      dummy_holder.gameObject.SetActive(true);

      OnDone();
      CompleteJingle();
   }


   public void WriteMetaFile(string to_folder) {
      var meta_rows = sprite_gen_meta.Select(SpriteGenMetaRow).ToArray();

      var mf = $"{to_folder}/{export_sheet_name}{append_to_atlas_name}.spritemeta";
      File.WriteAllText(mf, meta_rows.join("\n"));

      // File.Copy(mf, $"{export_to_folder}/test_atlas.spritemeta", overwrite: true);
   }

   public void WriteTexturefile(string to_folder) {
      var rb = export_tex_tot.EncodeToPNG();
      File.WriteAllBytes($"{to_folder}/{export_sheet_name}{append_to_atlas_name}.png", rb);
      var meta_rows = sprite_gen_meta.Select(SpriteGenMetaRow).ToArray();
   }

   public void WriteFiles(string to_folder) {
      if (!only_atlas_meta) WriteTexturefile(to_folder);
      WriteMetaFile(to_folder);

      omExportDone?.Invoke();
   }

   public Action open_output_folder => OpenOutputFolder;

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