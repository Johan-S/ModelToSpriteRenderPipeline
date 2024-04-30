using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MessageCanvas : MonoBehaviour {

   public static MessageCanvas message;
   public static RectTransform tr;

   // Start is called before the first frame update
   void Start() {
      if (tag != "MessageCanvas") throw new System.Exception("Message canvas needs to have tag 'MessageCanvas'");
      if (message) Destroy(gameObject);
      else {
         message = this;
         tr = GetComponent<RectTransform>();
         DontDestroyOnLoad(gameObject);
      }
   }

   // Update is called once per frame
   void Update() {

   }
}
