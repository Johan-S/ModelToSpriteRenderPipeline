using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Serialization;

[DefaultExecutionOrder(5)]
public class ModelHandle : MonoBehaviour {
   public GameObject ResetModel(GameObject model, Action<GameObject> model_action) {
      if (!model) {
         Debug.LogError("Trying to set missing model!");
         return null;
      }

      Init();

      Destroy(render_obj);
      render_obj.SetActive(false);
      model_root = null;
      var m = Instantiate(model, transform);

      model_action?.Invoke(m);

      Init();
      return m;
   }

   public bool isSet => model_root;
   static readonly ProfilerMarker _m_SampleAnimation = Std.Profiler<ModelHandle>("SampleAnimation");

   void SampleAnimation(GameObject go, float time, AnimationClip clip) {
      using var _m = _m_SampleAnimation.Auto();

      var animator = model_root;
      if (!animator) return;
      var last_pos = go.transform.localPosition;
      var last_rot = go.transform.localRotation;
      
      if (TRY_ANIMATOR) {
         var controller = animator.runtimeAnimatorController;
         if (controller) {
            
            // Debug.Log($"an name: {controller.name}");
            if (controller.animationClips.Exists(cl => cl.name == clip.name)) {
         
               // animator.StartPlayback();
               var old = 
                  animator.enabled;
               animator.enabled = true;
               animator.Play(clip.NormalizedName(), -1, time / clip.length);
               animator.Update(1);
               animator.enabled = false;
               // animator.enabled = false;
            
               return;
            } else {
               Debug.Log($"Missing animation: {clip.name}");
            }
         }

      }
      go.SetActive(false);
      clip.SampleAnimation(go, time);
      go.SetActive(true);
      last_root_motion = go.transform.localPosition;
      last_root_rotation = go.transform.localRotation;
      if (negate_root_motion) {
         go.transform.localPosition = last_pos;
         go.transform.localRotation = last_rot;
      }
      
   }

   public bool TRY_ANIMATOR;

   public void SetAnimationNow_Float(AnimationClip clip, float x) {
      {
         // o.GetComponent<Animator>().enabled = false;
         SampleAnimation(render_obj, x * clip.length, clip);
         render_obj.transform.localPosition += model_offset;
      }
   }

   public void SetAnimationNow(AnimationClip clip, int frame) {
      SampleAnimation(render_obj, frame * (1 / 60f), clip);
      render_obj.transform.localPosition += model_offset;
   }

   [Header("Update Preview Frame")] public Vector3 model_offset;

   [Range(0, 1)] public float animation_t = 0;
   public AnimationClip animation_clip;

   [Header("Animation Settings")] [Range(0, 2)]
   public float animation_speed = 1;


   [FormerlySerializedAs("animation_frame")] [Header("Output")]
   public float animation_time = 0;

   public int animation_frame = 0;

   [Header("Render types")] public Material raw_color_material;
   public Material forward_depth_material;
   public Material back_depth_material;
[NonSerialized]
   public Animator model_root;

   public bool negate_root_motion;

   public Vector3 last_root_motion;
   public Quaternion last_root_rotation;


   public GameObject render_obj => model_root.gameObject;

   public void SetActiveModel(bool a) {
      Init();
      render_obj.SetActive(a);
   }

   void Init() {
      if (model_root) {
         return;
      }

      model_root = GetComponentInChildren<Animator>();
      if (model_root) model_root.speed = 0;
   }


   // Start is called before the first frame update
   void Start() {
      Init();
   }

   void OnEnable() {
      if (model_root) model_root.StartPlayback();
   }

   void LateUpdate() {
   }

   // [Button]
   public void UpdateModelWithAnimationTime() {
      UpdateModelAnimationPos(0);
   }

   public void UpdateModelAnimationPos(float dt) {
      if (animation_clip != null) {
         if (animation_clip.hasGenericRootTransform) {
            Debug.Log($"Root motion: {animation_clip.name}");
         }

         if (!model_root) model_root = GetComponentInChildren<Animator>();
         if (model_root) {
            animation_t += animation_speed / animation_clip.length * dt;
            if (animation_t > 1) animation_t -= 1;
            animation_time = animation_t * animation_clip.length;
            SampleAnimation(model_root.gameObject, animation_time, animation_clip);
            // model_root.transform.localPosition = model_offset;
            animation_frame = Mathf.RoundToInt(animation_time * 60);
         }
      }
   }

   void HandleAnimation() {
      if (animation_clip != null) {
         if (animation_clip.length == 0) {
            animation_time = 0;
            animation_t = 0;
         } else {
            animation_t += animation_speed * Time.deltaTime / animation_clip.length;
            if (animation_t > 1) animation_t -= 1;
            if (animation_t > 1) animation_t = 0;

            animation_time = animation_t * animation_clip.length;
         }

         SampleAnimation(render_obj, animation_time, animation_clip);
      }
   }

   // Update is called once per frame
   void Update() {
      if (!model_root) Init();
      if (!model_root) return;
      HandleAnimation();

      animation_frame = Mathf.RoundToInt(animation_time * 60);
   }
}