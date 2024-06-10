using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;


public class MainObject : MonoBehaviour {

   public static Coroutine DelayCall(float t, Action a) {

      static IEnumerator Impl(float t, Action a) {
         yield return new WaitForSeconds(t);
         a?.Invoke();
      }

      return instance.StartCoroutine(Impl(t, a));
   }


   public static MainObject instance;
   [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
   static void MakeMain() {
      instance = new GameObject("Main").AddComponent<MainObject>();
   }

   void Awake() {
      DontDestroyOnLoad(gameObject);
   }
}