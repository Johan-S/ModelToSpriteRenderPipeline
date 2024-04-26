using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class OnClick : MonoBehaviour, IPointerClickHandler {

   public PointerEventData.InputButton button;

   [System.Serializable]
   public class OnMouseClick : UnityEvent {
     
   }

   public OnMouseClick onClick;

   public void OnPointerClick(PointerEventData eventData) {
      if (eventData.button == button) onClick.Invoke();
   }
}
