using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OnClickCallback : MonoBehaviour {

   public System.Action action;

   // Start is called before the first frame update
   void Awake() {
      var sl = GetComponent<Button>();
      var ebp = GetComponent<EagerButton>();
      if (ebp) {
         ebp.onClick.AddListener(() => action());
      } else {

         sl.onClick.AddListener(() => action());
      }
   }
}
