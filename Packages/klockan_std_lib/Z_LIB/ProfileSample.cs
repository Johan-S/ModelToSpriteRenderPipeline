using System;
using UnityEngine;
using UnityEngine.Profiling;

public class ProfileSample : IDisposable {
   
   


   public ProfileSample(string name) {
      this.name = name;
      
      Profiler.BeginSample(name);
   }

   private string name;

   private bool disposed;
   public void Dispose() {
      Profiler.EndSample();
      disposed = true;

   }

   ~ProfileSample() {
      if (!disposed) {
         Debug.Log("Failed to dispose ProfileSample<{name}>!");
      }
   }
}