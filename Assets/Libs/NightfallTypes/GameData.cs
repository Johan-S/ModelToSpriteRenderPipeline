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
   public static void TransferFieldData<T>(T from_o, T to_o) => Std.CopyFieldsTo(from_o, to_o);

   public static void TransferFieldData(object from_o, object to_o, bool deep = true) =>
      Std.CopyFieldsTo(from_o, to_o, deep);


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

      public Shared.UnitAnimationSprites animation_sprites;


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

      public Shared.DamageFlags melee_flags;

      [Header("Range")] [Stat(ignore_zero = true)]
      public int ranged_damage;

      [Stat(ignore_zero = true)] public int ranged_range;

      public Shared.DamageFlags ranged_flags;

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

   public static IEnumerable<(string name, T data)> GetAllStats<T>(object o) {
      foreach (var kv in StatsAnnotations.DefaultStats(o)) {
         if (kv.val is T) yield return (kv.name, (T)kv.val);
      }
   }
}