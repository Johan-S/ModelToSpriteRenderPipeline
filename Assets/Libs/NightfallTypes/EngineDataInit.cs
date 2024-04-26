using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;


[DefaultExecutionOrder(-600)]
public class EngineDataInit : MonoBehaviour, IEngineDataPart {


   public bool load_from_simple_objects;
   
   public EngineDataHolder engine_data_init;

   public ExportPipelineSheets engine_sheets;
   static bool init = false;


   Action on_destroy;

   static ExportPipelineSheets sheets_override;

   public static void SetEngineSheets(ExportPipelineSheets engine_sheets) {
      sheets_override = engine_sheets;
      if (instance) {
         instance.engine_data_init = MakeEngineData(engine_sheets, new IEngineDataPart[0], null);
         instance.InitGameEngineData();
      }
   }


   public UnitTypeObject first_unit;

   void InitGameEngineData() {
      engine_data_init.InitDataForPlay();

      first_unit = engine_data.unit_types.Get(0, null);
   }

   static EngineDataInit Init() {
      if (init) return instance;

      var prefab = Resources.Load<EngineDataInit>("MainSheetsLoader");

      Instantiate(prefab);
      
      // new GameObject("EngineDataInit").AddComponent<EngineDataInit>();
      return instance;
   }


   public static GameTypeCollection gear_data => engine_data.gear_data;
   public static EngineDataHolder.BaseTypes base_types => engine_data.base_type_ref;

   public static EngineDataHolder engine_data => Init().engine_data_init;

   static EngineDataInit instance;

   public bool load_subs;

   public EngineDataHolder custom_engine_data_objects;

   void Awake() {
      if (instance) {
         if (!init) Debug.LogError($"Has {instance} but no init {init}!");
         Destroy(this);
         return;
      }

      init = true;
      on_destroy = () => init = false;

      instance = this;
      DontDestroyOnLoad(gameObject);

      if (load_subs) {
         var data_refs = GetComponentsInChildren<EngineDataSheetRef>().ToArray();
         engine_data_init = MakeEngineData(null, data_refs, custom_engine_data_objects);
      } else if(sheets_override || engine_sheets) {
         var sh = sheets_override ? sheets_override : engine_sheets;
         var data_refs = GetComponentsInChildren<EngineDataSheetRef>().ToArray();
         engine_data_init = MakeEngineData(sh, data_refs, custom_engine_data_objects);
      } else {
         if (engine_data_init == null) {
            engine_data_init = Resources.Load<EngineDataHolder>("GameEngineData");
         }
      }

      InitGameEngineData();
   }

   void OnDestroy() {
      on_destroy?.Invoke();
   }

   static string[] _last_hdrs =
      null;

   static int _last_hdrs_diff = 0;

   static string[] AdjustRowValues(string[] target_hdrs, string[] hdrs, string[] row) {
      if (hdrs == null) return row;
      if (target_hdrs.Length == row.Length) return row;
      if (target_hdrs.Length >= hdrs.Length) return row;

      if (_last_hdrs != hdrs) {
         _last_hdrs = hdrs;
         _last_hdrs_diff = 0;
         int hdr_found = hdrs.Count(target_hdrs.Contains);
         // Debug.Log($"hdr: {hdr_found} / {hdrs.Length}");

         var diffs = hdrs
            .Select((s, i) => i - target_hdrs.IndexOf(StringSimilarity.GetClosestScore(s, target_hdrs).best)).ToArray();

         int median_diff = diffs.Sorted()[diffs.Length / 2];
         _last_hdrs_diff = median_diff;
         Debug.Log(
            $"WARN: hdr lens: {target_hdrs.Length} vs{hdrs.Length}, {row.Length}, Trying to adjust for Median hdr diff: {median_diff}");

         // var hd_cl = hdrs.map(s => (s, sim: StringSimilarity.GetClosestScore(s, target_hdrs)));
         // Debug.Log($"hdr sc:\n{hd_cl.filter(x => x.sim.score > 0.1f).join("\n", (a) => $"{a.sim.score:0.0} - {a.s} vs {a.sim.best}")}");
      }

      if (row.Length > target_hdrs.Length && _last_hdrs_diff >= 0) {
         return row[_last_hdrs_diff..(target_hdrs.Length + _last_hdrs_diff)];
      }


      return row;
   }

   public static bool NightfallHeaderRow(string[] row) {
      var name = row[0];
      if (name == "Unit_Number") return true;
      if (name == "Animation Type") return true;
      return name.EndsWith("_ID");
   }
   
   static T CopyDucked<T>(object fr) where T : new() {
      T res = new();
      Std.CopyShallowDuckTyped(fr, res);
      return res;
   }
   static bool AddDucked<T>(object fr, LookupList<T> o) where T : class, Named, new() {
      T res = new();
      Std.CopyShallowDuckTyped(fr, res);
      return o.AddOrIgnore(res);
   }
   static void AddDucked<T>(object fr, List<T> o) where T : new() {
      T res = new();
      Std.CopyShallowDuckTyped(fr, res);
      o.Add(res);
   }

   static EngineDataHolder MakeEngineData(ExportPipelineSheets sheets, IEngineDataPart[] iengine_datas, EngineDataHolder custom_engine_data_objects) {


      var flow = new LoadDataFlow();


      {
         if (sheets) AddTypesTo(sheets, flow);
         foreach (var ed in iengine_datas) {
            ed.AddTypesTo(flow);
         }
      }
      foreach (var la in flow.lazy) {
         la(flow.gear_data);
      }


      return MakeEngineDataFromGears(flow.gear_data, custom_engine_data_objects);
   }

   static EngineDataHolder MakeEngineDataFromGears(GameTypeCollection fgear_data, EngineDataHolder custom_engine_data_objects) {
      var engine_stuff = new EngineDataHolder();
      engine_stuff.unit_types = new();
      engine_stuff.base_types = new();

      fgear_data.AddTo(engine_stuff.base_types);

      var ug = new GameTypeCollection(engine_stuff);

      if (custom_engine_data_objects) {
       
         foreach (var cu in custom_engine_data_objects.unit_types) {
            engine_stuff.unit_types.Add(cu);
         }  
      }
      foreach (var utc in engine_stuff.base_types.units) {
         var val = ug.GetAsset(utc);

         val.sprite_gen_name = ExportPipeline.GetExportUnitName(val.name);
         val.generated_from_sheets = true;

         // val.FillGeneratedSprites(val.sprite_gen_name);

         engine_stuff.unit_types.Add(val);
      }

      return engine_stuff;
   }
   public static IEnumerable<(string htype, string[] row, string[] hdrs)> ParseGenericSheetRows(IEnumerable<string> rows) {
      string[] header = null;
      string htype = "";
      foreach (var row_string in rows) {
         if (row_string.Trim().Length == 0) continue;
         if (row_string.FastStartsWith("//")) {
            continue;
         }

         if (row_string.FastStartsWith("#")) {
            if (row_string.StartsWith("#hdr\t", out var hdr_row)) {
               header = hdr_row.Trim().Split("\t").Trim();
               continue;
            }

            htype = row_string.Substring(1).Trim();
            header = null;
            continue;
         }

         var row = row_string.Split("\t").Trim();


         if (NightfallHeaderRow(row)) {
            var old_htype = htype;
            htype = row[0].Replace("_Number", "_ID").Replace(" ", "_");
            // header = row;
            var new_header = row_string.Trim().Split("\t").Trim();
            if (htype == old_htype && htype == "Animation_Type") {
               header = header.Zip(new_header, (s, t) => $"{s}\t{t}").ToArray();

            } else {
               header = new_header;
            }
         } else {
            yield return ((htype, row, header));
         }
      }
   }

   public static void  AddGenericSheetRows(LoadDataFlow flow, IEnumerable<string> rows) {

      var rowd = ParseGenericSheetRows(rows).ToList();
      LoadRowd(flow, rowd);
   }

   public void AddTypesTo(LoadDataFlow flow) {
      AddTypesTo(this.engine_sheets, flow);
   }

   public static void AddTypesTo(ExportPipelineSheets sheets, LoadDataFlow flow) {
      sheets.InitData();

      {
         List<(string htype, string[] row, string[] hdrs)> rowd = new();

         void AddRowsL(string htype, IEnumerable<string> rows) {
            foreach (var row_string in rows) {
               if (row_string.Trim().Length == 0) continue;
               if (row_string.StartsWith("#")) {
                  continue;
               }

               var row = row_string.Split("\t").Trim();

               if (NightfallHeaderRow(row)) {
               } else {
                  rowd.Add((htype, row, null));
               }
            }
         }

         if (sheets.multi_sheet_do_not_set.IsNonEmpty()) {
            foreach (var sh in sheets.multi_sheet_do_not_set) {
               if (sh) AddGenericSheetRows(flow, sh.GetLines());
            }
         } else {

            if (sheets.animations) {
               AddGenericSheetRows(flow, sheets.animations.GetLines());
            }
            AddRowsL("Armor_ID", sheets.armor.GetLines());
            AddRowsL("Helmet_ID", sheets.helmet.GetLines());
            AddRowsL("Shield_ID", sheets.shield.GetLines());
            AddRowsL("Weapon_ID", sheets.weapon.GetLines());
            AddRowsL("Unit_ID", sheets.unit.GetLines());
         }

         {
            LoadRowd(flow, rowd);
         }
      }
   }

   public static GameTypeCollection LoadStuff(EngineDataHolder engine_stuff, List<(string htype, string[] row)> rowd) {
      var base_types = engine_stuff.base_type_ref;


      var flow = new LoadDataFlow();



      LoadRowd(flow, rowd.Map(x => (x.htype, x.row, (string[])null)));

      foreach (var la in flow.lazy) {
         la(flow.gear_data);
      }

      flow.gear_data.AddTo(base_types);


      var ug = new GameTypeCollection(engine_stuff);


      return ug;
   }

   public static string GetAssetName(string htype, string[] row) {
      if (htype == "Spell_ID") {

         return row.Get(3);
      }

      if (Regex.IsMatch(htype, @"Animation_Type|SimpleBuff|SimpleSpel", RegexOptions.Compiled)) {
         return row.Get(0);
      }

      return row.Get(1);
   }

   public static LastParsedAsset last_parsed_asset;

   public class LastParsedAsset {
      public string htype;
      public string[] row;
      public string[] hdrs;
      
      public string name;

      public string GetDebugPrefix() {
         return $"Error when parsing {name}: ";
      }

      public LastParsedAsset(string htype, string[] row, string[] hdrs) {
         this.htype = htype;
         this.row = row;
         this.hdrs = hdrs;
         name = GetAssetName(htype, row) ?? "<ROW_NAME_NOT_FOUND>";
      }
   }

   static void LoadRowd(LoadDataFlow flow, List<(string htype, string[] row,
      string[] hdrs)>  rowd) {

      foreach (var (htype, row, hdrs) in rowd) {
         try {
            last_parsed_asset = new LastParsedAsset(htype, row, hdrs);
            LoadSingle(flow, htype, row, hdrs);
            flow.parse_count++;
         } catch (Exception e) {
            flow.error_count++;
            Debug.LogError($"Error While pasing {last_parsed_asset?.name ?? "UNKNOWN"}: {e}");
         }
         last_parsed_asset = null;

      }
   }

   static void LoadSingle(LoadDataFlow flow, string htype, string[] row,
      string[] hdrs) {
      
      var gear_data = flow.gear_data;
      var lazy = flow.lazy;
      if (htype == "Unit_ID") {
         var val = GameTypeCollection.ParseRows<DataTypes.UnitTypeClass>(row);
         if (val.name.IsNullOrEmpty()) return;

         if (!gear_data.units.AddOrIgnore(val)) {
            Debug.LogError($"Found duplicate Unit: {val.GetType().Name}: {val.name}");
            return;
         }

         flow.row_back_map[val] = (htype, row, hdrs);


         lazy.Add(g => { GameTypeCollection.ParseRows_Finalize(g, val, row); });
      }

      if (htype == "Weapon_ID") {
         var val = GameTypeCollection.ParseRows<DataTypes.WeaponMelee>(row);
         if (val.name.IsNullOrEmpty()) return;
         if (!gear_data.melee_weapons.AddOrIgnore(val)) {
            Debug.LogError($"Found duplicate Weapon: {val.GetType().Name}: {val.name}");
            return;
         }
         flow.row_back_map[val] = (htype, row, hdrs);
      }

      if (htype == "Armor_ID") {
         var val = GameTypeCollection.ParseRows<DataTypes.Armor>(row);
         if (val.name.IsNullOrEmpty()) return;
         if (!gear_data.armors.AddOrIgnore(val)) {
            Debug.LogError($"Found duplicate Armor: {val.GetType().Name}: {val.name}");
            return;
         }
         flow.row_back_map[val] = (htype, row, hdrs);
      }

      if (htype == "Shield_ID") {
         var val = GameTypeCollection.ParseRows<DataTypes.Shield>(row);
         if (val.name.IsNullOrEmpty()) return;
         if (!gear_data.shields.AddOrIgnore(val)) {
            Debug.LogError($"Found duplicate Shield: {val.GetType().Name}: {val.name}");
            return;
         }
         flow.row_back_map[val] = (htype, row, hdrs);
      }

      if (htype == "Helmet_ID") {
         var val = GameTypeCollection.ParseRows<DataTypes.Helmet>(row);
         if (val.name.IsNullOrEmpty()) return;
         if (!gear_data.helmets.AddOrIgnore(val)) {
            Debug.LogError($"Found duplicate Helmet: {val.GetType().Name}: {val.name}");
            return;
         }
         flow.row_back_map[val] = (htype, row, hdrs);
      }

      if (htype == "Spell_ID") {
         var fn = DataTypes.MagicSpell.field_names;
         var used_row = row;
         used_row = AdjustRowValues(fn, hdrs, row);
         var val = GameTypeCollection.ParseRows<DataTypes.MagicSpell>(used_row);
         if (val.name.IsNullOrEmpty()) return;
         if (!gear_data.magic_spells.AddOrIgnore(val)) {
            Debug.LogError($"Found duplicate Spell: {val.GetType().Name}: {val.name}");
            return;
         }
         flow.row_back_map[val] = (htype, row, hdrs);
      }

      if (htype == "SimpleSpell") {
         var val = GameTypeCollection.ParseRows<DataTypes.SimpleSpell>(row);
         if (val.name.IsNullOrEmpty()) return;
         if (!gear_data.simple_spells.AddOrIgnore(val)) {
            Debug.LogError($"Duplicate simple spell: {val.name}");
            return;
         }
         flow.row_back_map[val] = (htype, row, hdrs);
      }

      if (htype == "SimpleBuff") {
         var kvs = row[^1].SplitPairs(",", "=");

         var val = GameTypeCollection.ParseRows<BattleData.UnitBuff>(row[..^1]);
         if (val.name.IsNullOrEmpty()) return;
         GameTypeCollection.SetFields(val, kvs);
         if (!gear_data.simple_buffs.AddOrIgnore(val)) {
            Debug.LogError($"Found duplicate SimpleBuff: {val.GetType().Name}: {val.name}");
            return;
         }
         flow.row_back_map[val] = (htype, row, hdrs);
      }
      
      if (htype == "Animation_Type") {
         
         Debug.Assert(hdrs.Length != 0);

         var cats = hdrs.map(x => x.Split("\t")[0]);
         var fields = hdrs.map(x => x.Split("\t")[1]);
         
         string type = row[0];

         Dictionary<(string, string), ExportPipelineSheets.AnimationParsed> res = new();

         List<ExportPipelineSheets.AnimationParsed> arr = new();
         
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

         var err_arr = arr.Filtered(x => (x.capture_frame?.Length ?? 0) != (x.time_ms?.Length ?? 0));

         if (err_arr.Count > 0) {
            var msg = err_arr.join(", ", x => $"{x.category}: Capture Frames ({x.capture_frame?.Length ?? 0}) != Time MS ({x.time_ms?.Length ?? 0}) ");
            Debug.LogError($"Error while parsing animation {type}: {msg}");
         }
         
         gear_data.parsed_animations.AddRange(arr);
      }
   }
}