using System;
using System.Collections;
using System.Collections.Generic;
using Shared;
using UnityEngine;
[SelectionBase]
public class ModelBodyRoot : MonoBehaviour {

   [Serializable]
   public class SlotVariant {
      [Tooltip("Matched against AnimationTypeObject.equipment_variant.")]
      public string variant;

      public AnimationTypeObject.EquipmentSlot slot;

      [Tooltip("Shown instead of the slot's default contents. Can sit under the slot or anywhere on the model, e.g. a back slot.")]
      public GameObject[] objects;
   }


   [Header("Slots")]
   
   public Transform Mouth;
   public Transform Main_Hand;
   public Transform Off_Hand_Shield;
   public Transform Off_Hand;
   public Transform NewHelmet;
   public Transform Tail;


   public Transform[] mounting_points;
   

   [Header("Renderers")]
   public Renderer[] renderers;

   [Header("Equipment Variants")]
   public SlotVariant[] slot_variants;

   public Transform GetSlot(AnimationTypeObject.EquipmentSlot slot) {
      switch (slot) {
         case AnimationTypeObject.EquipmentSlot.Main_Hand: return Main_Hand;
         case AnimationTypeObject.EquipmentSlot.Off_Hand: return Off_Hand;
         case AnimationTypeObject.EquipmentSlot.Off_Hand_Shield: return Off_Hand_Shield;
         case AnimationTypeObject.EquipmentSlot.Helmet: return NewHelmet;
         default: return null;
      }
   }

}