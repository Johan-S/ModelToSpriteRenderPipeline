using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using UnityEngine.UI;


public class ColorSchemeSetter : MonoBehaviour {

   public ColorSchemeCategory color_category;
   
   public enum ColorSchemeCategory {
      normal_text,
      background_outer,
      background_inner,
      background_window,
      table_row,
      clickable,
      selectable,
      menu_button,
      warning_text,

      background_text_menu_base,
      background_content_menu_base,
      background_collapsible_menu_base,
      background_dark_clarity,

      box_border_color,
      box_content_color,
   }
}
