using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class EagerButton : MonoBehaviour, IPointerDownHandler {

   Button b;

   public Button.ButtonClickedEvent onClick;

   private void Awake() {
      b = GetComponent<Button>();
   }

   public void OnPointerDown(PointerEventData eventData) {

      if (eventData.button == PointerEventData.InputButton.Left) {
         if (b.IsInteractable()) {
            onClick.Invoke();
            AnnotatedUI.PropagateDirty(transform);
         }

      }
   }
}
