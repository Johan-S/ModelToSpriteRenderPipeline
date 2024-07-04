using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shared;
using TMPro;
using static ExportPipeline;

[DefaultExecutionOrder(-150)]
public class AnimationManager : MonoBehaviour {
   static AnimationManager instance;


   [RuntimeInitializeOnLoadMethod]
   static void ClearInstance() {
      instance = null;
   }

   void OnEnable() {
      if (instance) {
         Debug.LogError($"Duplicate {this.GetType().Name}!");
         Destroy(gameObject);
         return;
      }

      instance = this;
   }

   public AnimationSet GetAnimationSet(string animation_type) {
      if (animation_sets == null) {
         Init();
      }

      return animation_sets.Get(animation_type);
   }


   public void Init() {
      if (sheets_pipeline_descriptor && sheets_pipeline_descriptor.animation_arr == null) {
         sheets_pipeline_descriptor.InitData();
      }

      animation_sets = GetanimationSets();
      foreach (var ap in SharedUtils.ANIMATION_SUBSTITUTE) {
         if (!animation_sets.ContainsKey(ap.Key)) {
            animation_sets[ap.Key] = animation_sets[ap.Value];
            // Debug.Log($"Missing animation Poleaxe");
         }
      }


      if (!animation_sets.ContainsKey("Crossbow")) {
         // animation_sets["Crossbow"] = animation_sets["Brute"];
         // Debug.Log($"Missing animation Crossbow");
      }
   }

   Dictionary<string, AnimationSet> animation_sets;

   public AnimationTypeObject[] override_animations;
   public AnimationBundle[] animation_bundles;

   public ExportPipelineSheets sheets_pipeline_descriptor;

   AnimationClip GetAnimationClip(string animation_name) {
      AnimationClip clip = null;

      foreach (var a in animation_bundles) {
         clip = a.animation_clips.Find(x => x.name == animation_name);
         if (clip) break;
      }

      return clip;
   }

   AnimationClip GetAnimationClip(AnimationTypeObject animation) {
      if (animation.clip_ref) return animation.clip_ref;
      return GetAnimationClip(animation.clip);
   }

   public IEnumerable<AnimationSet> GetDirectAnimationSets() {
      var override_anims = override_animations;
      var anim_objs = Resources.LoadAll<AnimationTypeObject>("");

      var all_keys = override_anims.Concat(anim_objs).Select(x => x.animation_type.Replace("&", "_")).Distinct();
      var ogroups = override_anims.ToKeyDict(x => x.animation_type.Replace("&", "_"));
      var groups = anim_objs.ToKeyDict(x => x.animation_type.Replace("&", "_"));


      foreach (var gk in all_keys) {
         var p = new AnimationSet();

         var g = ogroups.Get(gk, new());
         var override_cats = g.ToList().Select(x => x.category).ToHashSet();
         var orig_g = groups.Get(gk, new()).Where(x => !override_cats.Contains(x.category));

         p.name = gk;


         foreach (var data in g.Concat(orig_g)) {
            AnimationClip clip = GetAnimationClip(data);
            if (clip == null) {
               Debug.LogError($"Missing clip: {data.clip_name} for animation {data.name}");
               continue;
            }

            if (data.auto_frames_per_s > 0 && data.capture_frame.IsEmpty()) {
               GenerateCaptureFrames(data, clip, p);
            } else {
               int tot_dur = 0;
               foreach (var (fr, dur) in data.capture_frame.Zip(data.time_ms)) {
                  tot_dur += dur;
                  p.res.Add(new(data.clip, clip, data.category, fr, null) {
                     time_ms = tot_dur,
                  });
               }
            }
         }


         // Debug.Log($"an: {p.name}:\n\t{p.res.join("\n\t")}");


         // Debug.Log($"Group {g.Key}: {g.ToList().join(", ", x => x.category)}");
         yield return p;
      }
   }

   public static void GenerateCaptureFrames(AnimationTypeObject data, AnimationClip clip, AnimationSet p) {
      var len = clip.length;

      var frames = Mathf.CeilToInt(len * data.auto_frames_per_s);

      if (frames < 2) frames = 2;

      int tot_dur = 0;
      for (int i = 0; i < frames; i++) {
         float time = i * len / (frames - 1);
         tot_dur += (1000 / data.auto_frames_per_s).Round();

         p.res.Add(new(data.clip_name, clip, data.category, Mathf.FloorToInt(time * 60), data) {
            time_ms = tot_dur,
         });
      }
   }

   Dictionary<string, AnimationSet> GetanimationSets() {
      Dictionary<string, AnimationSet> res = new();

      if (sheets_pipeline_descriptor) {
         var arr = sheets_pipeline_descriptor.animation_arr;
         var types = arr.Select(x => x.animation_type).Distinct();
         var cats = arr.Select(x => x.category).Distinct();

         foreach (var anim in types) {
            var p = new AnimationSet {
               name = anim.Replace("&", "_"),
            };

            foreach (var c in cats) {
               if (sheets_pipeline_descriptor.animation_good.TryGetValue((anim, c), out var data)) {
                  AnimationClip clip = GetAnimationClip(data.clip);
                  if (clip == null) {
                     Debug.LogError($"Missing clip: {data.clip} for PARSED animation {data.animation_type}");
                     continue;
                  }

                  int tot_dur = 0;
                  foreach (var (fr, dur) in data.capture_frame.Zip(data.time_ms)) {
                     tot_dur += dur;
                     p.res.Add(new(data.clip, clip, data.category, fr, null) {
                        time_ms = tot_dur,
                     });
                  }
               }
            }

            res[p.name] = p;
         }
      }


      var direct_anims = GetDirectAnimationSets().ToList();

      foreach (var p in direct_anims) {
         if (res.TryGetValue(p.name, out var cur)) {
            cur.res.AddRange(p.res);
         } else {
            res[p.name] = p;
         }
      }

      return res;
   }

   public IEnumerable<Shared.AnimationParsed> GetDirectAnimationsParsed() {

      var clips = animation_bundles.FlatMap(x => x.animation_clips).ToLookupListObj();

      var anim_objs = Resources.LoadAll<AnimationTypeObject>("");

      var groups = anim_objs.GroupBy(x => SharedUtils.NormalizeAnimationName(x.animation_type));

      bool TryFixMissingMs(AnimationTypeObject data, AnimationParsed ap) {
         var clip = data.clip_ref;
         if (!clip) {
            clip = clips[data.clip_name];
         }
         
         
         if (data.auto_frames_per_s > 0 && data.capture_frame.IsEmpty() && clip) {
            var len = clip.length;

            var frames = Mathf.CeilToInt(len * data.auto_frames_per_s);

            if (frames < 2) frames = 2;

            ap.time_ms = new int[frames - 1];
            ap.capture_frame = new int[frames - 1];

            for (int i = 1; i < frames; i++) {
               float time = i * len / (frames - 1);
               float dt = time - (i - 1) * len / (frames - 1);

               ap.time_ms[i - 1] = Mathf.RoundToInt(dt * 1000);
               ap.capture_frame[i - 1] = Mathf.FloorToInt(time * 60);
            }

            return true;
         }

         return false;
      }

      foreach (var g in groups) {
         foreach (AnimationTypeObject data in g) {
            var ap = new Shared.AnimationParsed();

            ap.clip = data.clip_name;
            ap.animation_type = data.animation_type;
            ap.category = data.category;
            ap.auto_frames_per_s = data.auto_frames_per_s;


            if (data.time_ms.IsEmpty()) {
               if (!TryFixMissingMs(data, ap)) {
                  Debug.Log($"Missing animation ms for: {data.name}, clips refered: {data.clip_name}!");
                  var clio = clips[data.clip_name];
                  Debug.Log($"Missing animation ms for: {data.name}, clips refered: {data.clip_name}! but found: {clio}");
               }
            } else {
               ap.time_ms = data.time_ms;
               ap.capture_frame = data.capture_frame;
            }


            yield return ap;
         }
      }
   }
}