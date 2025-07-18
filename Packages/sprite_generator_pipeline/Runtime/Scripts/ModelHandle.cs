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

      if (render_obj) {
         Destroy(render_obj);
         render_obj.SetActive(false);
      }

      model_root = null;
      mesh_prepper.Clear();
      var m = Instantiate(model, transform);

      mesh_prepper.StoreOOriginalPosForModel(m.transform);

      model_action?.Invoke(m);

      Init();
      return m;
   }

   public MeshPrepper mesh_prepper = new();

   public bool isSet => model_root;
   static readonly ProfilerMarker _m_SampleAnimation = Std.Profiler<ModelHandle>("SampleAnimation");

   public class MeshPrepper {
      public void Clear() {
         foreach (var stored_meshes in stored_model_mesh) {
            Std.DestroyNow(stored_meshes.Value.changed_mesh);
         }

         stored_model_mesh.Clear();
         stored_for = null;
         retreived = false;
      }

      Mesh PrepMeshWithOriginalPos(Transform tr, Mesh mesh) {
         if (mesh) {
            if (stored_model_mesh.TryGetValue(tr, out var meshes)) {
               if (meshes.changed_mesh == mesh || meshes.old_mesh == mesh) return meshes.changed_mesh;
            }

            if (!mesh.isReadable) {
               Debug.Log($"Found unreadable mesh: {mesh.name} in object {tr.name}!", tr.gameObject);
               return mesh;
            }

            if (mesh.name.Contains("(PREPPED ORIG POS)")) return mesh;
            if (mesh.uv5.IsNonEmpty()) {
               Debug.LogError($"Mesh {mesh.name} already contains uv5 in object {tr.name}!!", tr.gameObject);
               return mesh;
            }

            if (mesh.uv6.IsNonEmpty()) {
               Debug.LogError($"Mesh {mesh.name} already contains uv6 in object {tr.name}!!", tr.gameObject);
               return mesh;
            }

            var nm = Instantiate(mesh);
            nm.name = mesh.name + " (PREPPED ORIG POS)";
            var mat = tr.localToWorldMatrix;
            var bw = nm.vertices.map(x => (Vector3)(mat * new Vector4(x.x, x.y, x.z, 1)) * (1f / 1));
            nm.uv5 = bw.map(x => new Vector2(x.x, x.y) * (1f / 1));
            nm.uv6 = bw.map(x => new Vector2(x.z, 0) * (1f / 1));

            stored_model_mesh[tr] = (mesh, nm);
            return nm;
         }

         return mesh;
      }

      Dictionary<Transform, (Mesh old_mesh, Mesh changed_mesh)> stored_model_mesh = new();

      public void RetreiveOriginalMeshes(Transform tr) {
         if (tr != stored_for) {
            Debug.Log($"Trying to retreive for wrong mesh {tr.name}, had {stored_for?.name ?? "NULL"}");
            return;
         }

         foreach (var sk in tr.GetComponentsInChildren<Renderer>()) {
            if (!stored_model_mesh.TryGetValue(sk.transform, out var meshes)) continue;
            if (sk is SkinnedMeshRenderer am) {
               if (am.sharedMesh == meshes.changed_mesh) {
                  am.sharedMesh = meshes.old_mesh;
               }
            }

            if (sk is MeshRenderer mr) {
               var mf = sk.GetComponent<MeshFilter>();
               if (mf.sharedMesh == meshes.changed_mesh) {
                  mf.sharedMesh = meshes.old_mesh;
               }
            }
         }

         retreived = true;
      }

      public Transform stored_for;

      public bool retreived;

      public void StoreOOriginalPosForModel(Transform tr) {
         if (tr == stored_for && !retreived && ExportPipeline.exporting) {
            return;
         }

         if (tr != stored_for) {
            Clear();
         }

         stored_for = tr;

         foreach (var sk in tr.GetComponentsInChildren<Renderer>()) {
            if (sk is SkinnedMeshRenderer am) {
               var m = PrepMeshWithOriginalPos(am.transform, am.sharedMesh);
               if (m) am.sharedMesh = m;
            }

            if (sk is MeshRenderer mr) {
               var mf = mr.GetComponent<MeshFilter>();
               var m = PrepMeshWithOriginalPos(mr.transform, mf.sharedMesh);
               if (m) mf.sharedMesh = m;
            }
         }

         retreived = false;
      }
   }

   void SampleAnimation(GameObject go, float time, AnimationClip clip) {
      using var _m = _m_SampleAnimation.Auto();

      mesh_prepper.StoreOOriginalPosForModel(go.transform);

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

   [NonSerialized]
   public Animator model_root;

   public bool negate_root_motion;

   public Vector3 last_root_motion;
   public Quaternion last_root_rotation;


   public GameObject render_obj => model_root ? model_root.gameObject : null;

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
      if (model_root) {
         if (!ExportPipeline.exporting) {
            mesh_prepper.RetreiveOriginalMeshes(model_root.transform);
         }
      }
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

         FetchLocalModel();
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

   void FetchLocalModel() {
      if (!model_root) model_root = GetComponentInChildren<Animator>();
      if (model_root && model_root.enabled) {
         Debug.LogError($"Animator should not be enabled! Disable animatior on: {model_root.name}", model_root);
         model_root.enabled = false;
      }
   }

   void Awake() {
      FetchLocalModel();
   }

   // Update is called once per frame
   void Update() {
      if (ExportPipeline.exporting) return;
      if (!model_root) Init();
      if (!model_root) return;
      HandleAnimation();

      animation_frame = Mathf.RoundToInt(animation_time * 60);
   }
}