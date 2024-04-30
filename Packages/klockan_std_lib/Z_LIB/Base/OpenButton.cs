using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Closes canvas group.
public class OpenButton : MonoBehaviour {

   public GameObject target;

   public void Open() {
      if (target) target.SetActive(true);
   }

   // Start is called before the first frame update
   void Awake() {
      GetComponentInChildren<Button>().onClick.AddListener(Open);
   }
}
