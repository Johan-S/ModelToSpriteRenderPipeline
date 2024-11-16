using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shared;
using UnityEngine;
using TMPro;
using Unity.Profiling;
using UnityEditor;
using UnityEngine.Events;
using UnityEngine.Serialization;
using static Shared.AnimationTypeObject.EffectPosMarkerTags;
using static UnityEngine.Mathf;
using MetaRow = GeneratedSpritesContainer.MetaRow;
using Object = UnityEngine.Object;

[DefaultExecutionOrder(20)]
[InitializeOnLoad]
public class ExportPipeline : MonoBehaviour {
   public static ExportPipeline exporting;

   [Serializable]
   [Stat("Export Pipeline Prefs")]
   public class ExportPipelinePrefs {
      public bool log_info;
   }

   public static ExportPipelinePrefs prefs = StandardLib.EditorSettingsHandler.Register<ExportPipelinePrefs>();

   public static ExportPipelinePrefs editor_prefs => prefs;

   static bool loginfo => prefs.log_info;

   void OnValidate() {
      tag = "ExportPipeline";
   }


   [Tooltip("Name of the output sheets, overwrites other sheets that has the same name.")]
   public string export_sheet_name = "atlas";

   [Space]
   [Tooltip(
      "Loads units on startup, doesn't add it to pipeline this is just to see what it loos like without running the pipeline.")]
   public UnitTypeForRender load_unit_on_play;

   [Tooltip(
      "When set this applies the full animation and pipeline logic at load, including rotations etc. Overrides a lot of other controls.")]
   public Shared.AnimationTypeObject load_animation_on_play;

   [Space]
   [InspectorName("UnitsToRender")]
   [Tooltip("These are the units that will get rendered in the pipeline and exported as sheets.")]
   public UnitTypeForRender[] extra_units_to_render;


   [Range(0.1f, 5)] [Tooltip("Scales up camera capture area, adding pixels to capture larger models.")]
   public float capture_scale = 1;

   [Tooltip("Pixels per sprite, before capture scale is applied")]
   public int export_size = 64;

   [Tooltip("The angles the differnet sprites will be taken in. Sprite pixel scale doesn't work.")]
   public SpriteRenderDetails[] default_shot_types = new SpriteRenderDetails[] {
      new SpriteRenderDetails() {
         pitch_angle = 10,
         sprite_pixel_scale = 2,
         yaw_angle = 30,
      }
   };

   public int effective_export_size => CeilToInt(export_size * capture_scale / 8) * 8;


   public string prepend_to_sprite_name;

   [Space] [Header("Export Pipeline Description")]
   public ExportPipelineSheets sheets_pipeline_descriptor;

   public const string defalt_export_dir = "../NightfallRogue/Packages/nightfall_sprites/Resources";


   [Space] [Header("Pipeline Toggles")] public bool compress_rects;

   public bool idle_only;
   public bool export_when_done;
   public static bool export_override;

   public bool only_atlas;

   public bool write_files = true;

   [Header("Not very useful now")] public ModelPartsBundle parts_bundle;

   [NonSerialized] public int atlas_sprites_per_row = 5;
   public string export_to_folder = "Gen";


   [Header("Unit Filter")] [Header("Bindings")]
   public AnimationManager animation_manager;


   public ModelHandle model;
   public SpriteCapturePipeline sprite_capture_pipeline;

   public TMP_Text progress_text;

   public string defaultRenderModelName = "Heavy Infantry";

   void OnEnable() {
      if (model) sprite_capture_pipeline.model = model;
      else model = sprite_capture_pipeline.model;
      if (!model) {
         Debug.LogError("Please set model!");
         enabled = false;
      }

      UpdateCaptureScale();
      if (!default_shot_types.IsEmpty()) SetShotType(default_shot_types[0]);
   }

   public void SetShotType(SpriteRenderDetails shot_type) {
      var cam = sprite_capture_pipeline.camera_handle;
      cam.camera_pitch_angle = shot_type.pitch_angle;
      cam.camera_yaw = shot_type.yaw_angle;
      cam.UpdateCamera();
   }

   public bool UpdateCaptureScale() {
      var ns = effective_export_size * sprite_capture_pipeline.export_resolution_downscale;
      sprite_capture_pipeline.camera_handle.camera_size = capture_scale * 4;
      sprite_capture_pipeline.camera_handle.UpdateCamera();
      if (ns != sprite_capture_pipeline.size) {
         sprite_capture_pipeline.size = ns;
         sprite_capture_pipeline.InitTextures(force: true);
         return true;
      }

      return false;
   }

   void Start() {
      if (progress_text) {
         progress_text.transform.parent.gameObject.SetActive(false);
      }

      InitSheets();
      if (to_visit) AnnotatedUI.Visit(to_visit, this).alwaysRevisit = true;

      StartCoroutine("GCCoroutine");
   }

   public IEnumerator GCCoroutine() {
      while (true) {
         yield return new WaitForSeconds(10);
         GC.Collect();
         Resources.UnloadUnusedAssets();
      }
   }

   UnitTypeForRender cur_loaded;

   void Update() {
      if (sprite_capture_pipeline.exporting) {
         cur_loaded = null;
         return;
      }

      if (UpdateCaptureScale()) {
         sprite_capture_pipeline.RunPipeline();
         sprite_capture_pipeline.PushToResultTextures();
      }


      if (load_unit_on_play != cur_loaded) {
         cur_loaded = load_unit_on_play;
         var u = parsed_pipeline_data_orig.units.Find(x => x.out_name == cur_loaded.export_name);
         if (u == null) {
            u = parsed_pipeline_data_orig.CreateParsedUnit(load_unit_on_play);
            // Debug.Log($"Didn't find {cur_loaded.export_name} in export data!");
            // return;
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
      exporting = this;

      IEnumerator SubPl() {
         yield return
            StartCoroutine(RunPipeline());

         exporting = null;
         sprite_capture_pipeline.exporting = false;
      }

      export_files_action = () => {
         export_when_done = true;
         export_files_action = null;
      };

      StartCoroutine(SubPl());
   }

   TimeBenchmark time_benchmark;

   List<MetaRow> sprite_gen_meta = new();


   int _ResultHolderTicker;

   public SpriteCaptureResultHolder NewResultHolder(string name) {
      var a = new GameObject(name).AddComponent<SpriteCaptureResultHolder>();
      a.transform.parent = dummy_holder.transform;
      a.transform.position = new Vector3((_ResultHolderTicker % 10) * 2 + 10, _ResultHolderTicker / 10 + 40);
      _ResultHolderTicker++;
      return a;
   }

   string SpriteGenMetaRow(MetaRow d) {
      var r = d.rect;
      var p = d.pivot;

      return $"{d.file_name}\t{r.x},{r.y},{r.width},{r.height}\t{p.x},{p.y}";
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
      r.animation_sprites = AnimationSubsystem.GetAnimationSprites(pu.out_name, pu.generated_animation_datas, cats,
            strip_missing_silently: false)
         .ToArray();
//          AnimationSubsystem.GetAnimationSprites(pu.out_name, AnimationSubsystem.animations_parsed, pu.animation_type,
//            cats, strip_missing_silently: true).ToArray();

      return r;
   }

   void OpenUnitViewer() {
      if (unit_viewer_running) return;

      {
         export_tex_tot.Apply();
         var catzip = parsed_pipeline_data_orig.units.Map(x => ParseUntP2(x, x.result));

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

   void UpdateProgress() {
      if (progress_text) {
         progress_text.transform.parent.gameObject.SetActive(true);

         progress_text.text = $"{ei} / {tot_out_sprites_count}";
      }
   }

   Vector2Int out_grid;
   Vector2Int out_grid_pix => out_grid * effective_export_size;
   int out_sprite_count;
   int tot_out_sprites_count;

   int batch_id;

   RectInt MoveMeta(string FileOutput, int2 exp_size, Vector2 pivot) {
      var ei = sprite_gen_meta.Count;

      var px = out_grid_pix;

      // Vector2Int tid = new(ei % export_tex_sprites_w, ei / export_tex_sprites_w);
      Vector2Int tid = export_tex_cursor;

      if (exp_size.x + tid.x > px.x) {
         tid = new Vector2Int(0, tid.y + export_tex_height_cursor);
         export_tex_height_cursor = 0;
      }

      export_tex_height_cursor = Mathf.Max(export_tex_height_cursor, exp_size.y);
      export_tex_cursor = tid + new Vector2Int(exp_size.x, 0);

      var pos = tid;
      ei++;

      pos.y = px.y - exp_size.y - pos.y;


      var rect = new RectInt(pos.x, pos.y, exp_size.x, exp_size.y);

      sprite_gen_meta.Add(new(FileOutput, rect, pivot));

      return rect;
   }

   public List<Sprite> tot_sprites = new();

   static readonly ProfilerMarker _m_DumpExport = Std.Profiler<ExportPipeline>("DumpExport");

   void DumpExport(RenderTexture res, string FileOutput) {
      using var _m = _m_DumpExport.Auto();
      ei++;
      RectInt from_r = new RectInt(0, 0, effective_export_size, effective_export_size);
      var pivot = sprite_capture_pipeline.camera_handle.camera_pivot;
      if (compress_rects) {
         Vector2Int osz = new Vector2Int(effective_export_size, effective_export_size);
         Vector2 pivot_px = pivot * osz;
         using var rect_buffer = ComputeShaderUtils.gpu_GetVisibleRect(res);

         rect_buffer.Read();
         var r = rect_buffer[0];

         from_r.max = Vector2Int.Min(from_r.max, from_r.min + new int2(r.z.Round(), r.w.Round()) + new int2(1, 1));
         from_r.min += Vector2Int.Max(new int2(r.x.Round(), r.y.Round()) - new int2(1, 1), new Vector2Int(0, 0));

         var po2 = pivot_px - from_r.min;
         pivot = po2 / from_r.size;
      }

      var rect = MoveMeta(FileOutput, from_r.size, pivot);


      RenderTexture.active = export_text_tot_r;
      var scale = new Vector2(1f / export_text_tot_r.width, 1f / export_text_tot_r.height);
      Graphics.CopyTexture(res, 0, 0, from_r.x, from_r.y, from_r.width, from_r.height, export_text_tot_r, 0, 0, rect.x,
         rect.y);

      UpdateProgress();
   }

   int export_tex_sprites_w;

   Vector2Int export_tex_cursor;
   int export_tex_height_cursor;

   Vector2Int tid => new(ei % export_tex_sprites_w, ei / export_tex_sprites_w);

   int ei = 0;

   static readonly ProfilerMarker _m_RunOutputImpl = Std.Profiler<ExportPipeline>("RunOutputImpl");

   public void SetModelRotation(ParsedUnit pu, AnimationTypeObject animation_type_object,
      SpriteRenderDetails shot_type) {
      SetShotType(shot_type);

      model.render_obj.transform.localPosition = new Vector3();
      bool mirror = pu.model_body.mirror_render;
      var ano = animation_type_object;
      if (animation_type_object && animation_type_object.mirror_render) mirror = !mirror;
      Quaternion perspectove_rot = ano ? Quaternion.Euler(ano.model_perspective_rot) : Quaternion.identity;
      if (mirror) {
         sprite_capture_pipeline.camera_handle.camera_yaw *= -1;
         sprite_capture_pipeline.camera_handle.UpdateCamera();
      }

      if (mirror) {
         var mrot = model.transform.localRotation.eulerAngles;
         mrot.y *= -1;
         model.transform.localRotation =
            perspectove_rot * Quaternion.Euler(mrot);
      } else {
         model.transform.localRotation = perspectove_rot * model.transform.localRotation;
      }

      sprite_capture_pipeline.mirror_output = mirror;
      model.render_obj.transform.localRotation = Quaternion.identity;
      if (animation_type_object) {
         model.transform.localPosition += animation_type_object.model_root_pos;
         if (animation_type_object.model_root_rot.w != default) {
            model.render_obj.transform.localRotation = animation_type_object.model_root_rot;
         }
      } else {
         // sprite_capture_pipeline.model.render_obj.transform.localRotation = Quaternion.identity;
      }
   }

   public void OverrideRunSpritePipeline(GeneratedSprite sprite_to_generate, Material res_mat) {
      ParsedUnit pu = sprite_to_generate.pu;
      AnimationWrap an = sprite_to_generate.an;
      SpriteRenderDetails shot_type = sprite_to_generate.shot_type;
      using var _m = _m_RunOutputImpl.Auto();
      var model = sprite_capture_pipeline.model;

      SetModelRotation(pu, an.animation_type_object, sprite_to_generate.shot_type);
      {
         bool old_neg = model.negate_root_motion;
         bool old_app = model.model_root.applyRootMotion;
         try {
            var animation_type_object = an.animation_type_object;
            if (animation_type_object != null) {
               if (animation_type_object.negate_root_motion) {
                  model.model_root.applyRootMotion = true;
                  model.negate_root_motion = true;
               } else {
                  if (animation_type_object.looping_root) {
                     sprite_capture_pipeline.HandleLoopingRootMotion(an.clip, an.frame / 60f / an.clip.length);
                  }
               }
            }

            sprite_capture_pipeline.model.SetAnimationNow(an.clip, an.frame);
         }
         finally {
            model.negate_root_motion = old_neg;
            model.model_root.applyRootMotion = old_app;
         }
      }

      output_i++;
      // Debug.Log($"Output {output_i}: {name}");

      int mapped_mats = 0;

      if (res_mat) {
         foreach (var x in model.GetComponentsInChildren<Renderer>()) {
            if (x.transform.parent.parent == model.transform) {
               x.material = res_mat;
               mapped_mats++;
            }
         }
      }

      {
         // sprite_capture_pipeline.model.render_obj.transform.localRotation =
         //   Quaternion.Euler(0, mirror ? -shot_type.yaw_angle : shot_type.yaw_angle, 0);
      }

      sprite_capture_pipeline.relative_model_height_for_shading = 1;

      if (pu.model_body.body_category) {
         var shading_height = pu.model_body.body_category.relative_model_height_for_shading;
         if (shading_height > 0) {
            sprite_capture_pipeline.relative_model_height_for_shading = shading_height;
         }
      }

      sprite_capture_pipeline.RunPipeline();
   }

   void RunOutputImpl(GeneratedSprite sprite_to_generate, Material res_mat) {
      OverrideRunSpritePipeline(sprite_to_generate, res_mat);

      DumpExport(sprite_capture_pipeline.downsampled_render_result, sprite_to_generate.name);
      // CopyAndDownsampleTo(sprite_capture_pipeline.result_rexture, export_tex, FileOutput,
      //    mirror: mirror);

      if (sprite_to_generate.an.animation_type_object != null) {
         var cid = output_i - 1;
         var o = sprite_gen_meta[cid];
         Vector3 model_off = sprite_to_generate.an.animation_type_object.model_offsetmodel_offset;
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
            Std.EnsureLocalDir($"{export_to_folder}/{export_sheet_name}");
            File.WriteAllBytes($"{export_to_folder}/{export_sheet_name}/{sprite_to_generate.name}.png", data);
         }
      }

      var mb = model.model_root.GetComponent<ModelBodyRoot>();

      if (mb) {
         bool mirror = sprite_to_generate.pu.model_body.mirror_render !=
                       sprite_to_generate.an.animation_type_object.mirror_render;
         var mat = sprite_capture_pipeline.camera_handle.transform.rotation;
         mat = Quaternion.Inverse(mat);
         // var mat2 = Quaternion.Euler(30, 0, 0);
         var cp = sprite_capture_pipeline.camera_handle.transform.position;

         Vector3 Trans(Vector3 p) {
            p = mat * p;
            if (mirror) p.x *= -1;
            return p;
         }


         if (mb.Main_Hand) {
            sprite_to_generate.marker_locations[Main_Hand] = Trans(mb.Main_Hand.transform.position);
         }

         if (mb.Off_Hand) {
            sprite_to_generate.marker_locations[Off_Hand] = Trans(mb.Off_Hand.transform.position);
         }

         if (mb.Mouth) {
            sprite_to_generate.marker_locations[Mouth] = Trans(mb.Mouth.transform.position);
         }

         if (mb.Tail) {
            sprite_to_generate.marker_locations[Tail] = Trans(mb.Tail.transform.position);
         }
      }

      if (res_mat) {
         foreach (var x in model.GetComponentsInChildren<Renderer>()) {
            if (x.transform.parent.parent == model.transform) {
               x.material = res_mat;
            }
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

      Material res_mat = !pu.material ? null : ApplyMaterialColor(pu.colors, pu.material);

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
   }

   List<GeneratedSpritesContainer.UnitCats> unit_cats_list = new();

   void PushNewRes(ParsedUnit pu, GeneratedSpritesContainer.UnitCats cat) {
      unit_cats_list.Add(cat);
      pu.result = cat;
      FillAnimationDatas("", pu);

      if (unit_viewer_running) {
         export_tex_tot.Apply();
         unit_viewer_running.AddUnit(ParseUntP2(pu, pu.result));
      }
   }

   Texture2D export_tex;
   public RenderTexture export_text_tot_r;
   public Texture2D export_tex_tot;

   List<(Texture2D tex, List<MetaRow> metadata)> export_tex_tot_res = new();

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


   public Mesh PrepMeshWithOriginalPos(Transform tr, Mesh mesh) {
      if (mesh) {
         if (!mesh.isReadable) {
            Debug.Log($"Found unreadable mesh: {mesh.name} in object {tr.name}!");
            return mesh;
         }

         if (mesh.name.Contains("(PREPPED ORIG POS)")) return mesh;
         if (mesh.uv5.IsNonEmpty()) {
            Debug.LogError($"Mesh {mesh.name} alreadt contains uv5 in object {tr.name}!!");
            return mesh;
         }

         if (mesh.uv6.IsNonEmpty()) {
            Debug.LogError($"Mesh {mesh.name} alreadt contains uv6 in object {tr.name}!!");
            return mesh;
         }

         var nm = Instantiate(mesh);
         nm.name = mesh.name + " (PREPPED ORIG POS)";
         var mat = tr.localToWorldMatrix;
         var bw = nm.vertices.map(x => (Vector3)(mat * new Vector4(x.x, x.y, x.z, 1)) * (1f / 1));
         nm.uv5 = bw.map(x => new Vector2(x.x, x.y) * (1f / 1));
         nm.uv6 = bw.map(x => new Vector2(x.z, 0) * (1f / 1));
         return nm;
      }

      return mesh;
   }

   public void StoreOOriginalPosForModel(Transform tr) {
      foreach (var sk in tr.GetComponentsInChildren<Renderer>()) {
         if (sk is SkinnedMeshRenderer am) {
            var m = PrepMeshWithOriginalPos(am.transform, am.sharedMesh);
            if (m) am.sharedMesh = m;
         }

         if (sk is MeshRenderer mr) {
            var mf = mr.GetComponent<MeshFilter>();
            var m = PrepMeshWithOriginalPos(mr.transform, mf.sharedMesh);
            if (m) mf.sharedMesh = m;
         }
      }
   }

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

            // sprite_capture_pipeline.model.animation_clip = u.animations.First().clip;

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

            if (!parsed_pipeline_data_orig.parts.TryGetValue(kv.Value, out var part_o)) {
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

         StoreOOriginalPosForModel(model_body.transform);
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

      // sprite_capture_pipeline.model.animation_clip = pu.animations[0].clip;
      if (load_animation_on_play) {
         StartCoroutine(RunAnimationCoroutine(pu));
      }
   }

   IEnumerator RunAnimationCoroutine(ParsedUnit pu) {
      float t = 0;
      var mb = sprite_capture_pipeline.GetMoveBackFunk();
      while (!sprite_capture_pipeline.exporting && pu.raw_name == load_unit_on_play.export_name) {
         UpdateCaptureScale();
         mb = sprite_capture_pipeline.GetMoveBackFunk();
         t += sprite_capture_pipeline.model.animation_speed * Time.deltaTime;
         t %= 1;

         AnimationClip clip;

         if (load_animation_on_play && load_animation_on_play.clip_ref) {
            clip = sprite_capture_pipeline.model.animation_clip = load_animation_on_play.clip_ref;
         } else {
            clip =
               pu.animations.First(x => x.category == load_animation_on_play.category)?.clip;
            if (clip) sprite_capture_pipeline.model.animation_clip = clip;
         }

         var animation_type_object = load_animation_on_play;
         SetModelRotation(pu, load_animation_on_play, default_shot_types[0]);


         if (animation_type_object != null) {
            bool old_neg = model.negate_root_motion;
            bool old_app = model.model_root.applyRootMotion;
            try {
               if (animation_type_object.negate_root_motion) {
                  model.model_root.applyRootMotion = true;
                  model.negate_root_motion = true;
               } else {
                  if (animation_type_object.looping_root) {
                     sprite_capture_pipeline.HandleLoopingRootMotion(clip, t);
                  }
               }

               sprite_capture_pipeline.model.SetAnimationNow_Float(clip, t);
            }
            finally {
               model.negate_root_motion = old_neg;
               model.model_root.applyRootMotion = old_app;
            }
         }

         sprite_capture_pipeline.model.enabled = false;
         sprite_capture_pipeline.RunPipeline();
         sprite_capture_pipeline.PushToResultTextures();
         sprite_capture_pipeline.enabled = false;
         mb?.Invoke();
         yield return null;
      }

      sprite_capture_pipeline.model.enabled = true;
      sprite_capture_pipeline.enabled = true;
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
      if (!loginfo) return;
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
      dummy_holder.gameObject.SetActive(false);
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


      parsed_pipeline_data_orig = new(sheets_pipeline_descriptor, this);
      if (ExportPipeline.editor_prefs.log_info)
         Debug.Log($"units: {parsed_pipeline_data_orig.units.Count}");
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

   ParsedPipelineData parsed_pipeline_data_orig;


   public Dictionary<string, BodyModelData> model_mapping_by_body_type;

   public Dictionary<string, BodyModelData> model_mappings;

   public RectTransform to_visit;

   int max_per_grid;
   int max_grid_width;

   void SetGridVars(int sz) {
      max_grid_width = ((16384) / sz);
      max_per_grid = max_grid_width * max_grid_width;
   }

   void SetOutGrid(int n_left) {
      export_tex_cursor = default;
      export_tex_height_cursor = 0;
      export_tex_sprites_w = Min(atlas_sprites_per_row, n_left, export_tex_sprites_w, max_grid_width);
      if (export_tex_sprites_w < 1) export_tex_sprites_w = 1;
      while (n_left > 2 * (export_tex_sprites_w + 5) * export_tex_sprites_w) {
         export_tex_sprites_w = export_tex_sprites_w * 2;
      }

      export_tex_sprites_w = Min(max_grid_width, export_tex_sprites_w);

      while (n_left > max_grid_width * export_tex_sprites_w) {
         export_tex_sprites_w += 1;
      }


      out_sprite_count = n_left;
      out_grid = new(export_tex_sprites_w,
         (out_sprite_count + export_tex_sprites_w - 1) / export_tex_sprites_w);

      out_grid.x = Min(out_grid.x, max_grid_width);
      out_grid.y = Min(out_grid.y, max_grid_width);
   }

   IEnumerator RunPipeline() {
      int export_size = this.effective_export_size;
      if (write_files) {
         Std.EnsureLocalDir(export_to_folder);
      }

      tot_out_sprites_count = parsed_pipeline_data_orig.output_n;

      SetGridVars(export_size);
      sprite_gen_meta = new();


      time_benchmark = new();
      batch_id = -1;
      foreach (var parsed_pipeline_data in parsed_pipeline_data_orig.SplitMe(max_per_grid)) {
         batch_id++;
         output_i = 0;
         sprite_gen_meta = new();

         SetOutGrid(parsed_pipeline_data.output_n);


         dummy_holder.gameObject.SetActive(false);
         export_tex = new Texture2D(export_size, export_size);

         UpdateProgress();

         if (loginfo)
            Debug.Log($"grid: {parsed_pipeline_data.output_n}, {out_grid}, sz: {out_grid * export_size}");


         export_text_tot_r = new RenderTexture(export_size * out_grid.x, out_grid.y * export_size, 32);
         export_text_tot_r.enableRandomWrite = true;
         export_text_tot_r.filterMode = FilterMode.Point;


         export_text_tot_r.Clear(default);

         time_benchmark.Lap("Setup");

         sprite_capture_pipeline.time_benchmark = time_benchmark;

         if (loginfo)
            Debug.Log($"px: {out_grid_pix}");
         yield return RunParsedPipeline(parsed_pipeline_data);

         CreateFinalTexture(parsed_pipeline_data);
      }

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

   void CreateFinalTexture(ParsedPipelineData parsed_pipeline_data) {
      Vector2Int res_size = new Vector2Int(out_grid_pix.x, export_tex_cursor.y + export_tex_height_cursor);

      int height_diff = out_grid_pix.y - res_size.y;

      for (int i = 0; i < sprite_gen_meta.Count; i++) {
         var mr = sprite_gen_meta[i];
         mr.rect.y -= height_diff;
         sprite_gen_meta[i] = mr;
      }

      {
         export_tex_tot = new Texture2D(res_size.x, res_size.y);
         RenderTexture.active = export_text_tot_r;
         export_tex_tot.ReadPixels(
            new Rect(0, export_text_tot_r.height - export_tex_tot.height, export_tex_tot.width, export_tex_tot.height),
            0, 0);
         RenderTexture.active = null;
         export_tex_tot.Apply();
         export_tex_tot_res.Add((export_tex_tot, sprite_gen_meta));
      }

      int sprites_in = tot_sprites.Count;

      foreach (var metas in sprite_gen_meta) {
         Vector2 pix = new(export_tex_tot.width, export_tex_tot.height);
         var spr = GeneratedSpritesContainer.MakeSprite(export_tex_tot, $"{metas.file_name}", metas.rect,
            metas.pivot, pixelsPerUnit: effective_export_size / 2);

         tot_sprites.Add(spr);
      }


      int tsr = parsed_pipeline_data.units.Sum(x => x.sprites_to_generate.Count);

      foreach (var pu in parsed_pipeline_data.units) {
         var tt = tot_sprites.SubArray(sprites_in, sprites_in + pu.sprites_to_generate.Count);
         sprites_in += pu.sprites_to_generate.Count;
         var sc = GeneratedSpritesContainer.GetUnitCats(tt, tt.map(x => new MetaRow() { file_name = x.name }));

         Debug.AssertFormat(sc.Count == 1, "Single unit should get since unit cats, but got {1}: {0} - {2}", sc.Count,
            pu.out_name, sc.Keys.join(", "));

         var vals = sc.Values.ToList();
         GeneratedSpritesContainer.UnitCats cat = vals[0];
         for (int i = 1; i < vals.Count; i++) {
            cat.sprites.AddRange(vals[i].sprites);
         }

         PushNewRes(pu, cat);
      }
   }

   string multi_atlas_name(int batch_id) => batch_id > 0 ? $"__{batch_id}" : "";
   string full_atlas_name(int id) => $"{export_sheet_name}{multi_atlas_name(id)}";


   bool DeleteIfExists(string to_folder, string name) {
      if (File.Exists($"{to_folder}/{name}")) {
         File.Delete($"{to_folder}/{name}");
         return true;
      }

      return false;
   }

   public void WriteMetaFile(string to_folder, List<MetaRow> sprite_gen_meta, int id) {
      var meta_rows = sprite_gen_meta.Select(x => new MetaRow(prepend_to_sprite_name + x.file_name, x.rect, x.pivot))
         .Select(SpriteGenMetaRow).ToArray();

      var mf = $"{to_folder}/{full_atlas_name(id)}.spritemeta";
      File.WriteAllText(mf, meta_rows.join("\n"));

      // File.Copy(mf, $"{export_to_folder}/test_atlas.spritemeta", overwrite: true);
   }

   public void WriteTexturefile(string to_folder, Texture2D tex, int id) {
      var rb = tex.EncodeToPNG();
      File.WriteAllBytes($"{to_folder}/{full_atlas_name(id)}.png", rb);
      var meta_rows = sprite_gen_meta.Select(SpriteGenMetaRow).ToArray();
   }

   public static List<UnitAnimationData> FillAnimationDatas(string prefix, ParsedUnit pu) {
      Debug.Assert(pu.generated_animation_datas.IsEmpty());
      var ad = GetAnimationDatas(prefix, pu);

      pu.generated_animation_datas = ad.ToList();

      return pu.generated_animation_datas;
   }

   public static IEnumerable<UnitAnimationData> GetAnimationDatas(string prefix, ParsedUnit pu) {
      var sdict = pu.sprites_to_generate.ToKeyDict(x => (a: x.an.category, yaw: x.shot_type.yaw_angle));

      foreach (var kv in sdict.sorted(x => x.ToString())) {
         var (cat, yaw) = kv.Key;
         var sprites = kv.Value.ToList();

         var o = pu.animation_categories[cat];

         var a = new UnitAnimationData();

         a.unit = prefix + pu.out_name;
         a.category = cat;
         a.yaw = yaw;
         a.effect_time_ms = o.effect_time_ms;
         a.loop_end_index = o.loop_end_index;
         a.loop_start_index = o.loop_start_index;
         var spr = o.capture_frame.IsEmpty()
            ? sprites.ToArray()
            : o.capture_frame.map(x => sprites.Find(s => s.frame == x));
         a.sprites = spr.map(x => prefix + x.name);
         a.time_ms = o.time_ms.IsEmpty()
            ? spr.map(x => (1000 / o.auto_frames_per_s).Round())
            : o.time_ms.Copy();

         Debug.Assert(a.time_ms.Length == a.sprites.Length, a.unit + " " + a.category);


         int time = a.effect_time_ms;

         for (int i = 0; i < a.time_ms.Length; i++) {
            time -= a.time_ms[i];
            if (time <= 0 || i == a.time_ms.Length - 1) {
               var sprite = spr[i];
               if (sprite.marker_locations.TryGetValue(o.effect_spawn_pos_marker, out var p)) {
                  a.effect_spawn_pos = p;
               } else {
                  Debug.LogError(
                     $"Misisng pos marker for {a.unit} animation {a.category}: {o.effect_spawn_pos_marker}");
               }

               break;
            }
         }


         yield return a;
      }
   }

   public void WriteFiles(string to_folder) {
      int i = 0;
      for (; i < export_tex_tot_res.Count; i++) {
         var (tex, meta) = export_tex_tot_res[i];
         WriteTexturefile(to_folder, tex, i);

         WriteMetaFile(to_folder, meta, i);
      }

      WriteAnimationDatas(to_folder);

      bool d;
      do {
         d = false;

         d = DeleteIfExists(to_folder, $"{full_atlas_name(i)}.png");

         d = DeleteIfExists(to_folder, $"{full_atlas_name(i)}.spritemeta") || d;
      } while (d);

      omExportDone?.Invoke();
   }

   void WriteAnimationDatas(string to_folder) {
      var anim = this.parsed_pipeline_data_orig.units.FlatMap(x => x.generated_animation_datas.map(x => {
         x = x.Copy();
         x.unit = prepend_to_sprite_name + x.unit;
         x.sprites = x.sprites.map(x => prepend_to_sprite_name + x);
         return x;
      }));

      var json = anim.join("\n", x => JsonUtility.ToJson(x));
      File.WriteAllText($"{to_folder}/{full_atlas_name(0)}.animationmeta", json);
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