using System;
using System.CodeDom;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Shared;
using UnityEditor;
using Object = UnityEngine.Object;

[ScriptedImporter(1, "sheets", importQueueOffset: 100)]
public class SheetsToUnityAssetLoader : ScriptedImporter {
   
 
   public override void OnImportAsset(AssetImportContext ctx) {
      
      
      

      var data = File.ReadAllText(ctx.assetPath);

      var fn = Regex.Split(ctx.assetPath, @"[./]")[^2];


      TextAsset ta = new TextAsset(data);
      ta.name = fn;
      
      var rows = data.SplitLines(skip_empty:true);
 
      var fl = new DataParsing.LoadDataFlow();
      
      DataParsing.AddGenericSheetRows(fl, rows);

      fl.lazy.ForEach(x => x(fl.gear_data));

      var d = fl.gear_data;

      int spell_i = 0;
      var spo = d.magic_spells.map(sp => {
         var sa = new SpellTypeObject();
         Std.CopyShallowDuckTyped(sp, sa);
         sa.name = sp.name;
         return sa;
      });
      
      var weps = d.melee_weapons.map(sp => {
         var sa = new WeaponTypeObject();
         Std.CopyShallowDuckTyped(sp, sa);
         sa.name = sp.name;
         return sa;
      });
      var arm = d.armors.map(sp => {
         var sa = new ArmorTypeObject();
         Std.CopyShallowDuckTyped(sp, sa);
         sa.name = sp.name;
         return sa;
      });
      var sh = d.shields.map(sp => {
         var sa = new ShieldTypeObject();
         Std.CopyShallowDuckTyped(sp, sa);
         sa.name = sp.name;
         return sa;
      });
      var helms = d.helmets.map(sp => {
         var sa = new HelmetTypeObject();
         Std.CopyShallowDuckTyped(sp, sa);
         sa.name = sp.name;
         return sa;
      });
      
      
      var simple_spells = d.simple_spells.map(sp => {
         var sa = new SimpleSpellTypeObject();
         Std.CopyShallowDuckTyped(sp, sa);
         sa.name = sp.name;
         return sa;
      });
      
      var simple_buffs = d.simple_buffs.map(sp => {
         var sa = new SimpleBuffTypeObject();
         Std.CopyShallowDuckTyped(sp, sa);
         sa.name = sp.name;
         return sa;
      });
      
      var animations = d.parsed_animations.map(sp => {
         var sa = new AnimationTypeObject();
         Std.CopyShallowDuckTyped(sp, sa);
         sa.auto_frames_per_s = sa.auto_frames_per_s;
         sa.name = $"{sa.animation_type}__{sa.category}";
         return sa;
      });
      var fi = typeof(SimpleUnitTypeObject).GetFields().Where(x => x.FieldType == typeof(string) && !x.IsStatic).ToDictionary(x => x.Name, x => x);
      
      var units = d.units.map(sp => {
         var sa = new SimpleUnitTypeObject();
         var (htype, row, headers) = fl.row_back_map[sp];

         foreach (var (r, h) in row.Zip(headers)) {
            fi.Get(h)?.SetValue(sa, r);
         }
         Std.CopyShallowDuckTyped(sp, sa);
         sa.name = sp.name;
         return sa;
      });

      var simple_container = ScriptableObject.CreateInstance<SimpleUnityTypeContainer>();

      simple_container.name = ta.name;
      simple_container.armor = new(arm);
      simple_container.shield = new(sh);
      simple_container.weapon = new(weps);
      simple_container.helmet = new(helms);
      simple_container.animation = new(animations);
      simple_container.spell = new(spo);
      simple_container.simpleSpell = new(simple_spells);
      simple_container.simpleBuff = new(simple_buffs);
      simple_container.simpleUnit = new(units);
      
      ctx.AddObjectToAsset("simple_container", simple_container);
      
      ctx.AddObjectToAsset("text asset", ta);
      
      ctx.SetMainObject(ta);
      
      foreach (var (i, sp) in spo.enumerate()) {
         ctx.AddObjectToAsset($"spell_{i}", sp);
      }
      
      foreach (var (i, sp) in weps.enumerate()) {
         ctx.AddObjectToAsset($"weapon_{i}", sp);
      }
      
      foreach (var (i, sp) in arm.enumerate()) {
         ctx.AddObjectToAsset($"armor_{i}", sp);
      }
      
      foreach (var (i, sp) in sh.enumerate()) {
         ctx.AddObjectToAsset($"shield_{i}", sp);
      }
      
      foreach (var (i, sp) in helms.enumerate()) {
         ctx.AddObjectToAsset($"helmet_{i}", sp);
      }
      foreach (var (i, sp) in simple_spells.enumerate()) {
         ctx.AddObjectToAsset($"simple_spell_{i}", sp);
      }
      foreach (var (i, sp) in simple_buffs.enumerate()) {
         ctx.AddObjectToAsset($"simple_buff_{i}", sp);
      }
      foreach (var (i, sp) in animations.enumerate()) {
         ctx.AddObjectToAsset($"animation_{i}", sp);
      }
      foreach (var (i, sp) in units.enumerate()) {
         ctx.AddObjectToAsset($"unit_{i}", sp);
      }
   }
}