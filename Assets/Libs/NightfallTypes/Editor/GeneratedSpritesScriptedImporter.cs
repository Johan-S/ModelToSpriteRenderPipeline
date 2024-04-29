using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using Object = UnityEngine.Object;

[ScriptedImporter(1, "spritemeta")]

public class GeneratedSpritesScriptedImporter : ScriptedImporter {

   public Texture2D atlas;
 
   public override void OnImportAsset(AssetImportContext ctx) {

      if (!atlas) {
         var me = ctx.assetPath;
         var dir = me[0..(me.LastIndexOf('/')+1)];
         string nm = me[(me.LastIndexOf('/') + 1)..];

         nm  =nm[..nm.LastIndexOf('.')];
         var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Join(dir, $"{nm}.png"));

         if (!tex) {
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Join(dir, $"atlas.png"));
         }

         if (tex) {
            atlas = tex;
            Debug.Log($"Added atlas {atlas.name}");
            EditorUtility.SetDirty(this);
         } else {
         
            // AssetDatabase.LoadAssetAtPath<>()
         
            Debug.LogError($"Need atlas!");
            return;
         }
         
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
         
         
         ctx.AddObjectToAsset(s.name, s);
      }

      foreach (var g in gened_sprites) {
         ctx.AddObjectToAsset(g.name, g);
      }

      GeneratedSpritesContainer.ClearCache();

   }
}