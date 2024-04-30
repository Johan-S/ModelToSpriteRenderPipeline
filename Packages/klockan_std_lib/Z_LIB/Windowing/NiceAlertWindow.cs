using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NiceAlertWindow : MonoBehaviour {


   public TMP_Text text_prefab;

   static NiceAlertWindow instance;

   public static void SendAlert(string msg) {
      if (instance) {
         instance.DoMessage(msg);
      } else {
         Debug.Log("Missing Alert:" + msg);
      }
   }

   private void Awake() {
      instance = this;
   }


   public void DoMessage(string msg) {

      var t = Instantiate(text_prefab);
      t.text = msg;
      t.transform.SetParent(transform);
      t.gameObject.ForceComponent<DestroyAfter>().duration = 5;
   }

   // Update is called once per frame
   void Update() {

      transform.SetAsLastSibling();
   }
}
