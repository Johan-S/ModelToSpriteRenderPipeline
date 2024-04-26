using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HoverColor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

   public Color hover_color = Color.white;

   public Color start_color;

   public bool color_set = false;

   public void OnPointerEnter(PointerEventData eventData) {
      GetComponent<Image>().color = hover_color;
   }

   public void OnPointerExit(PointerEventData eventData) {
      GetComponent<Image>().color = start_color;
   }

   private void OnEnable() {
      if (color_set) {
         GetComponent<Image>().color = start_color;
      } else {
         start_color = GetComponent<Image>().color;
         color_set = true;
      }
   }
}
