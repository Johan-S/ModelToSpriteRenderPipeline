using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using static UnitTypeObject;

using static EngineDataInit;


public static partial class BattleData {


   public class BattleUnit {
      public BattleUnit() {
      }

      public int id;

      public Vector2Int Rotate1(Vector2Int a) {
         if (prefer_right) {
            return a.RotateRight();
         }

         return a.RotateLeft();
      }

      public Vector2Int Rotate2(Vector2Int a) {
         if (prefer_right) {
            return a.RotateRight().RotateRight();
         }

         return a.RotateLeft().RotateLeft();
      }

      public Vector2Int Rotate3(Vector2Int a) {
         if (prefer_right) {
            return a.RotateLeft();
         }

         return a.RotateRight();
      }

      public Vector2Int Rotate4(Vector2Int a) {
         if (prefer_right) {
            return a.RotateLeft().RotateLeft();
         }

         return a.RotateRight().RotateRight();
      }

      public UnitBuffEnum special_buffs;
      public int prot_bonus = 0;
      public int speed_bonus = 0;
      public int def_bonus = 0;
      public int strength_bonus = 0;

      public int size_bonus = 0;

      public void AddBuff(UnitBuff buff) {
         prot_bonus += buff.prot_bonus;
         speed_bonus += buff.speed_bonus;
         def_bonus += buff.def_bonus;
         strength_bonus += buff.strength_bonus;
         size_bonus += buff.size_bonus;
         special_buffs |= buff.buff_enum;
         buffs.Add(buff);
      }

      public List<UnitBuff> buffs = new();

      public ScriptedAction[] scripted_actions;

      public int script_id;

      public int script_counter;

      public bool ScriptDone() => scripted_actions == null || script_id >= scripted_actions.Length;

      public int initiative_roll;

      public bool walk_around;

      public bool prefer_right;

      public int fatigue;

      public int harrassment;

      public GameData.UnitStats base_stats;

      public GameData.UnitType type_object;

      public UnitMovementOrder order;

      public UnitTargetingOrder targeting_order;

      public Vector2Int pos;

      public int ap;

      public float knockback_speed;
      public bool knocked_back;
      public int knockback_ticks;
      public int sleep_ticks;

      public int move_speed_adusted => move_speed + speed_bonus;
      public int move_speed;
      public int attack_speed;

      public int type_id;

      public int hp = 10;

      public bool dead => hp <= 0;

      public BattleUnit target_unit;

      public int armor;

      public int side;

      public DelayedAction delayed_action;

      public int size => base_stats.size;

      public int projectile_speed = 30;
      public int melee_damage;
      public int melee_damage_adj => melee_damage + strength_bonus;

      public Shared.DamageFlags melee_flags;

      public int ranged_damage;
      public Shared.DamageFlags ranged_flags;

      public int ranged_range;

      public bool ranged_ap => default != (ranged_flags & Shared.DamageFlags.ArmorPiercing);

      public int slow_reload => ((int)ranged_flags >> (int)Shared.DamageFlags.SlowReload1) & 3;

      public int reloading;

      public int shield_block => base_stats.shield_parry;

      public int shield_prot => base_stats.shield_block;

      public BattleUnit Clone() {
         var r = MemberwiseClone() as BattleUnit;

         return r;
      }
   }


   public static IEnumerable<Vector2Int> BoxInts(int size_l) {
      int x = 0;
      int y = 0;


      for (int n = 0; n * 2 < size_l; n++) {
         for (; y <= n; y++) {
            yield return new(x, y);
         }

         for (; x <= n; x++) {
            yield return new(x, y);
         }


         for (; y > -n; y--) {
            yield return new(x, y);
         }

         for (; x > -n; x--) {
            yield return new(x, y);
         }
      }
   }

   public static IEnumerable<Vector2Int> SparseBoxInts(int size_l) {
      return BoxInts(size_l * 2).Where(x => Mathf.Abs(x.x + x.y) % 2 == 0);
   }


   public static IEnumerable<Vector2Int> GenLine(int size_l) {
      for (int i = 0; i < size_l; i++) {
         for (int x = 0; x < 60; x++) {
            yield return new(i, x % 2 == 0 ? x / 2 : -(x + 1) / 2);
         }
      }
   }

   static IEnumerable<Vector2Int> Gen2Line(int size_l) {
      for (int i = 0; i * 2 < size_l; i++) {
         for (int x = 0; x < 60; x++) {
            yield return new(i * 2, x % 2 == 0 ? x / 2 : -(x + 1) / 2);
            yield return new(i * 2 + 1, x % 2 == 0 ? x / 2 : -(x + 1) / 2);
         }
      }
   }
   public static IEnumerable<Vector2Int> GenRect(int size_l, int width, int height) {
      
      
      Vector2Int dim = Vector2Int.one;

      yield return new(0, 0);

      static int AltDiv(int i) {
         if (i % 2 == 0) return i / 2;
         return -(i / 2 + 1);
      }


      IEnumerable<Vector2Int> AddRow() {
         dim.y++;

         Vector2Int p = new(0, AltDiv(dim.y - 1));

         for (int x = 0; x < dim.x; x++) {
            yield return p;
            p.x++;
         }
      }

      IEnumerable<Vector2Int> AddColumn() {
         dim.x++;

         for (int i = 0; i < dim.y; i++) {
            yield return new(dim.x - 1, AltDiv(i));
         }
      }

      while (dim.x * dim.y < size_l * 60) {

         if ((dim.x + 1) * height < (dim.y + 1) * width) {
            foreach (var p in AddColumn()) {
               yield return p;
            }
         } else {
            foreach (var p in AddRow()) {
               yield return p;
            }
         }
         
      }
   }

   static Dictionary<BattleArmyTemplate.FormationType, IEnumerable<Vector2Int>> formations;
}