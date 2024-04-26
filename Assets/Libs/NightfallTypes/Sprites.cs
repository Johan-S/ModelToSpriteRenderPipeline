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


}