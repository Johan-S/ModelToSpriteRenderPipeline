using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// [ExecuteInEditMode]
public abstract class EditorMonoBehavior : MonoBehaviour {

   public abstract void Execute();

   // Start is called before the first frame update
   void Start() {
      Execute();
   }

   // Update is called once per frame
   void Update() {
      if (!Application.isPlaying) Execute();
   }
}
