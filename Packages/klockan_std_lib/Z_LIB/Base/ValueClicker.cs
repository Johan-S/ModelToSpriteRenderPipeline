using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ValueClicker : MonoBehaviour {

   [System.Serializable]
   public class ValueEvent : UnityEvent<int> {
   }

   public ValueEvent onValueChange;

   public Text text;


   public Button inc;
   public Button dec;


   public int val;

   public List<int> vals = new List<int> {
      0, 2, 4, 8, 16, 32, 1000
   };


   // Start is called before the first frame update
   void Start() {
      inc.onClick.AddListener(() => {
         val++;
         if (val >= vals.Count) val = vals.Count - 1;
         onValueChange.Invoke(vals[val]);
      });
      dec.onClick.AddListener(() => {
         val--;
         if (val < 0) val = 0;
         onValueChange.Invoke(vals[val]);
      });
      text.text = $"{vals[val]}";
      onValueChange.AddListener(x => {
         text.text = $"{x}";
      });
   }
}
