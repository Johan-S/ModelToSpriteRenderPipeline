using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Serialization;

[DefaultExecutionOrder(5)]
[SelectionBase]
public class ModelHandle : MonoBehaviour {
   public GameObject ResetModel(GameObject model, Action<GameObject> model_action) {
      if (!model) {
         Debug.LogError("Trying to set missing model!");
         return null;
      }

      Init();
      foreach (var x in sub_render_obj) {
         Destroy(x);
         x.SetActive(false);
      }

      sub_render_obj = null;
      var m = Instantiate(model, transform);

      model_action?.Invoke(m);

      Init();
      return m;
   }
   public void SetAnimationNow_Float(AnimationClip clip, float x) {
      SetActiveModel(-1);

      foreach (var o in sub_render_obj) {
         // o.GetComponent<Animator>().enabled = false;
         clip.SampleAnimation(o, x * clip.length);
         o.transform.localPosition += model_offset;
      }

      SetActiveModel(0);
   }

   public void SetAnimationNow(AnimationClip clip, int frame) {
      SetActiveModel(-1);

      foreach (var o in sub_render_obj) {
         // o.GetComponent<Animator>().enabled = false;
         clip.SampleAnimation(o, frame * (1 / 60f));
         o.transform.localPosition += model_offset;
      }

      SetActiveModel(0);
   }

   [Header("Update Preview Frame")] public Vector3 model_offset;
   
   [Range(0, 1)] public float animation_t = 0;
   public AnimationClip animation_clip;

   [Header("Animation Settings")]
   

   [Range(0, 2)] public float animation_speed = 1;


   [FormerlySerializedAs("animation_frame")] [Header("Output")]
   public float animation_time = 0;

   public int animation_frame = 0;

   [Header("Render types")] public Material raw_color_material;
   public Material forward_depth_material;
   public Material back_depth_material;

   Animator model_root;

   GameObject model_game_object => model_root.gameObject;

   [NonSerialized]
   public List<GameObject> sub_render_obj = new();

   public void SetActiveModel(int id) {
      Init();
      for (int i = 0; i < sub_render_obj.Count; i++) {
         sub_render_obj[i].SetActive(i == id);
      }
   }


   void InitRenderObjs() {
      Material Make(int n) {
         var m = Instantiate(raw_color_material);
         m.color = new Color32(255, 255, (byte)n, 255);
         return m;
      }

      {
         var go = Instantiate(model_game_object, model_game_object.transform.parent);

         sub_render_obj.Add(go);

         int i = 0;
         foreach (var x in go.GetComponentsInChildren<Renderer>()) {
            x.material = Make(i++);
         }
      }
      {
         var go = Instantiate(model_game_object, model_game_object.transform.parent);


         sub_render_obj.Add(go);

         foreach (var x in go.GetComponentsInChildren<Renderer>()) {
            x.material = forward_depth_material;
         }
      }
      {
         var go = Instantiate(model_game_object, model_game_object.transform.parent);


         sub_render_obj.Add(go);

         foreach (var x in go.GetComponentsInChildren<Renderer>()) {
            x.material = back_depth_material;
         }
      }
   }

   void Init() {
      if (sub_render_obj != null && sub_render_obj.Count > 0 && model_root) {
         return;
      }

      sub_render_obj = new();
      model_root = GetComponentInChildren<Animator>();

      sub_render_obj.Add(model_root.gameObject);
      try {
         InitRenderObjs();
      }
      finally {
         SetActiveModel(-1);
      }
   }


   // Start is called before the first frame update
   void Start() {
      Init();
   }

   void OnEnable() {
      Application.onBeforeRender += Beforerender;
   }

   void OnDisable() {
      Application.onBeforeRender -= Beforerender;
   }

   void Beforerender() {
      SetActiveModel(0);
   }

   void LateUpdate() {
   }

   public void UpdateModelAnimationPos() {
      if (animation_clip != null) {
         if (animation_clip.hasGenericRootTransform) {
            Debug.Log($"Root motion: {animation_clip.name}");
         }
         if (!model_root) model_root = GetComponentInChildren<Animator>();
         if (model_root) {

            animation_t += animation_speed / animation_clip.length * 0.01f;
            if (animation_t > 1) animation_t -= 1;
            animation_time = animation_t * animation_clip.length;
            
            
            animation_clip.SampleAnimation(model_root.gameObject, animation_time);
            // model_root.transform.localPosition = model_offset;
            animation_frame = Mathf.RoundToInt(animation_time * 60);
         }
      }
   }

   void FixedUpdate() {
      if (animation_clip != null) {
         if (animation_time > animation_clip.length) {
            animation_time = 0;
         }

         foreach (var o in sub_render_obj) {
            // o.GetComponent<Animator>().enabled = false;
            animation_clip.SampleAnimation(o, animation_time);
            // model_root.transform.localPosition = model_offset;
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

         foreach (var o in sub_render_obj) {
            bool a = o.activeSelf;
            o.SetActive(false);
            animation_clip.SampleAnimation(o, animation_time);
            o.transform.localPosition += model_offset;
            o.SetActive(a);
         }
      }
   }

   // Update is called once per frame
   void Update() {
      HandleAnimation();

      animation_frame = Mathf.RoundToInt(animation_time * 60);
   }
}