using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using static BattleData;

using UnityEditor;

// [CreateAssetMenu(fileName = "NewUnitType", menuName = "Unit Type", order = 0)]
public class UnitTypeObject : ScriptableObject {
   [HideInInspector] public bool generated_from_sheets;

   [NonSerialized] public GameData.UnitType game_data_impl;


   static Dictionary<object, UnitTypeObject> load_cache = new();

   [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
   static void Onload() {
      load_cache.Clear();
   }


   public UnitTypeObject Normalized {
      get {
         if (!this) {
            Debug.LogError($"Trying to fetch on destroyed UnitTypeObject object!");
            return null;
         }

         if (!load_cache.TryGetValue(this, out var norm_cache)) {
            if (!EngineDataInit.engine_data.unit_dict.TryGetValue(name, out norm_cache)) {
               norm_cache = this;

               EngineDataInit.engine_data.FillanimationsFor(this);
               game_data_impl = null;
            }

            load_cache[this] = norm_cache;
         }

         return norm_cache;
      }
   }

   public GameData.UnitType GetGameData {
      get {
         var norm =
            Normalized;
         if (norm != this) {
            // Debug.Log($"Skewed import for: {norm.name}");
         }

         return norm.pGetGameData;
      }
   }

   GameData.UnitType pGetGameData =>
      game_data_impl ??= EngineDataInit.gear_data.game_units.Get(name) ?? GameData.ParseUnit(this, null);

   public float animation_scale = 1;

   public bool commander;

   public GameData.UnitStats stats;
   [Header("Magic")] public DataTypes.MagicPath[] magic_paths;


   [Header("Gear")] [SerializeReference] public DataTypes.Armor armor;
   [SerializeReference] public DataTypes.Shield shield;
   [SerializeReference] public DataTypes.Helmet helmet;
   [SerializeReference] public DataTypes.WeaponMelee primary_weapon;
   [SerializeReference] public DataTypes.WeaponMelee secondary_weapon;

   [SerializeReference] public DataTypes.WeaponMelee innate_primary;
   [SerializeReference] public DataTypes.WeaponMelee innate_secondary;

   public Sprite icon_sprite;
   public Sprite sprite {
      get {
         if (!sprite_ref) {
            FillGeneratedSprites((x, b) => FillBundleMs(b));
         }

         return sprite_ref;
      }
   }

   [Header("Animations"), FormerlySerializedAs("sprite")]
   public Sprite sprite_ref;


   public GameData.UnitAnimationSprites animations;
   
   public string sprite_gen_name;

   public static void FillBundleMs(GameData.AnimationBundle b) {
      if (b.sprites.IsEmpty()) {
         Debug.Log($"Trying to fill empty bundle!");
         return;
      }

      if (b.time_ms.IsEmpty()) {
         throw new Exception($"Bad bundle, should always have MS!");
      } else {
         Debug.Assert(b.sprites.Length == b.time_ms.Length);
      }

   }


   int cur_load_id;

   public void FillGeneratedSprites(Action<string, GameData.AnimationBundle> bundle_callback) {
      // if (cur_load_id == Std.LoadId) return;
      cur_load_id = Std.LoadId;



      if (animations == null) {
         Debug.LogError($"Missing animations!: {sprite_gen_name}");
      }


      // throw new Exception($"Don't load sprites here!");
   }

   public IEnumerable<GameData.AnimationBundle> AllAnimations() {

      foreach (var (n, a) in animations.GetAllAnimations()) {
         yield return a;
      }
   }


   public string animation_class;
   
   
   #if UNITY_EDITOR

   [CustomEditor(typeof(UnitTypeObject))]
   public class CustomLocalEditor : Editor {
      public override void OnInspectorGUI() {
         var t = (UnitTypeObject)target;
         if (!t.icon_sprite) {
            t.icon_sprite = GeneratedSpritesContainer.GetStableIcon(t.sprite_gen_name);
            
         }
         if (t.icon_sprite) {
            GUIContent label = new(t.name, AssetPreview.GetAssetPreview(t.icon_sprite));
            EditorGUILayout.LabelField(label);
         }
         base.OnInspectorGUI();
      }
   }
   #endif
}