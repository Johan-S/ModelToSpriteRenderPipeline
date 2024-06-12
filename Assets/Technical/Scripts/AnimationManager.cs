using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shared;
using TMPro;
using static ExportPipeline;

public class AnimationManager : MonoBehaviour {
   public static IEnumerable<Shared.AnimationParsed> GetDirectAnimationsParsed() {
      var allclips = Resources.LoadAll<AnimationBundle>("");

      Dictionary<string, AnimationClip> clips = allclips.FlatMap(x => x.animation_clips).ToDictionary(x => x.name);

      var anim_objs = Resources.LoadAll<AnimationTypeObject>("");

      var groups = anim_objs.GroupBy(x => SharedUtils.NormalizeAnimationName(x.animation_type));

      foreach (var g in groups) {

         foreach (var data in g) {
            AnimationClip clip =
               data.clip_ref ? data.clip_ref : clips.Get(data.clip);
            var ap = new Shared.AnimationParsed();

            ap.clip = data.clip_name;
            ap.animation_type = data.animation_type;
            ap.category = data.category;
            ap.auto_frames_per_s = data.auto_frames_per_s;


            if (ap.time_ms.IsEmpty() && data.auto_frames_per_s > 0 && clip) {
               if (data.auto_frames_per_s > 0 && data.capture_frame.IsEmpty()) {
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