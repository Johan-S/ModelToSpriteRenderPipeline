using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextName : EditorMonoBehavior {

   public UnityEngine.UI.Text text;

   public override void Execute() {
      if (!text) text = GetComponentInChildren<Text>();
      if (text) text.text = name;
   }
}
