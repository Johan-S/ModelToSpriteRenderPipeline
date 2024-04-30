using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleButton : MonoBehaviour {

   public GameObject target;

   public bool disable_target_on_start;
   private void Awake() {
      GetComponent<Button>().onClick.AddListener(() => target.SetActive(!target.activeSelf));
   }

   private void Start() {
      if (disable_target_on_start) {
         target.SetActive(false);
      }
   }
}
