using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenWindowButton : MonoBehaviour {

   public GameObject window_to_open;

   public void Open() {
      Instantiate(window_to_open, transform.root);
   }

   // Start is called before the first frame update
   void Awake() {
      gameObject.OnClick(Open);
   }

   // Update is called once per frame
   void Update() {

   }
}
