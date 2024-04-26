using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;

[ScriptedImporter(1, "dynamic")]
public class DynamicAssetMaker : ScriptedImporter {

   public Texture2D copy_settings;

   public int sprite_size = 32;
   public int pixelsPerUnit = 200;

   public int padding = 1;

   public SpriteGen.BorderData[] border_d;

   

   public override void OnImportAsset(AssetImportContext ctx) {

      border_d ??= new SpriteGen.BorderData[0];
      var gd = new SpriteGen.GUITextureData();

      
      Std.CopyShallowDuckTyped(this, gd);

      var (glob_tex, sprites) = gd.MakeGUITexture();
      
      var data = File.ReadAllText(ctx.assetPath);
      var rows = data.SplitLines();



      var format = copy_settings.format;
      var gformat = copy_settings.graphicsFormat;

      var fl = copy_settings.hideFlags;
      
      string main_name =  Path.GetFileName(ctx.assetPath);

      glob_tex.name = main_name;

      // var t = MakeT(Color.blue, Color.green);

      ctx.AddObjectToAsset("main obj", glob_tex);
      ctx.SetMainObject(glob_tex);

      int name_id = 1;
      

      foreach (var s in sprites) {
         
         ctx.AddObjectToAsset($"border_sprite_{name_id++}", s);
      }
      
      
   }
}
