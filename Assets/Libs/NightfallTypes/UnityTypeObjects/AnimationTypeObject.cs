using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationTypeObject : ScriptableObject {
   public string animation_type;
   public string category;
   public AnimationClip clip_ref;
   public string clip_name => clip_ref ? clip_ref.name : clip;


   [Header("Capture Data")]
   public int auto_frames_per_s = 0;

   public int[] capture_frame;
   public int[] time_ms;


   public bool looping_root;
   
   
   public Vector3 model_offsetmodel_offset;
   
   public Vector3 model_root_pos;
   public Quaternion model_root_rot;
   
   [Header("Old clip ref")]

   public string clip;
}