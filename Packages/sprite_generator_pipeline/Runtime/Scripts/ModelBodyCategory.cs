﻿using UnityEngine;

[CreateAssetMenu(fileName = "New ModelBody", menuName = "Pipeline Model Body", order = 0)]
public class ModelBodyCategory : ScriptableObject {

   public Animator model_root_prefab;


   public Material material;

   public string bodyTypeName;

   public string animationTypeName;

   public bool no_gear;

   [Tooltip("Is in local model space, so Z is forward backward for the model.")]
   public Vector3 model_offset;


   public float relative_model_height_for_shading;

   public bool mirror_render;
}