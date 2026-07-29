
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using Random = UnityEngine.Random;

using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.Animations;
using UnityEngine.Playables;
using static System.Runtime.CompilerServices.MethodImplOptions;


public static class AnimationBlender {
   

   /// <summary>
   /// Generates a new AnimationClip by blending two existing clips.
   /// </summary>
   /// <param name="clipA">The first animation clip (Weight 0)</param>
   /// <param name="clipB">The second animation clip (Weight 1)</param>
   /// <param name="blendWeight">0 returns clipA, 1 returns clipB, 0.5 is exactly in the middle</param>
   /// <returns>A new, blended AnimationClip</returns>
   public static AnimationClip CreateBlendedClip(AnimationClip clipA, AnimationClip clipB, float blendWeight, string rootBoneName = "") {
      if (clipA == null || clipB == null) {
         Debug.LogError("Both AnimationClips must be assigned to blend.");
         return null;
      }

      AnimationClip blendedClip = new AnimationClip();
      blendedClip.name = $"{clipA.name}_blend_{clipB.name}";
      // Use the highest framerate of the two clips for the best quality
      blendedClip.frameRate = clipA.frameRate;
      
      AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clipA);
      AnimationUtility.SetAnimationClipSettings(blendedClip, settings);

      // 1. Gather all property bindings (bones, transforms, floats) from both clips
      HashSet<EditorCurveBinding> allBindings = new HashSet<EditorCurveBinding>();
      allBindings.UnionWith(AnimationUtility.GetCurveBindings(clipA));
      allBindings.UnionWith(AnimationUtility.GetCurveBindings(clipB));

      float maxLength = clipA.length;
      float timeStep = clipA.length / maxLength / blendedClip.frameRate;
      float bSpeed = clipB.length / maxLength;

      // 2. Iterate through every bone/property being animated
      foreach (EditorCurveBinding binding in allBindings) {
         AnimationCurve curveA = AnimationUtility.GetEditorCurve(clipA, binding);
         AnimationCurve curveB = AnimationUtility.GetEditorCurve(clipB, binding);
         bool isRoot = string.IsNullOrEmpty(binding.path); // Checks for Unity Root Motion
         Debug.Log($"Binding: {binding.path}");
         if (!string.IsNullOrEmpty(rootBoneName) && binding.path.Equals(rootBoneName))
         {
            isRoot = true; // Checks for specific skeletal root bone
            // Debug.Log($"Is Root: {binding.path}");
         }
         if (curveA == null && curveB == null) continue;

         AnimationCurve blendedCurve = new AnimationCurve();

         // 3. Sample the values frame-by-frame across the duration
         for (float time = 0; time <= maxLength; time += timeStep) {
            // If a bone is animated in one clip but not the other, 
            // we fall back to the existing clip's value to prevent snapping to 0.
            
            // float valA = curveA != null ? curveA.Evaluate(time) : (curveB != null ? curveB.Evaluate(time * bSpeed) : 0f);
            // float valB = curveB != null ? curveB.Evaluate(time * bSpeed) : (curveA != null ? curveA.Evaluate(time) : 0f);
            
            float valA = curveA != null ? curveA.Evaluate(time) : 0f;
            float valB = curveB != null ? curveB.Evaluate(time * bSpeed) : 0f;

            // Blend the raw float values
            float blendedVal = Mathf.Lerp(valA, valB, blendWeight);

            blendedCurve.AddKey(time, blendedVal);
         }

         // 4. Smooth out the generated keyframes to prevent rigid transitions
         for (int i = 0; i < blendedCurve.keys.Length; i++) {
            AnimationUtility.SetKeyLeftTangentMode(blendedCurve, i, AnimationUtility.TangentMode.Auto);
            AnimationUtility.SetKeyRightTangentMode(blendedCurve, i, AnimationUtility.TangentMode.Auto);
         }

         // 5. Apply the blended curve to the new clip
         AnimationUtility.SetEditorCurve(blendedClip, binding, blendedCurve);
      }
      Debug.Log($"Has root: {new{blendedClip.hasRootCurves, blendedClip.hasGenericRootTransform, blendedClip.hasMotionCurves}}");
      Debug.Log($"Has root: {new{clipA.hasRootCurves, clipA.hasGenericRootTransform, clipA.hasMotionCurves}}");

      return blendedClip;
   }
   /// <summary>
    /// Uses Unity's native PlayableGraph to perfectly blend two clips and bake the result into a new AnimationClip.
    /// </summary>
    public static AnimationClip BakeBlendedClip(GameObject go, AnimationClip clipA, AnimationClip clipB, float blendWeight)
    {
        if (go == null || clipA == null || clipB == null)
        {
            Debug.LogError("GameObject and both AnimationClips must be assigned.");
            return null;
        }

        Animator animator = go.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("The target GameObject must have an Animator component.");
            return null;
        }

        var ctrl = animator.runtimeAnimatorController;
        animator.runtimeAnimatorController = null;

        bool old_enabled = animator.enabled;
        animator.enabled = true;

        try {

           // 1. Prepare the new clip and calculate duration
           AnimationClip bakedClip = new AnimationClip();
           bakedClip.name = $"{clipA.name}_baked_{clipB.name}";
           bakedClip.frameRate = clipA.frameRate;

           float length = clipA.length;
           float timeStep = 1f / bakedClip.frameRate;

           // 2. Setup the Playable Graph to handle the complex blending math natively
           PlayableGraph graph = PlayableGraph.Create("BakeGraph");
           graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

           var animOutput = AnimationPlayableOutput.Create(graph, "Output", animator);
           var mixer = AnimationMixerPlayable.Create(graph, 2);
           animOutput.SetSourcePlayable(mixer);

           var playableA = AnimationClipPlayable.Create(graph, clipA);
           var playableB = AnimationClipPlayable.Create(graph, clipB);

           graph.Connect(playableA, 0, mixer, 0);
           graph.Connect(playableB, 0, mixer, 1);

           mixer.SetInputWeight(0, 1f - blendWeight);
           mixer.SetInputWeight(1, blendWeight);

           // 3. Setup the Recorder to listen to the GameObject's transforms
           GameObjectRecorder recorder = new GameObjectRecorder(go);
           
           // This binds every Transform in the hierarchy. 
           // It captures positions, rotations, and scales exactly as they appear visually.
           recorder.BindComponentsOfType<Transform>(go, true);

           var st = go.transform.localPosition;
           // 4. Step through time frame-by-frame, forcing the graph to pose the model, and taking a snapshot
           for (float t = 0; t <= length; t += timeStep)
           {
               playableA.SetTime(t);
               playableB.SetTime(t * clipB.length / clipA.length);
               
               // Force the graph to evaluate at this exact frame
               graph.Evaluate(0f);
               animator.Update(0f);
               go.transform.localPosition = st;

               // Record the visual pose into the clip
               recorder.TakeSnapshot(timeStep);
           }

           // 5. Save the recorded data into our new clip and clean up
           recorder.SaveToClip(bakedClip);
           graph.Destroy();

           // 6. Restore Animation Clip Settings (Looping, Root Transform Baking) from Clip A
           AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clipA);
           AnimationUtility.SetAnimationClipSettings(bakedClip, settings);

           return bakedClip;
        }
        finally {
           animator.enabled = old_enabled;
           animator.runtimeAnimatorController = ctrl;
        }
    }
}