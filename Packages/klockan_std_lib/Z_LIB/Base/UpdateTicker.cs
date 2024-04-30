using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class UpdateTicker : MonoBehaviour {
   public TickEvent onTick;

   public float tickTime = 1;

   public float valStart = 1;
   public float valEnd = 0;

   [System.Serializable]
   public class TickEvent : UnityEvent<float> {
   }

   [SerializeField] float cur;

   // Start is called before the first frame update
   void Start() {
      onTick.Invoke(valStart);
   }

   // Update is called once per frame
   void Update() {
      cur += Time.deltaTime;

      if (cur >= tickTime) {
         onTick.Invoke(valEnd);
         Destroy(this);
      } else {
         onTick.Invoke(Mathf.Lerp(valStart, valEnd, cur / tickTime));
      }
   }
}