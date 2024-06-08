using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


using UnityEditor;

public class SimpleUnitTypeObject : ScriptableObject, Named, IUnitTypeForRender {
   

   public string ExportName => Unit_Name;
   public ModelBodyCategory ModelBody => null;
   public Material MaterialOverride { get; }
   public string ModelBodyName => Anatomy;
   
   string IUnitTypeForRender.AnimationType => AnimationType;
   
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
   public string Weapon_Primary;
   public string Weapon_Secondary;
   public string Quiver_Slot;
   public string Innate_Primary;
   public string Innate_Secondary;
   public string Shield;
   public string Helmet;
   public string Armor;
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
   public DataTypes.GenericEffectList Abilities;
   public DataTypes.MagicPathList Magic;
   public int Leadership;

   
   [Header("SUB VALS BELOW")]

   [HideInInspector] public AttachedVars vars;
   public Color tint_color = Color.white;

   public int formation_size = 6;

   public float attack_haste;


   [Serializable]
   public class AttachedVars {
      public Sprite icon;
   }


   [NonSerialized] public Texture2D gen_icon;

   public Sprite icon {
      get {
         vars ??= new();

         if (!vars.icon) {
            if (Unit_Name.FastStartsWith("r/")) {
               vars.icon = serialized_icon;
            } else {
               var gen_name = DataParsing.GetExportUnitName(Unit_Name);
               vars.icon = GeneratedSpritesContainer.GetStableIcon(gen_name);
            }

         }

         return vars.icon;
      }
   }

   public Sprite serialized_icon;


#if UNITY_EDITOR

   [CustomEditor(typeof(SimpleUnitTypeObject))]
   public class CustomLocalEditor : Editor {
      void OnEnable() {
         var t = (SimpleUnitTypeObject)target;

         if (t.Unit_Name.StartsWith("r/")) {

            icon_sprite = t.serialized_icon;
         } else {
            
            var gen_name = DataParsing.GetExportUnitName(t.name);

            icon_sprite = GeneratedSpritesContainer.GetStableIcon(gen_name);
         }

      }


      public Sprite icon_sprite;
      

      public override void OnInspectorGUI() {

         var t = (SimpleUnitTypeObject)target;
         if (GUILayout.Button("Copy Csv To Clipboard")) {
            var fields = Std.GetFields(t);

            var i = fields.FindIndex(x => x.name == "vars");

            fields = fields.SubList(0, i);

            var data = fields.map(x => $"{x.val}");
            var row = data.join("\t");
            
            Debug.Log($"Copied csv row for {t.name}:\n{row}");
            Debug.Log($"AS_ROWS:\n{data.join("\n")}");

            GUIUtility.systemCopyBuffer = row;
         }
         if (target && t.icon) {
            GUIContent label = new(t.name, AssetPreview.GetAssetPreview(t.icon));
            EditorGUILayout.LabelField(label);
         }

         base.OnInspectorGUI();
      }
   }
#endif
}