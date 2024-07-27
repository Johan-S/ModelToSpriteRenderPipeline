using System;
using System.Collections.Generic;
using System.Linq;
using Shared;
using UnityEditor;
using UnityEngine;
using static UnitAnimationState;

using AnimationBundle = Shared.AnimationBundle;

// Load
[DefaultExecutionOrder(-150)]
public class AnimationSubsystem : MonoBehaviour {
   public static string[] standard_animation_names =
      EnumUtil.Values<UnitAnimationState>().filtered(x => x.valid()).map(x => x.ToString());

   public static HashSet<string> standard_animation_names_set = standard_animation_names.ToHashSet();

   public static bool IsStandardAnimation(string animation_category) {
      return standard_animation_names_set.Contains(animation_category);
   }

   [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
   static void Onl() {
      var go = new GameObject("AnimationSubsystem", typeof(AnimationSubsystem));

      DontDestroyOnLoad(go);
   }

   ExportedAnimation[] exported_animations;


   public static void LogAnimationErrors(string ani_class, string name, AnimationTypeObject obj) {
      if (instance.checked_animations.Contains(name)) return;

      instance.checked_animations.Add(name);

      if (!standard_animation_names.Contains(name)) {
         var closest = StringSimilarity.GetScoresClose(name, standard_animation_names);


         var file_path = AnimatonStateTransitions.GetPathToHere();

         var descr = Std.GetLogLink(file_path, "Add animations here.");

         string asset_path = "";
#if UNITY_EDITOR

         asset_path = AssetDatabase.GetAssetPath(obj);
#endif

         Debug.Log(
            $"Missing engine support for animation category: '{name}' in type {ani_class}, possible alt: {closest.join(", ", x => $"'{x.val}'")}\n" +
            $"File in question: {Std.GetLogLink(asset_path, $"{obj.name}.asset")}\n" +
            $"{descr}" +
            $"\n(Note that you can still view the sprites)\n{closest.join(", ", x => x.score.ToString())} ");
      }
   }

   HashSet<string> checked_animations = new();

   public static Shared.AnimationTypeObject[] animations => instance._animations;
   public static Shared.AnimationParsed[] animations_parsed => instance._animations_parsed;

   public static AnimationSubsystem instance;
   public Shared.AnimationTypeObject[] _animations;

   public Shared.AnimationParsed[] _animations_parsed;

   Dictionary<string, List<UnitAnimationData>> _unit_animations;


   public Dictionary<string, List<UnitAnimationData>> unit_animations {
      get {
         CheckReload();
         return _unit_animations;
      }
   }


   GameStateTracker data_asset_tracker;

   void LoadAnims() {
      _unit_animations = DataAssetRefs.instance.get_animations_containers.FlatMap(x => x.animation_data)
         .ToKeyDict(x => x.unit);

      foreach (var (name, ans) in _unit_animations) {
         foreach (var a in ans) {
            Debug.Assert(a.time_ms.Length == a.sprites.Length, name + " " + a.category);
            if (a.effect_spawn_pos.x != 0) {
               // Debug.Log($"{a.unit} {a.category} {a.effect_spawn_pos}");
            }
         }
      }
   }

   void CheckReload() {
      data_asset_tracker ??= DataAssetRefs.instance.state.Track();
      if (data_asset_tracker.IsCurrent()) return;

      data_asset_tracker.Sync();
      LoadAnims();
   }

   void OnEnable() {
      instance = this;
      checked_animations.Clear();


      CheckReload();
      _animations = Resources.LoadAll<Shared.AnimationTypeObject>("DirectAnims")
         .Concat(Resources.LoadAll<Shared.AnimationTypeObject>("ExportedAnimations")).ToArray();


      _animations_parsed = _animations.map(x => {
         var res = new Shared.AnimationParsed();
         Std.CopyShallowDuckTyped(x, res);
         return res;
      });

      // Debug.Log($"animations: {animations.Length}\n{animations.sorted(x => x.name).join("\n", x => x.name)}");
   }

   public static Shared.UnitTypeVisuals MakeVisuals(GeneratedSpritesContainer.UnitCats sprite,
      IList<UnitAnimationData> animation_datas) {
      var ua = animation_datas.First();

      var ex_name = sprite.unit_name;
      var maybe_cat = sprite;

      var gv = new Shared.UnitTypeVisuals() { name = ex_name };
      gv.animation_class = sprite.unit_name;

      gv.sprite = maybe_cat.idle_sprite;
      gv.icon_sprite = maybe_cat.icon_sprite;
      gv.render_angle = maybe_cat.sprites[0].pitch;


      if (gv.animation_class != null) {
         var r = GetAnimationSprites(ex_name, animation_datas, sprite, false);
         gv.animation_sprites_dirs =
            r.sorted(x => AnimationPicker.NormYawSigned(x.yaw + 90));
      }

      return gv;
   }

   public static Shared.UnitTypeVisuals MakeVisuals(SUnitSpritesExport u_s) {
      var ex_name = Sprites.GetExportUnitName(u_s.SpriteRefName);
      var maybe_cat = GeneratedSpritesContainer.Get(ex_name);

      var gv = new Shared.UnitTypeVisuals() { name = u_s.Unit_Name };
      gv.animation_class = u_s.AnimationType;

      if (maybe_cat == null) {
         throw new Exception($"Found no sprites for '{u_s.SpriteRefName}', looking for base name '{ex_name}'.");
      }

      gv.sprite = maybe_cat.idle_sprite;
      gv.icon_sprite = maybe_cat.icon_sprite;
      gv.render_angle = maybe_cat.sprites[0].pitch;

      if (gv.animation_class != null) {
         gv.animation_sprites_dirs =
            GetAnimationSprites(u_s.SpriteRefName, AnimationSubsystem.animations_parsed, gv.animation_class, maybe_cat,
                  strip_missing_silently: true)
               .sorted(x => AnimationPicker.NormYawSigned(x.yaw + 90));
      }

      return gv;
   }


   public static Shared.UnitTypeVisuals MakeVisuals(Shared.UnitSpritesExport u_s) {
      return MakeVisuals(u_s.ToBasic());
   }


   public static IEnumerable<Shared.UnitAnimationSprites> GetAnimationSprites(string SpriteRefName,
      IList<Shared.AnimationParsed> parsed_animations,
      GeneratedSpritesContainer.UnitCats maybe_cat, bool strip_missing_silently) {
      throw new Exception();
   }

   public static IEnumerable<Shared.UnitAnimationSprites> GetAnimationSprites(string SpriteRefName,
      IList<UnitAnimationData> data,
      GeneratedSpritesContainer.UnitCats maybe_cat, bool strip_missing_silently = true) {
      void FillBundle(Shared.AnimationBundle bundle, UnitAnimationData da) {
         bundle.time_ms = da.time_ms.Copy();
         try {
            bundle.sprites =
               da.sprites.map(s => maybe_cat.sprites.Find(x => x.full_name == s, null)?.sprite);
         }
         catch (KeyNotFoundException e) {
            Debug.LogError($"Error when parsing animations for unit {SpriteRefName}: {e}");
         }

         bundle.animation_duration_ms = bundle.time_ms.Sum();
         Debug.Assert(da.time_ms.Length == da.sprites.Length);
         Debug.Assert(bundle.sprites.Length == bundle.time_ms.Length);

         bundle.effect_start = da.effect_spawn_pos * 0.5f;
         if (da.effect_spawn_pos.x != default) {
            // Debug.Log($"{SpriteRefName} {name} {da.effect_spawn_pos}");
         }

         bundle.loop_start_index = da.loop_start_index;
         bundle.loop_end_index = da.loop_end_index;
         bundle.effect_time_ms = da.effect_time_ms;
      }

      SpriteRefName = Sprites.GetExportUnitName(SpriteRefName);
      var spr = maybe_cat.sprites.ToKeyDictUnique(x => x.full_name);
      foreach (var k in data.GroupBy(x => x.yaw)) {
         var yaw = k.Key;
         UnitAnimationSprites r = new UnitAnimationSprites();
         r.yaw = yaw;
         var datas = k.ToKeyDictUnique(x => x.category);
         foreach (var (name, bundle) in r.GetAllAnimations()) {
            if (datas.TryGetValue(name, out var da)) {
               FillBundle(bundle, da);
            }
         }

         foreach (var d in datas) {
            if (!AnimationFuncs.engnine_anime_names.Contains(d.Key)) {
               var bundle = new Shared.AnimationBundle();
               r.extra_bundles ??= new();
               r.extra_bundles.Add((d.Key, bundle));
               FillBundle(bundle, d.Value);
            }
         }

         yield return r;
      }
   }

   public static IEnumerable<Shared.UnitAnimationSprites> GetAnimationSprites(string SpriteRefName,
      IList<Shared.AnimationParsed> parsed_animations, string animation_class,
      GeneratedSpritesContainer.UnitCats maybe_cat, bool strip_missing_silently) {
      SpriteRefName = Sprites.GetExportUnitName(SpriteRefName);
      if (instance.unit_animations.TryGetValue(SpriteRefName, out List<UnitAnimationData> anims)) {
         foreach (var r in GetAnimationSprites(SpriteRefName, anims, maybe_cat)) {
            yield return r;
         }

         yield break;
      } else {
         Debug.LogError($"No unit animation data found for {SpriteRefName}!");
         yield break;
      }

      var standard_names = new UnitAnimationSprites().GetAllAnimations().Map(x => x.name).ToHashSet();

      var acl = parsed_animations.Where(x => x.animation_type == animation_class).ToList();

      var ad = maybe_cat.sprites.GroupBy(x => (x.animation_category, x.yaw)).ToDictionary(x => x.Key, x => x.ToArray());

      if (ad.Count == 0) {
         Debug.LogError($"Missing sprites for: {SpriteRefName}");
         yield break;
      }

      var extra_names = ad.Keys.map(x => x.animation_category).filtered(x => !standard_names.Contains(x)).Unique();
      var yaws = ad.Keys.map(x => x.yaw).Unique();

      void PushAni(string name, float yaw, Shared.AnimationBundle animation_bundle) {
         var cl = acl.Find(x => x.category == name);
         var key = (name: name, yaw: yaw);
         if (cl == null) {
            ad.Remove(key);
            return;
         }

         if (!ad.TryGetValue(key, out var sprites)) {
            return;
         }

         ad.Remove(key);

         if (cl.time_ms.NotEmpty()) {
            animation_bundle.time_ms = cl.time_ms.Copy();
            try {
               animation_bundle.sprites =
                  cl.capture_frame.map(frame => maybe_cat.dict[(cl.category, frame, yaw: yaw)].sprite);
            }
            catch (KeyNotFoundException e) {
               Debug.LogError($"Error when parsing animations for unit {SpriteRefName}: {e}");
            }
         } else {
            animation_bundle.sprites = sprites.map(x => x.sprite);
            if (cl.auto_frames_per_s > 0) {
               animation_bundle.time_ms = sprites.map(x => Mathf.RoundToInt(1000 / cl.auto_frames_per_s));
            } else {
               Debug.Log(
                  $"Wrong capture frame: {SpriteRefName} - {name} : {sprites.Length} != {cl.capture_frame.Length}");

               animation_bundle.time_ms = sprites.map(x => Mathf.RoundToInt(1000 / 10));
            }
         }

         Debug.Assert(animation_bundle.sprites.Length == animation_bundle.time_ms.Length);

         animation_bundle.animation_duration_ms = animation_bundle.time_ms.Sum();
      }

      foreach (var yaw in yaws) {
         var animation_sprites = new Shared.UnitAnimationSprites();
         animation_sprites.yaw = yaw;
         if (acl.Count == 0) {
            Debug.LogError($"Missing animation class {animation_class} for {SpriteRefName}");
         } else {
            foreach (var (name, am) in animation_sprites.GetAllAnimations()) {
               PushAni(name, yaw, am);
            }

            foreach (var name in extra_names) {
               var am = new Shared.AnimationBundle();
               animation_sprites.extra_bundles ??= new();
               animation_sprites.extra_bundles.Add((name, am));
               PushAni(name, yaw, am);
            }
         }

         yield return animation_sprites;
      }

      if (!strip_missing_silently) {
         if (ad.Count > 0 || extra_names.Count > 0) {
            var mg = ad.map(x => x.Key.animation_category).Concat(extra_names).Distinct().Sorted();
            Debug.LogError(
               $"Failed to parse {mg.Count} animations for unit {maybe_cat.unit_name}: {mg.join(", ", x => $"'{x}'")}\n" +
               $"CAN STILL VIEW THEM BUT WONT WORK IN GAME");
         }
      }
   }
}