using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using static DataTypes;
using System.Collections.Generic;
using System.Reflection;

public interface Copyable {
   Copyable DeepCopy();
}


public static partial class GameData {
   public static void TransferFieldData<T>(T from_o, T to_o) {
      var t = typeof(T);
      var fs = t.GetFields();
      foreach (var f in fs) {
         if (f.IsStatic) continue;
         var o = f.GetValue(from_o);
         if (o is Copyable cp) o = cp.DeepCopy();
         f.SetValue(to_o, o);
      }
   }

   public static void TransferFieldData(object from_o, object to_o, bool deep=true) {
      var t = to_o.GetType();
      foreach (var o in Std.GetFields(from_o)) {
         var v = o.val;

         var fi = t.GetField(o.name);
         if (fi == null) continue;
         if (fi.FieldType.IsInstanceOfType(v)) {
            if (deep && v is Copyable cp) v = cp.DeepCopy();
            fi.SetValue(to_o, v);
         }
      }
   }

   public static void ParseUnitToUnityObject(UnitType from_o, UnitTypeObject to_o) {
      to_o.name = from_o.name;
      TransferFieldData(from_o, to_o);

      if (from_o.animation_sprites != null)
         TransferFieldData(from_o.animation_sprites, to_o, deep: false);

      to_o.sprite_ref = from_o.sprite;
      to_o.magic_paths = from_o.magic_paths.ToArray();
   }
   public static void TransferParseUnit(UnitTypeObject from_o, DataTypes.UnitTypeClass uclass, UnitType res) {
      res.name = from_o.name;

      TransferFieldData(from_o, res);

      res.animation_sprites = new();

      TransferFieldData(from_o.animations, res.animation_sprites, deep: false);

      res.sprite = from_o.sprite;

      res.magic_paths = new(from_o.magic_paths ?? new MagicPath[0]);

      if (uclass != null) {
         TransferFieldData(uclass, res);
      }
   }

   public static UnitType ParseUnit(UnitTypeObject from_o, DataTypes.UnitTypeClass uclass) {
      var res = new UnitType();
      TransferParseUnit(from_o, uclass, res);
      return res;
   }

   public class GameDataBase : Named {
      public string name { get; set; }

      public static implicit operator bool(GameDataBase d) => d != null && d.name.IsNonEmpty();
   }


   [Serializable]
   public class UnitType : GameDataBase {
      public int Strength;
      public int M_Affinity;
      public int Attack;
      public int Defense;
      public int Precision;
      public int Population_Cost;
      
      
      public Sprite icon_sprite;

      public float animation_scale = 1;

      public string animation_class;

      public WeaponMelee Weapon_Primary => primary_weapon;
      public WeaponMelee Weapon_Secondary => secondary_weapon;

      public WeaponMelee Innate_Primary => innate_primary;
      public WeaponMelee Innate_Secondary => innate_secondary;
      public Armor armor;
      public Shield shield;
      public Helmet helmet;
      public WeaponMelee primary_weapon;
      public WeaponMelee secondary_weapon;
      
      public WeaponMelee innate_primary;
      public WeaponMelee innate_secondary;


      public Sprite sprite;

      public UnitStats stats;

      public bool commander;

      public UnitAnimationSprites animation_sprites;


      public MagicPathList magic_paths;
      public override string ToString() {
         return $"UnitType<{name}>";
      }
   }

   [Serializable]
   public class UnitStats : Copyable {
      [Header("Defense")] [Stat] public int hp = 10;

      [Stat] public int def = 10;

      [Stat] public int ma = 10;

      [Stat] public int move_speed = 12;

      [Stat] public int prot_nat = 0;

      [Stat] public int prot_head = 0;

      [Stat] public int prot_body = 0;


      [Stat(ignore_zero = true)] public int shield_parry;
      [Stat(ignore_zero = true)] public int shield_block;

      [Stat] public int size = 2;

      [Header("Melee")] [Stat] public int melee_attack = 10;

      [Stat] public int melee_damage;

      public DamageFlags melee_flags;

      [Header("Range")] [Stat(ignore_zero = true)]
      public int ranged_damage;

      [Stat(ignore_zero = true)] public int ranged_range;

      public DamageFlags ranged_flags;

      [Header("Cost")] [Stat(ignore_zero = true)]
      public int gold_cost = 3;

      [Stat(ignore_zero = true)] public int resource_cost = 3;


      public UnitStats Copy() {
         return (UnitStats)MemberwiseClone();
      }

      public Copyable DeepCopy() {
         return Copy();
      }
   }

   [Flags]
   public enum DamageFlags {
      None = 0,
      ArmorPiercing = 1 << 0,
      Blunt = 1 << 1,
      Piercing = 1 << 2,
      Slashing = 1 << 3,

      SlowReload1 = 1 << 4,
      SlowReload2 = 1 << 5,

      Knockback = 1 << 6,
      Trample = 1 << 7,
      JudoThrow = 1 << 8,
      ArmorNegating = 1 << 9,
      Magic = 1 << 10,
      Fire = 1 << 11,
      Poison = 1 << 12,
      Death = 1 << 12,
      Shock = 1 << 13,
      Attack = 1 << 14,
      RandomElement = 1 << 15,
      Healing = 1 << 16,
      Holy = 1 << 17,
   }

   public static IEnumerable<(string name, T data)> GetAllStats<T>(object o) {
      foreach (var kv in StatsAnnotations.DefaultStats(o)) {
         if (kv.val is T) yield return (kv.name, (T)kv.val);
      }
   }


   [Serializable]
   public class AnimationBundle  {

      public static implicit operator bool(AnimationBundle b) => b != null && b.sprites.IsNonEmpty();
      public AnimationBundle() {
      }


      public int animation_duration_ms = 1000 / 3;

      public Sprite[] sprites;
      public int[] time_ms;

      public AnimationBundle(Sprite[] sprites, int[] time_ms) {
         this.animation_duration_ms = time_ms.Sum();
         this.sprites = sprites;
         this.time_ms = time_ms;
      }

      public AnimationBundle(int animation_duration_ms, Sprite[] sprites, int[] time_ms) {
         this.animation_duration_ms = animation_duration_ms;
         this.sprites = sprites;
         this.time_ms = time_ms;
      }

      public int GetRendLarge(int ms) {
         int l = ms / animation_duration_ms;

         return GetRend(ms % animation_duration_ms) + l * sprites.Length;
      }

      public int GetRend(int ms) {

         ms %= animation_duration_ms;
         for (int j = 0; j < time_ms.Length; j++) {
            ms -= time_ms[j];
            if (ms < 0) return j;
         }

         return time_ms.Length - 1;
      }

      public AnimationBundle ShallowCopy() {
         return (AnimationBundle)MemberwiseClone();
      }

      public AnimationBundle Copy() {
         return new(animation_duration_ms, sprites?.Copy(), time_ms?.Copy());
      }
   }


   [Serializable]
   public class UnitAnimationSprites : Copyable {

      public string GetLogStr() {
         return GetAllAnimations().join(", ", x => $"{x.name}, {x.am?.sprites?.Length} {x.am.sprites.Count(t => t)} ,");
      }
      
      [Stat("Idle")] public AnimationBundle idle_animation = new();
      
      [Stat("Attack1")] public AnimationBundle attack_animation = new();

      [Stat("Attack2")] public AnimationBundle attack_animation_2 = new();
      [Stat("Ranged1")] public AnimationBundle attack_ranged_animation_1 = new();

      [Stat("Block")] public AnimationBundle on_hit_animation = new();


      [Stat("Walk")] public AnimationBundle walk_animation = new();
      [Stat("Run")] public AnimationBundle run_animation = new();

      [Stat("Fall")] public AnimationBundle knockback_animation = new();
      [Stat("Knockdown")] public AnimationBundle knockdown_animation = new();
      [Stat("Special1")] public AnimationBundle cast_ability_animation = new();

      [Stat("Death")] public AnimationBundle death_animation = new();


      [Stat("Standup")] public AnimationBundle standup_animation = new();


      [Stat("BlockBreak")] public AnimationBundle block_break_animation = new();
      public IEnumerable<(string name, AnimationBundle am)> GetAllAnimations() {
         foreach (var st in StatsAnnotations.DefaultStats(this)) {
            yield return (st.name.Replace(" ", ""), (AnimationBundle)st.val);
         }
      }

      public void Set(string name, AnimationBundle b) {
         Stat.Set(this, name, b);
      }

      public UnitAnimationSprites Copy() {
         var res = new UnitAnimationSprites();

         TransferFieldData(this, res);
         return res;
      }

      public Copyable DeepCopy() {
         return Copy();
      }

   }
}