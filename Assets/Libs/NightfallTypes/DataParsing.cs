using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

public static class DataParsing {
   public static Dictionary<string, string> ANIMATION_SUBSTITUTE = new() {
      { "Poleaxe", "Spearman" },
   };

   public static string NormalizeAnimationName(string name) => name.Replace('&', '_');

   public static GameData.UnitAnimationSprites GetAnimationSprites(string SpriteRefName,
      List<GameTypeCollection.AnimationParsed> parsed_animations, string animation_class,
      GeneratedSpritesContainer.UnitCats maybe_cat) {
      animation_class = ANIMATION_SUBSTITUTE.Get(animation_class, animation_class);
      var acl = parsed_animations.Where(x => x.animation_type == animation_class).ToList();

      var ad = maybe_cat.sprites.GroupBy(x => x.animation_category).ToDictionary(x => x.Key, x => x.ToArray());

      var animation_sprites = new GameData.UnitAnimationSprites();
      if (acl.Count == 0) {
         Debug.LogError($"Missing animation class {animation_class} for {SpriteRefName}");
      } else {
         foreach (var (name, am) in animation_sprites.GetAllAnimations()) {
            var cl = acl.Find(x => x.category == name);
            if (cl == null) {
               ad.Remove(name);
               continue;
            }

            if (!ad.TryGetValue(name, out var sprites)) {
               continue;
            }
            ad.Remove(name);


            am.sprites = sprites.map(x => x.sprite);
            am.time_ms = cl.time_ms.Copy();
            if (sprites.Length == 1) {
               am.time_ms = new[] { am.time_ms.Sum() };
            } else {
               Debug.Assert(sprites.Length == cl.capture_frame.Length);
            }


            if (sprites.Length != am.time_ms.Length) {
               if (cl.auto_frames_per_s > 0) {
                  am.time_ms = sprites.map(x => 1000 / cl.auto_frames_per_s);
               } else {
                  Debug.LogError(
                     $"Wrong capture frame: {SpriteRefName} - {name} : {sprites.Length} != {cl.capture_frame.Length}");
               }
            }

            Debug.Assert(sprites.Length == am.time_ms.Length);

            am.animation_duration_ms = am.time_ms.Sum();
         }
      }

      if (ad.Count > 0) {
         Debug.LogError(
            $"Failed to parse {ad.Count} animations for unit {maybe_cat.unit_name}: {ad.join(", ", x => $"'{x.Key}'")}");
      }

      return animation_sprites;
   }

   public static string GetExportUnitName(string orig_name) {
      return orig_name.Replace(" (", "_").Replace("(", "_").Replace(")", "").Replace(" ", "_").Trim();
   }

   public class LoadDataFlow {
      public int parse_count;
      public int error_count;

      public GameTypeCollection gear_data = new();
      public List<Action<GameTypeCollection>> lazy = new();

      public Dictionary<object, (string htype, string[] row, string[] hdrs)> row_back_map = new();
   }

   public static void AddGenericSheetRows(LoadDataFlow flow, IEnumerable<string> rows) {
      var rowd = ParseGenericSheetRows(rows).ToList();
      LoadRowd(flow, rowd);
   }


   public static LastParsedAsset last_parsed_asset;

   public static string GetAssetName(string htype, string[] row) {
      if (htype == "Spell_ID") {
         return row.Get(3);
      }

      if (Regex.IsMatch(htype, @"Animation_Type|SimpleBuff|SimpleSpell", RegexOptions.Compiled)) {
         return row.Get(0);
      }

      return row.Get(1);
   }

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

   public static string[] GetRows(string text_file) {
      return Resources.Load<TextAsset>(text_file).text.SplitLines();
   }

   public static string GetText(string text_file) {
      return Resources.Load<TextAsset>(text_file).text;
   }

   public static string NormalizeHeader(string hdr) {
      return hdr.Replace(' ', '_');
   }


   public static IEnumerable<(string htype, string[] row, string[] hdrs)> ParseGenericSheetRows(
      IEnumerable<string> rows) {
      string[] header = null;
      string htype = "";
      foreach (var row_string in rows) {
         var row_tr = row_string.Trim();
         if (row_tr.Trim().Length == 0 || row_tr.FastStartsWith("//")) continue;
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

   public static bool NightfallHeaderRow(string[] row) {
      var name = row[0];
      if (name == "Unit_Number") return true;
      if (name == "Animation Type") return true;
      return name.EndsWith("_ID");
   }

   public static T CopyDucked<T>(object fr) where T : new() {
      T res = new();
      Std.CopyShallowDuckTyped(fr, res);
      return res;
   }

   public static bool AddDucked<T>(object fr, LookupList<T> o) where T : class, Named, new() {
      T res = new();
      Std.CopyShallowDuckTyped(fr, res);
      return o.AddOrIgnore(res);
   }

   public static void AddDucked<T>(object fr, List<T> o) where T : new() {
      T res = new();
      Std.CopyShallowDuckTyped(fr, res);
      o.Add(res);
   }

   static void LoadRowd(LoadDataFlow flow, List<(string htype, string[] row,
      string[] hdrs)> rowd) {
      foreach (var (htype, row, hdrs) in rowd) {
         try {
            last_parsed_asset = new LastParsedAsset(htype, row, hdrs);
            LoadSingle(flow, htype, row, hdrs);
            flow.parse_count++;
         }
         catch (Exception e) {
            flow.error_count++;
            Debug.LogError($"Error While pasing {last_parsed_asset?.name ?? "UNKNOWN"}: {e}");
         }

         last_parsed_asset = null;
      }
   }

   static void LoadSingle(LoadDataFlow flow, string htype, string[] row,
      string[] hdrs) {
      if (row[0].FastStartsWith("//")) {
         Debug.LogError($"Passing comment row!");
         return;
      }

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

         Dictionary<(string, string), GameTypeCollection.AnimationParsed> res = new();

         List<GameTypeCollection.AnimationParsed> arr = new();

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
            var msg = err_arr.join(", ",
               x =>
                  $"{x.category}: Capture Frames ({x.capture_frame?.Length ?? 0}) != Time MS ({x.time_ms?.Length ?? 0}) ");
            Debug.LogError($"Error while parsing animation {type}: {msg}");
         }

         gear_data.parsed_animations.AddRange(arr);
      }
   }

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

   static string[] _last_hdrs =
      null;

   static int _last_hdrs_diff = 0;
}