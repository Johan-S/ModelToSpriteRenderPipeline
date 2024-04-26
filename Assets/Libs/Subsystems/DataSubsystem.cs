using System;
using UnityEngine;

public class DataSubsystem : MonoBehaviour {
   

   [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
   static void Onl() {
      var go = new GameObject("DataSubsystem", typeof(DataSubsystem));
      
      DontDestroyOnLoad(go);
   }

   public static Action runtimeUpdate;


   void Update() {
      runtimeUpdate?.Invoke();
   }
}