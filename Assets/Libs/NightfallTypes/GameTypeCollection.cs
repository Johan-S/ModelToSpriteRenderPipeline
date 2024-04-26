using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using static DataTypes;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using static GameData.DamageFlags;

public static class DataTypes {

   public static string context_msg => EngineDataInit.last_parsed_asset?.GetDebugPrefix() ?? "";
   
   [Serializable]
   public class GenericEffectList : CustomListBase<GenericEffectHandle> {
      public GenericEffectList() {
      }

      public GenericEffectList(IEnumerable<GenericEffectHandle> o) : base(o) {
      }
   }

   [Serializable]
   public class MagicPathList : CustomListBase<MagicPath> {
      public MagicPathList() {
      }

      public MagicPathList(IEnumerable<MagicPath> o) : base(o) {
      }

      public MagicPathList Copy() {
         return new(this);
      }
   }

   public static (GameData.DamageFlags flag, string key)[] flag_key_map = {
      (ArmorNegating, "AN"),
      (ArmorPiercing, "AP"),
      (Fire, "F"),
      (Death, "D"),
      (Magic, "M"),
      (Poison, "Po"),
      (Blunt, "B"),
      (Slashing, "S"),
      (Piercing, "P"),
      (Shock, "Sh"),
      (RandomElement, "R"),
      (Healing, "He"),
      (Holy, "H"),
   };

   static GameData.DamageFlags ParseDamageFlagSymbol(string s) {
      var f = flag_key_map.Find(x => x.key.Equals(s, StringComparison.InvariantCultureIgnoreCase));
      if (f.flag == default) {
         if (Enum.TryParse(s, out GameData.DamageFlags res)) {
            return res;
         }
         Debug.LogError(context_msg + $"Failed to parse DamageFlags: '{s}'");
      }

      return f.flag;
   }

   [Serializable]
   public class DamageField {
      public GameData.DamageFlags flags;


      public int amount;
      

      public override string ToString() {
         if (flags != default) {
            // var fl = flag_key_map.Where(x => flags.HasFlag(x.flag)).join(",", x => x.flag.ToString());
            return $"{amount} {flags}";
         }

         return $"{amount}";
      }


      public static DamageField ParseOld(string msg) {
         if (msg.IsNullOrEmpty()) return null;
         var parts = Regex.Split(msg, "[+]");
         var d = new DamageField();
         if (!int.TryParse(parts[0], out d.amount)) {
            Debug.LogError(context_msg + $"Bad damage string: {msg}");
            return null;
         }

         for (int i = 1; i < parts.Length; i++) {
            d.flags |= ParseDamageFlagSymbol(parts[i]);
         }

         return d;
      }

      public static DamageField Parse(string msg) {
         if (msg.IsNullOrEmpty()) return null;
         var op = Regex.Match(msg, @"[-+]?\d+", RegexOptions.Compiled);
         int val = 0;
         if (op.Success) {
            val = int.Parse(op.Value);
            msg = msg.Remove(op.Index, op.Length);
         } else {
            // Debug.Log($"No damage in: {msg}");
         }

         var parts = Regex.Split(msg, "[_+]", RegexOptions.Compiled).filter(x => x.Length > 0);
         var d = new DamageField();

         d.amount = val;

         for (int i = 0; i < parts.Length; i++) {
            var fl = ParseDamageFlagSymbol(parts[i]);
            d.flags |= fl;
            // if (fl == default) Debug.Log(context_msg + $"Failed to parse damage flag: {parts[i]}");
         }

         return d;
      }
   }

   [Serializable]
   public struct MagicPath {
      public MagicSchool school;
      public int skill;


      public string name => school.ToString();
      public int value => skill;

      public override string ToString() {
         return $"{school} {skill}";
      }
   }


   public static int ParseStat(UnitTypeClass du, string val) {
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

   [Flags]
   public enum SpellTag {
      Friendly = 1,
   }

   public enum SpellCategories {
      None,
      Evocation,
      Enchantment,
      Summon,
      Buff,
      Debuff,
      LocalEnchantment,
      Transformation,
   }

   public static char Abr(this MagicSchool s) {
      switch (s) {
         case MagicSchool.Heavens:
            return 'H';
            break;
         case MagicSchool.Terra:
            return 'T';
            break;
         case MagicSchool.Entropy:
            return 'E';
            break;
         case MagicSchool.Order:
            return 'O';
            break;
         case MagicSchool.Spirit:
            return 'S';
            break;
         case MagicSchool.Body:
            return 'B';
            break;
         case MagicSchool.Growth:
            return 'G';
            break;
         case MagicSchool.Death:
            return 'D';
            break;
         default:
            throw new ArgumentOutOfRangeException(nameof(s), s, null);
      }
   }

   public class TableMeta {
      public FieldInfo[] field_infos;
      public string[] field_names;

      public TableMeta(Type type) {
         field_infos = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetField).ToArray();
         field_names = field_infos.ArrayMap(x => x.Name);
      }
   }

   public enum BattleUnitResource {
      Untyped,
      Health,
      Fatigue,
      Gems,
      FriendlyBlood,
   }

   static bool ParseGenericKv(string msg, out (string type, string val) res) {
      if (msg.Contains("+")) {
         // "Type+Amount"
         res = msg.SplitE("+", 2).Unpack2();
         return true;
      }

      var m = Regex.Match(msg, "-?\\d+", RegexOptions.Compiled);
      if (m.Success) {
         // "AmountType"
         res = (msg.Substring(m.Length), m.Value);
         return true;
      }

      res = (msg, "0");
      return false;
   }

   static (string type, string val) ParseGenericKv(string msg) {
      if (ParseGenericKv(msg, out var res)) {
         return res;
      }

      Debug.LogError(context_msg + $"Unknown resource descriptor of length {msg.Length}: '{msg}' , expects format TYPE+ARGUMENT");
      return (msg, "0");
   }

   [Serializable]
   public class ResourceTuple {
      public ResourceTuple(int amount, BattleUnitResource type) {
         this.amount = amount;
         this.type = type;
      }

      public int amount;

      public BattleUnitResource type;

      public static implicit operator ResourceTuple((int, BattleUnitResource) ab) => new(ab.Item1, ab.Item2);

      public void Deconstruct(out int amount, out BattleUnitResource type) {
         amount = this.amount;
         type = this.type;
      }

      public static ResourceTuple Parse(string msg) {
         if (!ParseGenericKv(msg, out var kv)) {
            Debug.LogError(context_msg + $"Couldn't parse resource: {msg}");
            return (0, default);
         }

         var tp = kv.type;


         int amount = int.Parse(kv.val);
         BattleUnitResource rt = default;

         if (tp.ToLower() == "f") return (amount, BattleUnitResource.Fatigue);
         if (Enum.TryParse(tp, out rt)) return (amount, rt);

         if (tp.Length > 0) return (amount, BattleUnitResource.Gems);

         Debug.Log(context_msg + $"Untyped msg: {msg}");
         return (amount, default);
      }

      public override string ToString() {
         return $"{amount} {type.ToString().ToTitleCase()}";
      }
   }

   public class SpellCostField {
      public ResourceTuple[] costs;

      public static SpellCostField Parse(string data) {
         if (data.IsNullOrEmpty()) return null;
         SpellCostField r = new SpellCostField();

         var vs = data.Split(",");
         r.costs = vs.map(ResourceTuple.Parse);

         return r;
      }

      public override string ToString() {
         return costs.join(" + ");
      }
   }

   [Serializable]
   public class MagicSpell : Named {
      public string display_name => name.Replace("_", " ");

      public string name {
         get => SpellName;
         set => SpellName = value;
      }

      public string School;
      public string combat_spell;
      [Stat] public string Category;
      public string SpellName;
      public string Description;
      [Stat] public string Path1;
      [Stat] public string Path2;
      [Stat] public SpellCostField Cost;
      [Stat] public string Range;
      [Stat] public string AOE;
      [Stat] public string AOE_Type;
      [Stat] public string effect_count;
      [Stat] public string Prec;
      [Stat] public string Link;
      [Stat] public DamageField Dmg;
      [Stat] public string Linger;
      public GenericEffectList Checks;
      [Stat] public string Tags;
      [Stat] public GenericEffectList Extra_Effects;

      public static TableMeta table_meta = new TableMeta(typeof(MagicSpell));

      [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
      static void RefreshMeta() {
         table_meta = new TableMeta(typeof(MagicSpell));
      }

      public static FieldInfo[] field_infos => table_meta.field_infos;
      public static string[] field_names => table_meta.field_names;


      public object[] field_values {
         get { return field_infos.ArrayMap(x => x.GetValue(this) ?? ""); }
      }

      public IEnumerable<KeyVal> nonemoty_field_values =>
         StatsAnnotations.DefaultStats(this).Where(x => Std.IsNotEmpty(x.val));

      public string TooltipString() {
         return $"{display_name} - {Description.Replace("_", " ")}";
      }

      public string GetMessage() {
         return TooltipString();
      }
   }

   [Serializable]
   public class SimpleSpell : Named {
      public string display_name => object_name.ToTitleCase();

      public string name => object_name;

      public string object_name;
      public string Path;
      public string Effect;


      string parsed_effect;

      string TooltipString() {
         if (parsed_effect.IsNullOrEmpty()) {
            var sp = Effect.SplitPairs(",", "=");

            var e = sp.join(", ", kv => $"{kv.v} {kv.k.ToTitleCase()}");

            parsed_effect = $"{display_name} - {e}";
         }

         return parsed_effect;
      }

      public string GetMessage() {
         return TooltipString();
      }
   }

   public enum MagicSchool {
      Heavens,
      Terra,
      Entropy,
      Order,
      Spirit,
      Body,
      Growth,
      Death,
      Chaos,
   }

   public static (string a, string b) ParsePlus(string plus_val) {
      var arr = plus_val.Split("+");
      Debug.Assert(arr.Length == 2);
      return (arr[0], arr[1]);
   }

   public static MagicPath[] ParseMagicArray(params string[] rows) {
      return
         rows.FlatMap(row => ParseMagic(row).ArrayMap(x => new MagicPath { school = x.school, skill = x.skill }))
            .ToArray();
   }

   public static MagicPathList ParseMagicStruct(params string[] rows) {
      return
         new MagicPathList(ParseMagicArray(rows));
   }

   static string[] GenericSoupValues(string raw_i) {
      var spls = Regex.Split(raw_i, @"(\+|)", RegexOptions.Compiled);
      throw new NotImplementedException();
   }

   static GenericEffectHandle[] ParseFuncCals(string raw_i) {
      int lp = 0;

      int last_cut = 0;

      List<(string call, List<string> args)> res = new();
      int i = 0;

      for (; i < raw_i.Length; i++) {
         var c = raw_i[i];
         if (c == '(') {
            if (lp == 0) {
               var cs = raw_i[last_cut..i].Trim();
               res.Add((cs, new()));
               last_cut = i + 1;
            }

            lp++;
         } else if (c == ')') {
            lp--;
            if (lp == 0) {
               var cs = raw_i[last_cut..i].Trim();
               if (cs.Length > 0) res[^1].args.Add(cs);
               last_cut = i + 1;
            }
         }

         if (c == ',') {
            if (lp == 0) {
               var cs = raw_i[last_cut..i].Trim();
               if (cs.Length > 0) res.Add((cs, new()));
               last_cut = i + 1;
            } else if (lp == 1) {
               var cs = raw_i[last_cut..i].Trim();
               if (cs.Length > 0) res[^1].args.Add(cs);
               last_cut = i + 1;
            }
         }
      }

      if (lp == 0) {
         var cs = raw_i[last_cut..i].Trim();
         if (cs.Length > 0) res.Add((cs, new()));
         last_cut = i + 1;
      } else if (lp == 1) {
         var cs = raw_i[last_cut..i].Trim();
         if (cs.Length > 0) res[^1].args.Add(cs);
         last_cut = i + 1;
      }

      return res.Select(x => new GenericEffectHandle { name = x.call, args = x.args.ToArray() }).ToArray();
   }

   public static GenericEffectHandle[] ParseAbilityHandle(string raw_i) {
      if (raw_i.Contains('(')) {
         return ParseFuncCals(raw_i);
      }

      return raw_i.SplitE(",").ArrayMap(p => {
         var ps = p.SplitE("+");
         return new GenericEffectHandle { name = ps[0], args = ps[1..] };
      });
   }

   public static (MagicSchool school, int skill)[] ParseMagic(string row) {
      return row.Split(",")
         .filter(x => x.Length > 0)
         .map(ParsePlus)
         .map(x => (Enum.Parse<MagicSchool>(x.a), int.Parse(x.b)));
   }

   public static FieldInfo[] GetFields<T>(T t) {
      return typeof(T).GetFields();
   }

   public static (string name, object val)[] GetData<T>(T t) {
      return GetFields(t).map(f => (f.Name, f.GetValue(t)));
   }

   [Serializable]
   public class Shield : Named {
      public static implicit operator bool(Shield a) => a != null && a.name.IsNonEmpty();

      public string name {
         get => Shield_Name;
         set => Shield_Name = value;
      }

      public int Shield_ID;
      public string Shield_Name;
      public int Shield_Protection;
      public int Defense;
      public int Parry;
      public int Encumberance;
      public int Resource_Cost;
      public string Material;
      public string Effects;

      public override string ToString() {
         return Shield_Name;
      }
   }

   [Serializable]
   public class WeaponMelee : Named {
      public static implicit operator bool(WeaponMelee a) => a != null && a.name.IsNonEmpty();

      public string name {
         get => Weapon_Name;
         set => Weapon_Name = value;
      }

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
      public GenericEffectList Effects;

      public IEnumerable<KeyVal> base_stats => StatsAnnotations.DefaultStats(this).Where(x => Std.IsNotEmpty(x.val));

      public int GetStrengthBonus(int strength) {
         foreach (var eff in Effects) {
            if (eff.name == "No_StrBonus") return 0;
         }

         foreach (var eff in Effects) {
            if (eff.name == "1/3_StrBonus") return strength / 3;
         }

         return strength;
      }

      public bool IsRanged => Range.IsNotZero();

      public override string ToString() {
         return Weapon_Name;
      }
   }

   [Serializable]
   public class Armor : Named {
      public static implicit operator bool(Armor a) => a != null && a.name.IsNonEmpty();

      public string name {
         get => Armor_Name;
         set => Armor_Name = value;
      }

      public int Armor_ID;
      public string Armor_Name;
      public int Protection_Head;
      public int Protection_Body;
      public int Attack;
      public int Defense;
      public int Encumberance;
      public int Resource_Cost;

      public int Combat_Speed;
      public int Map_Move;

      public string Material;
      public string Effects;

      public override string ToString() {
         return Armor_Name;
      }
   }

   [Serializable]
   public class Helmet : Named {
      public static implicit operator bool(Helmet a) => a != null && a.name.IsNonEmpty();

      public string name {
         get => Armor_Name;
         set => Armor_Name = value;
      }

      public int Armor_ID;
      public string Armor_Name;
      public int Protection_Head;
      public int Protection_Body;
      public int Attack;
      public int Defense;
      public int Encumberance;
      public int Resource_Cost;
      public string Material;
      public string Effects;

      public override string ToString() {
         return Armor_Name;
      }
   }
   
   

   [Serializable]
   public class UnitTypeClass : Named {
      public static implicit operator bool(UnitTypeClass a) => a != null && a.name.IsNonEmpty();

      public string name {
         get => Unit_Name;
         set => Unit_Name = value;
      }

      public int Unit_Id;
      public string Unit_Name;
      public string Faction;

      public string AnimationType;
      public string Template;

      public int Gold_Cost;
      public int Training_Cost;
      public int Resources_Cost;
      public int Population_Cost;
      public string Type;
      [SerializeReference] public WeaponMelee Weapon_Primary;
      [SerializeReference] public WeaponMelee Weapon_Secondary;
      public string Quiver_Slot;
      [SerializeReference] public WeaponMelee Innate_Primary;
      [SerializeReference] public WeaponMelee Innate_Secondary;
      [SerializeReference] public Shield Shield;
      [SerializeReference] public Helmet Helmet;
      [SerializeReference] public Armor Armor;
      public string Mount;
      public string Anatomy;
      public int Size;
      public int HP;
      public int Strength;
      public int M_Affinity;
      public int Resistence;
      public int Nat_Protection;
      public int Attack;
      public int Defense;
      public int Precision;
      public int Combat_Speed;
      public int Enc;
      public int Morale;
      public int Cur_Age;
      public int Max_Age;
      public string Combat_Speed2;
      public GenericEffectList Abilities;
      public MagicPathList Magic;
      public int Leadership;

      public Sprite icon {
         get;
         set;
      }
      public override string ToString() {
         return Unit_Name;
      }
   }


   public static Type[] data_types => new[] {
      typeof(MagicSpell),
      typeof(Armor),
      typeof(UnitTypeClass),
      typeof(Shield),
      typeof(Helmet),
      typeof(WeaponMelee),
      typeof(SimpleSpell),
      typeof(BattleData.UnitBuff),
   };


   [Serializable]
   public class GenericEffectHandle {
      public string name;

      public string[] args;

      public string arg_msg => args.join("+");

      public string val => arg_msg;

      public override string ToString() {
         var res = name;
         if (args != null && args.Length > 0) res += "+" + args.join("+");

         return res;
      }
   }


   static Dictionary<Type, Func<FieldInfo, string, object>> type_parsers;

   static DataTypes() {
      type_parsers = new();

      type_parsers[typeof(int)] = (ctx, item) => {
         if (!int.TryParse(item, out var sr)) {
            if (item == "") sr = 0;
            else Debug.LogError(context_msg + $"Failed to parse int '{item}' for field {ctx.Name}");
         }

         return sr;
      };
      type_parsers[typeof(string)] = (ctx, item) => item;
      type_parsers[typeof(MagicPathList)] = (ctx, item) => ParseMagicStruct(item);

      type_parsers[typeof(GenericEffectList)] = (ctx, item) =>
         new GenericEffectList(ParseAbilityHandle(item));


      type_parsers[typeof(DamageField)] = (ctx, item) =>
         DamageField.Parse(item);


      type_parsers[typeof(SpellCostField)] = (ctx, item) =>
         SpellCostField.Parse(item);
   }

   public static bool ParseSetItem(object o, FieldInfo f, string v) {
      var item = v.Trim();
      var parsed = type_parsers[f.FieldType](f, item);
      f.SetValue(o, parsed);
      return true;
   }
}


public class GameTypeCollection {

   public List<ExportPipelineSheets.AnimationParsed> parsed_animations = new ();
   
   public LookupList<GameData.UnitType> game_units;
   
   
   public LookupList<Shield> shields;
   public LookupList<WeaponMelee> melee_weapons;
   public LookupList<Armor> armors;
   public LookupList<Helmet> helmets;
   public LookupList<UnitTypeClass> units;
   public LookupList<SimpleSpell> simple_spells;
   public LookupList<BattleData.UnitBuff> simple_buffs;
   public LookupList<MagicSpell> magic_spells;

   public bool Get(string name, out Shield w) => shields.Get(name, out w);
   public bool Get(string name, out WeaponMelee w) => melee_weapons.Get(name, out w);
   public bool Get(string name, out Armor w) => armors.Get(name, out w);
   public bool Get(string name, out Helmet w) => helmets.Get(name, out w);
   public bool Get(string name, out UnitTypeClass w) => units.Get(name, out w);


   public object GetGeneric(Type t, string name) {
      if (t == typeof(Shield)) return shields.Get(name);
      if (t == typeof(WeaponMelee)) return melee_weapons.Get(name);
      if (t == typeof(Armor)) return armors.Get(name);
      if (t == typeof(Helmet)) return helmets.Get(name);
      if (t == typeof(UnitTypeClass)) return units.Get(name);
      if (t == typeof(SimpleSpell)) return simple_spells.Get(name);
      if (t == typeof(MagicSpell)) return magic_spells.Get(name);
      if (t == typeof(BattleData.UnitBuff)) return simple_buffs.Get(name);

      return null;
   }

   public static void SetFields_IgnoreMissing(object o, (string k, string v)[] kvs) {
      var t = o.GetType();

      foreach (var (k, v) in kvs) {
         var f = t.GetField(k);
         if (f == null) continue;
         ParseSetItem(o, f, v);
      }
   }

   public static void SetFields(object o, (string k, string v)[] kvs) {
      var t = o.GetType();

      foreach (var (k, v) in kvs) {
         var f = t.GetField(k);
         Debug.Assert(f != null);
         ParseSetItem(o, f, v);
      }
   }

   static T ParseKv<T>(params (string k, string v)[] kvs) where T : new() {
      T o = new();
      SetFields(o, kvs);

      return o;
   }

   public static void ParseRows_Finalize<T>(GameTypeCollection g, T o, string[] data_row) where T : new() {
      var t = typeof(T);

      var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetField);
      int field_i = 0;
      fields.Zip(data_row, (f, item) => {
         if (data_types.Contains(f.FieldType)) {
            var go = g.GetGeneric(f.FieldType, item);
            f.SetValue(o, go);
         }

         field_i++;
         return true;
      }).ToArray();
   }

   public static T ParseRows<T>(string[] data_row) where T : new() {
      T o = new();


      var t = typeof(T);

      var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetField);
      int field_i = 0;
      fields.Zip(data_row, (f, item) => {
         if (data_types.Contains(f.FieldType)) {
         } else {
            ParseSetItem(o, f, item);
         }

         field_i++;
         return true;
      }).ToArray();
      return o;
   }

   public UnitTypeObject GetAsset(UnitTypeClass du) {
      var ut = GetGameData(du);
      UnitTypeObject asset = ScriptableObject.CreateInstance<UnitTypeObject>();
      
      GameData.ParseUnitToUnityObject(ut, asset);

      return asset;
   }

   public GameData.UnitType GetGameData(UnitTypeClass du) {
      var unit = new GameData.UnitType();

      unit.animation_class = du.AnimationType;
      unit.stats = new GameData.UnitStats();
      unit.name = du.name;
      unit.commander = du.Type.Equals("commander", StringComparison.OrdinalIgnoreCase);
      var st = unit.stats;
      st.hp = du.HP;
      st.gold_cost = du.Gold_Cost;
      st.resource_cost = du.Resources_Cost;
      st.move_speed = du.Combat_Speed;
      st.ma = du.M_Affinity;

      unit.armor = du.Armor;
      unit.shield = du.Shield;
      unit.helmet = du.Helmet;
      unit.primary_weapon = du.Weapon_Primary;
      unit.secondary_weapon = du.Weapon_Secondary;
      
      unit.innate_primary = du.Innate_Primary;
      unit.innate_secondary = du.Innate_Secondary;


      st.prot_body = unit.armor?.Protection_Body ?? 0;
      st.prot_head = unit.helmet?.Protection_Head ?? 0;
      st.prot_nat = du.Nat_Protection;

      st.shield_block = unit.shield?.Shield_Protection ?? 0;
      st.shield_parry = unit.shield?.Parry ?? 0;

      st.melee_damage = du.Strength;

      void AddW(WeaponMelee weapon) {
         if (weapon != null) {
            var str_bonus = weapon.GetStrengthBonus(du.Strength);

            if (weapon.IsRanged) {
               st.ranged_damage = weapon.Damage + str_bonus;

               var ap = weapon.Effects.Find(eff => eff.name == "ArmorPiercing");
               if (ap != null) {
                  st.ranged_flags = ArmorPiercing;
               }

               st.ranged_range = ParseStat(du, weapon.Range);
               // Debug.Log($"{du.name}: unit damage: {str_bonus} + {weapon.Damage}");
            } else {
               st.melee_damage = weapon.Damage + str_bonus;
               var ap = weapon.Effects.Find(eff => eff.name == "ArmorPiercing");
               if (ap != null) {
                  st.melee_flags = ArmorPiercing;
               }
            }
         }
      }

      AddW(unit.primary_weapon);
      AddW(unit.secondary_weapon);

      unit.magic_paths = du.Magic.Copy();

      return unit;
   }

   public GameTypeCollection() {
      game_units = new();
      shields = new();
      melee_weapons = new();
      armors = new();
      helmets = new();
      units = new();

      simple_buffs = new();
      simple_spells = new();
      magic_spells = new();
   }

   public void AddTo(EngineDataHolder.BaseTypes e) {
      e.game_units.AddRange(game_units);
      e.shields.AddRange(shields);
      e.weapon_melees.AddRange(melee_weapons);
      e.armors.AddRange(armors);
      e.helmets.AddRange(helmets);
      e.units.AddRange(units);

      e.simple_buff.AddRange(simple_buffs);
      e.simple_spells.AddRange(simple_spells);
      e.spells.AddRange(magic_spells);
      
      e.animation_data.AddRange(parsed_animations);
      
   }

   public GameTypeCollection(EngineDataHolder eng) {
      var e = eng.base_type_ref;
      game_units = new(e.game_units);
      shields = new(e.shields);
      melee_weapons = new(e.weapon_melees);
      armors = new(e.armors);
      helmets = new(e.helmets);
      units = new(e.units);

      simple_buffs = new(e.simple_buff);
      simple_spells = new(e.simple_spells);
      magic_spells = new(e.spells);
   }
}