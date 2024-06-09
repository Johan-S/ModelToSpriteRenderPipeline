using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using static DataTypes;
using ADict =
   System.Collections.Generic.Dictionary<(string animation_type, string category),
      GameTypeCollection.AnimationParsed>;

// [CreateAssetMenu(fileName = "GameEngineData", menuName = "EngineData", order = 0)]
public class EngineDataHolder : ScriptableObject {
   public void Awake() {
      if (name.IsNullEmpty()) name = "LocalData";
   }

   public Sprite dummy_sprite;
   public List<UnitTypeObject> unit_types;


   public Dictionary<string, UnitTypeObject> unit_dict;

   public int UnitIdOf(UnitTypeObject u) => unit_types.IndexOf(u);

   [Serializable]
   public class BaseTypes {


      public int dummy_field;
      [NonSerialized]
      public List<GameData.UnitType> game_units = new();

      [HideInInspector, SerializeReference]
      public List<WeaponMelee> weapon_melees = new();
   
      [HideInInspector, SerializeReference]
      public List<Armor> armors = new();
      [HideInInspector, SerializeReference]
      public List<Helmet> helmets = new();
      [HideInInspector, SerializeReference]
      public List<UnitTypeClass> units = new();
      [HideInInspector, SerializeReference]
      public List<Shield> shields = new();
      
      [HideInInspector, SerializeReference]
      public List<MagicSpell> spells = new();
      
      [HideInInspector, SerializeReference]
      public List<SimpleSpell> simple_spells = new();
      
      
      [HideInInspector, SerializeReference]
      public List<BattleData.UnitBuff> simple_buff = new();


      [HideInInspector] public List<GameTypeCollection.AnimationParsed> animation_data = new();

      [HideInInspector] public ADict adict;
      
      
      public Shared.UnitAnimationSprites GetAnimationSprites(string unit, string animation_class) {
      
      
         var acl = animation_data.Where(x => x.animation_type == animation_class).ToList();

      
         var maybe_cat = Sprites.GetUnitCats(unit);

         var animation_sprites = new Shared.UnitAnimationSprites();
         foreach (var (name, am) in animation_sprites.GetAllAnimations()) {
            var cl = acl.Find(x => x.category == name);
            if (cl == null) continue;
            var sprites = maybe_cat.sprites.Where(x => x.animation_category == name).ToArray();
            if (sprites.Length == 0) {
               continue;
            }




            am.sprites = sprites.map(x => x.sprite);
            am.time_ms = cl.time_ms.Copy();
            if (sprites.Length == 1) {
               am.time_ms = new []{am.time_ms.Sum()};
            } else {
               Debug.Assert(sprites.Length == cl.capture_frame.Length);
            }

            if (sprites.Length != am.time_ms.Length) {
               if (cl.auto_frames_per_s > 0) {
                  am.time_ms = sprites.map(x => 1000 / cl.auto_frames_per_s);
               } else {
                  Debug.LogError($"Wrong capture frame: {unit} - {name} : {sprites.Length} != {cl.capture_frame.Length}");
               }
            }

                  

            am.animation_duration_ms = am.time_ms.Sum();
         }

         return animation_sprites;
      }

   }

   public GameTypeCollection gear_data;


   public BaseTypes base_type_ref => base_types;

   public BaseTypes base_types = new();


   [CanBeNull]
   static HashSet<string> log_limit_cache;

   [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
   static void ReplaceLogCache() {
      log_limit_cache?.Clear();
   }

   static bool loglimit(string msg, out string s) {
      s = msg;
      return (log_limit_cache ??= new()).Add(msg);
   }

   static string ToPString(int[] s) {
      return "[" + s.join(",") + "]";
   }

   static Action<string, Shared.AnimationBundle> MakeBundleCallback(string atype, ADict adict, object debug_info) {
      return (cat, b) => {
         if (b.sprites.IsEmpty()) {
            return;
         }

         if (b.time_ms != null && b.time_ms.Length > 0 && b.animation_duration_ms != 0) {
            Debug.Log($"Double dip {b.time_ms.Length}");
            return;
         }

         if (adict.TryGetValue((atype, cat), out var adata)) {
            b.time_ms = adata.time_ms.ToArray();
            if (b.time_ms.Length != b.sprites.Length) {
               if (adata.time_ms.Length != adata.capture_frame.Length) {
                  if (loglimit(
                         $"Missmatch animation length for {(atype, cat)}: {b.time_ms.Length} != {b.sprites.Length}",
                         out var msg))
                     Debug.LogError(msg);
               } else {
                  if (loglimit(
                         $"Missmatch animation length for {debug_info}: {(atype, cat)} {b.time_ms.Length} != {b.sprites.Length}",
                         out var msg))
                     Debug.LogError(msg);
               }

               if (b.time_ms.Length > b.sprites.Length) b.time_ms = b.time_ms[..b.sprites.Length];
               else b.sprites = b.sprites[..b.time_ms.Length];
            }

            b.animation_duration_ms = b.time_ms.Sum();
         } else {
            // Debug.Log($"No bundle for {atype}:  {cat}");

            UnitTypeObject.FillBundleMs(b);
         }
      };
   }
   


   public void FillanimationsFor(UnitTypeObject u) {
      
      u.animations = base_type_ref.GetAnimationSprites(u.sprite_gen_name, u.animation_class);

      u.sprite_ref = Sprites.GetUnitCats(u.sprite_gen_name).idle_sprite;

      var adict = base_type_ref.adict;

      {
         
         foreach (var an in u.AllAnimations()) {
            if (an) {
               if (an.sprites.Exists(x => !x)) {
                  an.sprites = null;
                  Debug.LogError($"Reloading sprite for {u.name}");
               }
            }
         }

         var atype = u.animation_class ?? "";
         if (!adict.ContainsKey((atype, "Idle"))) {
            if (atype == "Poleaxe") atype = "Spearman";
            if (!adict.ContainsKey((atype, "Idle"))) {
               atype = base_type_ref.animation_data.FirstOrDefault()?.animation_type ?? "";
            }
         }


         Action<string, Shared.AnimationBundle> bundle_callback;


         bundle_callback = MakeBundleCallback(atype, adict, debug_info: (u.name, u.animation_class));

         if (u.sprite_gen_name.IsEmpty()) {
            u.FillGeneratedSprites((x, b) => UnitTypeObject.FillBundleMs(b));
         } else {
            u.FillGeneratedSprites(bundle_callback);
         }
      }
   }

   public void InitDataForPlay() {
      Debug.Assert(base_type_ref.game_units.IsEmpty());
      unit_dict = new();

      base_type_ref.units = new List<UnitTypeClass>();

      gear_data = new GameTypeCollection(this);


      foreach (var ut in unit_types) {
         if (unit_dict.ContainsKey(ut.name)) {
            // Debug.Log($"Dupliacte unit {ut.name}");
            continue;
         }

         unit_dict[ut.name] = ut;
      }

      int replaced = 0;

      string miss_name = null;

      void ReplaceMissing(Sprite[] s) {
         for (int i = 0; i < s.Length; i++) {
            if (!s[i]) {
               s[i] = dummy_sprite;
               replaced++;
            }
         }
      }

      foreach (var a in ExportPipeline.GetDirectAnimationsParsed()) {
         base_type_ref.animation_data.Add(a);
         // Debug.Log($"{a.animation_type}, {a.category}");
      }

      foreach (var a in base_type_ref.animation_data) {
         if (a.time_ms.IsEmpty()) {
            Debug.Log($"Missing animation time for :{a}");
            continue;
         }

         if (a.capture_frame.Length != a.time_ms.Length) {
            Debug.Log(
               $"Animation frame missmatch for :{a} {a.clip}. {a.capture_frame.Length} != {a.time_ms.Length}, {ToPString(a.capture_frame)} & {ToPString(a.time_ms)}");
         }
      }


      base_type_ref.adict = base_type_ref.animation_data.ToDictionary(x => (x.animation_type, x.category), x => x);


      foreach (var u in unit_types) {
         FillanimationsFor(u);
         

         if (!u.sprite_ref) {
            u.sprite_ref = dummy_sprite;
            replaced++;
         }

         foreach (var a in u.AllAnimations().Where(x => x != null)) {
            if (a.sprites == null) a.sprites = new Sprite[0];
            ReplaceMissing(a.sprites);
         }

         if (replaced > 0) miss_name ??= u.name;
      }
      foreach (var ut in unit_types) {
         var gt = ut.game_data_impl = GameData.ParseUnit(ut, gear_data.units.Get(ut.name));

         base_types.game_units.Add(gt);
         gear_data.game_units.Add(gt);
      }

      if (unit_types.Count != gear_data.game_units.Count) {
         Debug.Log($"{unit_types.Count}, {gear_data.game_units.Count}");
      }

      if (replaced > 0) {
         Debug.LogError($"Missing {replaced} sprites with dummies in unit objects! {miss_name}");
      }
   }
}