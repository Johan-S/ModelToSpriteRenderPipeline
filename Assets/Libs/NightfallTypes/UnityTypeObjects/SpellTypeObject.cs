using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellTypeObject : ScriptableObject {
   

   public string School;
   public string combat_spell;
   [Stat] public string Category;
   public string SpellName;
   public string Description;
   [Stat] public string Path1;
   [Stat] public string Path2;
   [Stat] public DataTypes.SpellCostField Cost;
   [Stat] public string Range;
   [Stat] public string AOE;
   [Stat] public string AOE_Type;
   [Stat] public string effect_count;
   [Stat] public string Prec;
   [Stat] public string Link;
   [Stat] public DataTypes.DamageField Dmg;
   [Stat] public string Linger;
   public DataTypes.GenericEffectList Checks;
   [Stat] public string Tags;
   [Stat] public DataTypes.GenericEffectList Extra_Effects;
}