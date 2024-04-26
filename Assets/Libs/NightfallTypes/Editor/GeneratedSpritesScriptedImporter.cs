using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Object = UnityEngine.Object;

[ScriptedImporter(1, "spritemeta")]

public class GeneratedSpritesScriptedImporter : ScriptedImporter {

   public Texture2D atlas;
   static (string file_name, RectInt rect) ParseMetaRow(string row) {
      row = row.Trim();

      var ab = row.Split("\t");
      var fn = ab[0];
      var ri = ab[1].Split(",").map(int.Parse);

      RectInt rect = new(ri[0], ri[1], ri[2], ri[3]);

      return (fn, rect);
   }
 
   public override void OnImportAsset(AssetImportContext ctx) {

      if (!atlas) {
         Debug.LogError($"Need atlas!");
         return;
      }


      var name = Path.GetFileName(ctx.assetPath).Split(".")[0];
      
      
      var asset_text = File.ReadAllText(ctx.assetPath);

      var container = GeneratedSpritesContainer.MakeFromData(atlas, asset_text);

      container.name = name;
      
      ctx.AddObjectToAsset("container asset", container);
      
      ctx.SetMainObject(container);

      var sprites = container.GetSprites();
      var gened_sprites = container.GetGennedSprites();

      int sprite_i = 1;

      foreach (var s in sprites) {
         
         
         ctx.AddObjectToAsset($"sprite_{sprite_i++}", s);
      }

      foreach (var g in gened_sprites) {
         ctx.AddObjectToAsset(g.name, g);
      }

   }
}