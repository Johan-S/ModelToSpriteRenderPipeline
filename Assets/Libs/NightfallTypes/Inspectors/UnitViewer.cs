using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static EngineDataInit;
using static DataTypes;

public class UnitViewer : MonoBehaviour {
   public bool show_animations;


   static bool NightfallHeaderRow(string[] row) {
      var name = row[0];
      if (name == "Unit_Number") return true;
      return name.EndsWith("_ID");
   }

   [NonSerialized] GameObject prefab;

   public EventSystem event_system => EventSystem.current;


   GameObject last_select;

   public static int ParseStat(GameData.UnitType du, string val) {
      int ar = 0;
      foreach (var a in val.Split("+").Select(x => x.Trim())) {
         if (int.TryParse(val, out var res)) {
            ar += res;
            continue;
         }

         if (val.ToLower() == "str") {
            ar += du.Strength;
         }
      }

      return ar;
   }

   static string FormatRanged(WeaponMelee w, GameData.UnitType u) {
      var dmg = w.Damage + u.Strength / 3;

      var prec = w.Precision + u.Precision;
      var range = ParseStat(u, w.Range);

      string res = $"{prec} prec    {dmg}d    r{range}";

      return res;
   }

   public class UnitTypeDetails {
      public UnitTypeDetails(GameData.UnitType unit) {
         this.unit = unit;
         type = unit;
         corner_msg = $"<color=#4f4>{unit.stats.gold_cost}</color>\n{unit.stats.resource_cost}";
         sprite = unit.sprite;
         name = unit.name;
         animation_sprites = unit.animation_sprites;
      }

      public UnitTypeDetails() {
         
      }

      public string name;

      public GameData.UnitType unit;

      public GameData.UnitType type;
      public string corner_msg;
      public Sprite sprite;
      public Shared.UnitAnimationSprites animation_sprites;

      public AnnotatedUI.ColoredSprite sized_sprite => new AnnotatedUI.ColoredSpriteImpl {
         sprite = sprite,
         size = sprite.rect.size / sprite.pixelsPerUnit * 64,
      };

      public IEnumerable<KeyVal> base_stats {
         get {
            var stats = unit.stats;
            yield return new("Gold", stats.gold_cost);
            yield return new("Resources", stats.resource_cost);
            yield return new("Manpower", unit.Population_Cost);
            yield return new("Health", stats.hp);
            yield return new("Magic Affinity", stats.ma);
            yield return new("Speed", stats.move_speed);
            yield return new("Strength", unit.Strength);
            yield return new("Attack", unit.Attack);
         }
      }

      public IEnumerable<KeyVal> weapon_stats {
         get {
            if (unit.Weapon_Primary) {
               var w = unit.Weapon_Primary;
               if (w.IsRanged) {
                  yield return new(w.name.Replace("_", " "), FormatRanged(w, unit));
               } else {
                  yield return new(w.name.Replace("_", " "),
                     $"{w.Attack + unit.Attack} att    {w.Damage + unit.Strength}d");
               }
            }

            if (unit.Weapon_Secondary) {
               var w = unit.Weapon_Secondary;
               if (w.IsRanged) {
                  yield return new(w.name.Replace("_", " "), FormatRanged(w, unit));
               } else {
                  yield return new(w.name.Replace("_", " "),
                     $"{w.Attack + unit.Attack} att    {w.Damage + unit.Strength}d");
               }
            }

            if (unit.Innate_Primary) {
               var w = unit.Innate_Primary;
               if (w.IsRanged) {
                  yield return new(w.name.Replace("_", " "), FormatRanged(w, unit));
               } else {
                  yield return new(w.name.Replace("_", " "),
                     $"{w.Attack + unit.Attack} att    {w.Damage + unit.Strength}d");
               }
            }

            if (unit.Innate_Secondary) {
               var w = unit.Innate_Secondary;
               if (w.IsRanged) {
                  yield return new(w.name.Replace("_", " "), FormatRanged(w, unit));
               } else {
                  yield return new(w.name.Replace("_", " "),
                     $"{w.Attack + unit.Attack} att    {w.Damage + unit.Strength}d");
               }
            }
         }
      }

      public IEnumerable<KeyVal> armor_stats {
         get {
            if (unit.armor) {
               var enc = "";
               if (unit.armor.Encumberance > 0) enc = $"{unit.armor.Encumberance} enc    ";

               yield return new(unit.armor.name.Replace("_", " "), $"{enc}{unit.armor.Protection_Body} prot");
            }

            if (unit.shield) {
               var enc = "";
               if (unit.shield.Encumberance > 0) enc = $"{unit.shield.Encumberance} enc    ";
               yield return new(unit.shield.name.Replace("_", " "),
                  $"{enc}{unit.shield.Parry} parry    {unit.shield.Shield_Protection} prot");
            }

            if (unit.helmet) {
               var enc = "";
               if (unit.helmet.Encumberance > 0) enc = $"{unit.helmet.Encumberance} enc    ";
               yield return new(unit.helmet.name.Replace("_", " "), $"{enc}{unit.helmet.Protection_Head} prot");
            }
         }
      }
   }

   public static UnitTypeDetails FromUnitSprites(Shared.UnitSprites sprites) {
      return new() {
         name = sprites.name,
         corner_msg = sprites.name,
         sprite = sprites.sprite,
         animation_sprites = sprites.animation_sprites,
      };
   }

   public static UnitTypeDetails FromUnit(GameData.UnitType unit) {
      return new(unit);
   }

   bool init_done;

   public void AddUnit(UnitTypeDetails u) {
      if (units == null) {
         SetUnits(new[] { u });
         return;
      }
      var g = units.ConstructNewElement(u);
      units.elements.Add(g);
      AnnotatedUI.MarkDirty_UI(transform);
   }


   public void SetUnits(IEnumerable<UnitTypeDetails> units_to_view) {
      units = SelectUtils.MakeSelectClass(units_to_view.ToList(), can_click_selected: true, can_toggle_off: true,
         default_selected: -1);
      units.extra_callback = () => { };

      units.selected_comp = null;

      init_done = true;
      AnnotatedUI.Visit(transform, this);
   }

   public SelectClass<UnitTypeDetails> units;

   public bool include_no_sprites;

   // Start is called before the first frame update
   void Start() {
      if (!init_done) {
         var ul = gear_data.game_units.Where(x => include_no_sprites || x.sprite);
         SetUnits(ul.Select(FromUnit)
            .ToList());
      }
   }

   // Update is called once per frame
   void Update() {
      var a = event_system.currentSelectedGameObject;
      if (a && a != last_select) {
         var b = a.GetComponent<Button>();
         if (b) b.onClick.Invoke();
      }
   }
   
}