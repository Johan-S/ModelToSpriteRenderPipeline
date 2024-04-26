using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using static DataParsing;
using static GeneratedSpritesContainer;

using static StatsAnnotations;

public static class Sprites {
   public static Sprite GetIconSprite(string name) => GetIcon(name);
   public static Sprite GetIcon(string name) {

      var sprite_gen_name = GetExportUnitName(name);

      return GetStableIcon(sprite_gen_name);
   }

   public static List<SpriteCats> GetUnitSprites(string name) {

      var c = GetUnitCats(name);

      if (!c) {
         Debug.Log($"Missing sprites for unit: {name} - {GetExportUnitName(name)}");
         return new();
      }

      return c.sprites;
   }

   public static UnitCats GetUnitCats(string name) {
      
      
      var sprite_gen_name = GetExportUnitName(name);
      var sprite_cats = GetInstance().lookup.Get(sprite_gen_name);

      return sprite_cats;
   }


   public static GameData.UnitAnimationSprites GetAnimationSprites(string unit, string animation_class) {
      
      
      
      var acl = EngineDataInit.base_types.animation_data.Where(x => x.animation_type == animation_class).ToList();

      var maybe_cat = GetUnitCats(unit);

      var animation_sprites = new GameData.UnitAnimationSprites();
      foreach (var (name, am) in animation_sprites.GetAllAnimations()) {
         var cl = acl.Find(x => x.category == name);
         if (cl == null) continue;
         var sprites = maybe_cat.sprites.Where(x => x.animation_category == name).ToArray();


         if (sprites.Length != cl.capture_frame.Length) throw new Exception();

         Debug.Assert(sprites.Length == cl.capture_frame.Length);


         am.sprites = sprites.map(x => x.sprite);
         am.time_ms = cl.time_ms.Copy();
                  

         am.animation_duration_ms = am.time_ms.Sum();
      }

      return animation_sprites;
   }

}