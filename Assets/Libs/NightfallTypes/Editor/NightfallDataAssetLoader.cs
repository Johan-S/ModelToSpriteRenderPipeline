using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;
using System.Linq;

[ScriptedImporter(1, "nightfall")]

public class NightfallDataAssetLoader : ScriptedImporter {

   public ExportPipelineSheets sheets;

   bool NightfallHeaderRow(string[] row) {
      var name = row[0];
      if (name == "Unit_Number") return true;
      return name.EndsWith("_ID");
   }
 
   public override void OnImportAsset(AssetImportContext ctx) {

      var data = File.ReadAllText(ctx.assetPath);

      var rows = data.SplitLines();

      var flow = new LoadDataFlow();
      
      EngineDataInit.AddGenericSheetRows(flow, rows);

      foreach (var l in flow.lazy) {
         l(flow.gear_data);
      }
      
      
      var engine_stuff = ScriptableObject.CreateInstance<EngineDataHolder>();

      flow.gear_data.AddTo(engine_stuff.base_types);



      engine_stuff.unit_types = new();
      
      ctx.AddObjectToAsset("main obj", engine_stuff);
      ctx.SetMainObject(engine_stuff);

      
      var base_types = engine_stuff.base_type_ref;
      
      var ug = new GameTypeCollection(engine_stuff);
      
      foreach (var bval in base_types.units) {
         var val = ug.GetAsset(bval);
         ctx.AddObjectToAsset(val.name, val);
         
         
         val.sprite_gen_name = ExportPipeline.GetExportUnitName(val.name);
         
         // val.FillGeneratedSprites(val.sprite_gen_name);
         val.generated_from_sheets = true;
         engine_stuff.unit_types.Add(val);
      }
   }
}