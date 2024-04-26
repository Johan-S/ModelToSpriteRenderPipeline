using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Closes canvas group.
public class CloseButton : MonoBehaviour {

   public bool destroy;

   public void Close() {

      GameObject o = null;

      var t = transform;
      while (t) {
         t = t.parent;
         if (!t) break;
         if (t.GetComponent<CanvasGroup>()) o = t.gameObject;
         if (t.GetComponent<WindowContext>()) {
            o = t.gameObject;
            break;
         }
      }
      if (destroy) Destroy(o);
      o.SetActive(false);
   }

   // Start is called before the first frame update
   void Awake() {
      GetComponentInChildren<Button>().onClick.AddListener(Close);
   }
}
