using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationTypeObject : ScriptableObject {
   public string animation_type;
   public string category;
   public string clip;


   public int auto_frames_per_s = 0;

   public int[] capture_frame;
   public int[] time_ms;


   public bool looping_root;
   
   
   public Vector3 model_offsetmodel_offset;
}