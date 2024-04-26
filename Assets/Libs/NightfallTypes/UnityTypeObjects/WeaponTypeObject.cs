using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponTypeObject : ScriptableObject {


   public int Weapon_ID;
   public string Weapon_Name;
   [Stat] public int Damage;
   [Stat] public string Hands_Requirement;
   [Stat] public string Damage_Type;
   [Stat(ignore_zero = true)] public int Number_Of_Attacks;
   [Stat(ignore_zero = true)] public int Cooldown;
   [Stat] public int Length;
   [Stat(ignore_zero = true)] public int Attack;
   [Stat(ignore_zero = true)] public int Defense;
   [Stat(ignore_zero = true)] public int Encumberance;
   [Stat(ignore_zero = true)] public int Resources;
   [Stat(ignore_zero = true)] public string Range;
   [Stat(ignore_zero = true)] public int Precision;
   [Stat] public string Material;
   public DataTypes.GenericEffectList Effects;
}