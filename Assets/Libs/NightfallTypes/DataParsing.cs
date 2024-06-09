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

   public static Shared.UnitAnimationSprites GetIdleSpriteOnlyBundle(Sprite spr) {

      return new Shared.UnitAnimationSprites() {
         idle_animation = new () {
            animation_duration_ms = 100,
            
            sprites = new []{spr},
            time_ms = new[]{100},
         },
      };
   }

   public static Shared.UnitAnimationSprites GetAnimationSprites(string SpriteRefName,
      List<Shared.AnimationParsed> parsed_animations, string animation_class,
      GeneratedSpritesContainer.UnitCats maybe_cat) {
      var acl = parsed_animations.Where(x => x.animation_type == animation_class).ToList();

      var ad = maybe_cat.sprites.GroupBy(x => x.animation_category).ToDictionary(x => x.Key, x => x.ToArray());

      var animation_sprites = new Shared.UnitAnimationSprites();
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


            if (sprites.Length != am.time_ms.Length) {
               if (cl.auto_frames_per_s > 0) {
                  am.time_ms = sprites.map(x => Mathf.RoundToInt(1000 / cl.auto_frames_per_s));
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
   public static string NormalizeHeader(string hdr) {
      return hdr.Replace(' ', '_');
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
}