using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SimpleUnityTypeContainer : ScriptableObject {

   static SimpleUnityTypeContainer cache;

   [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
   static void ResetCache() {
      cache = null;
   }
   
   public static SimpleUnityTypeContainer GetFull() {
      if (!cache) {

         var tot = ScriptableObject.CreateInstance<SimpleUnityTypeContainer>();


         foreach (var c in Resources.LoadAll<SimpleUnityTypeContainer>("")) {
            tot.armor.AddRange(c.armor);
            tot.shield.AddRange(c.shield);
            tot.weapon.AddRange(c.weapon);
            tot.helmet.AddRange(c.helmet);
            tot.animation.AddRange(c.animation);
            tot.simpleSpell.AddRange(c.simpleSpell);
            tot.spell.AddRange(c.spell);
            tot.simpleBuff.AddRange(c.simpleBuff);
            tot.simpleUnit.AddRange(c.simpleUnit);
         }
         tot.name = "Full_SimpleUnityTypeContainer";
         cache = tot;
      }

      return cache;
   }
   
   [HideInInspector]
   public List<ArmorTypeObject> armor = new();
   [HideInInspector]
   public List<ShieldTypeObject> shield = new();
   [HideInInspector]
   public List<WeaponTypeObject> weapon = new();
   [HideInInspector]
   public List<HelmetTypeObject> helmet = new();
   [HideInInspector]
   public List<AnimationTypeObject> animation = new();
   [HideInInspector]
   public List<SpellTypeObject> spell = new();
   [HideInInspector]
   public List<SimpleSpellTypeObject> simpleSpell = new();
   [HideInInspector]
   public List<SimpleBuffTypeObject> simpleBuff = new();
   [HideInInspector]
   public List<SimpleUnitTypeObject> simpleUnit = new();
}