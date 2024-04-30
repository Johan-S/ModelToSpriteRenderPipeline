using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BattleData;

public static class BattleArmyTemplate {
   public enum FormationType {
      Box,
      Line,
      DoubleLine,
      Sparse,
      Rectangle,
   }
   
   
   [System.Serializable]
   public class ScriptedAction {
      public ScriptActionType order;

      public string spell = "";
   }

   [Serializable]
   public class HeroUnit {
      public UnitTypeObject type;
      public UnitTypeObject unit_type => type;

      public Vector2Int pos;

      public List<ScriptedAction> actions = new();


      public UnitTargetingOrder targeting_order;

      public int dummy_end_field;
   }

   [Serializable]
   public class Group {
      public UnitTypeObject type;
      public Vector2Int pos;

      public int amount = 1;

      public FormationType formation_type;
      public UnitTargetingOrder targeting_order;

      public UnitMovementOrder movement_order;
   }
}


public static class MagicManager {
   

   public enum EffectResultType {
      None,
      Damage,
      Buff,
   }

   public enum EffectElement {
      Pierce,
      Fire,
      Force,
      Chromatic,
      Shadow,
      Poison,
   }

   public enum EffectShape {
      Projectile,
      Caster,
      Chain,
   }
}

public static class Extend_BattleData {
   public static bool Alive(this BattleData.BattleUnit u) {
      return u != null && u.hp > 0;
   }

   public static bool invalid(this BattleData.BattleUnit u) {
      return !u.valid();
   }

   public static bool valid(this BattleData.BattleUnit u) {
      return u != null && u.hp > 0;
   }
}


public static partial class BattleData {
   

   public enum UnitTargetingOrder {
      Default,
      Closest,
      Random,
      Rear,
      Hold,
   }

   public enum UnitMovementOrder {
      Default,
      HoldAttack,
      ShootKeepDistance,
   }

   public enum ScriptActionType {
      Wait,
      Move,
      Cast,
      AdvanceThenCast,
      Advance,
      Repeat,
   }

   public class ScriptedAction {
      public string name;
      public string display_name => name.Replace("_", " ");

      public ScriptActionType type;
      public string ability_name;

      public UnitTargetingOrder targeting_order;
   }

   [Flags]
   public enum UnitBuffEnum {
      Blessing = 1 << 0,
      Aurora_Shield = 1 << 1,
      Heavenly_Ascension = 1 << 2,
      Communion_Master = 1 << 3,
      Communion_Slave = 1 << 4,
      Spritual_Aspiration = 1 << 5,
      Spiritual_Awakening = 1 << 6,
      Spiritual_Wave = 1 << 7,
      Flicker = 1 << 8,
      Astral_Blades = 1 << 9,
      Spirit_Warriors = 1 << 10,
      Strengthen_Armor = 1 << 11,
      Sharpen_Weapons = 1 << 12,
      Strengthen_Resolve = 1 << 13,
      Marching_Song = 1 << 14,
   }

   [System.Serializable]
   public class UnitBuff : Named {
      public string name => BuffName;
      public string BuffName;
      public string paths = "";
      public string display_name => BuffName.ToTitleCase();

      public int def_bonus;
      public int prot_bonus;
      public int strength_bonus;
      public int size_bonus;

      public int speed_bonus;


      public UnitBuffEnum buff_enum;


      public (string field, int val)[] IntDecsr() {
         return typeof(UnitBuff).GetFields().filter(x => x.FieldType == typeof(int))
            .map(x => (x.Name, (int)x.GetValue(this))).filter(x => x.Item2 != 0);
      }

      public string TooltipString() {
         return $"{display_name} - {IntDecsr().join(", ", x => $"{x.val} {x.field.ToTitleCase()}")}";
      }


      public MagicManager.EffectElement effect_element = MagicManager.EffectElement.Force;
      public string GetMessage() {
         return TooltipString();
      }
   }

   public class DelayedAction {
      public int impact_tick;
      public Action callback;

      public DelayedAction(int impact_tick, Action callback) {
         this.impact_tick = impact_tick;
         this.callback = callback;
      }
   }
   
}


public interface IEngineDataPart {
   void AddTypesTo(LoadDataFlow flow);
}

public class LoadDataFlow {

   public int parse_count;
   public int error_count;
   
   public GameTypeCollection gear_data = new();
   public List<Action<GameTypeCollection>> lazy = new();

   public Dictionary<object, (string htype, string[] row, string[] hdrs)> row_back_map = new();
}