using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class NewBaseInputModule : BaseInputModule {

   public class NewInput : BaseInput {

      public BaseInput w;
      public override string compositionString {
         get => w.compositionString;
      }
      public override IMECompositionMode imeCompositionMode {
         get => w.imeCompositionMode;
         set => w.imeCompositionMode= value;
      }
      public override Vector2 compositionCursorPos {
         get => w.compositionCursorPos;
         set => w.compositionCursorPos = value;
      }
      public override bool mousePresent {
         get => w.mousePresent;
      }
      public override Vector2 mousePosition {
         get => w.mousePosition;
      }
      public override Vector2 mouseScrollDelta {
         get => w.mouseScrollDelta;
      }
      public override bool touchSupported {
         get => w.touchSupported;
      }
      public override int touchCount {
         get => w.touchCount;
      }

      public override float GetAxisRaw(string axisName) => w.GetAxisRaw(axisName);
      public override bool GetMouseButton(int button) => w.GetMouseButton(button);
      public override bool GetMouseButtonDown(int button) => w.GetMouseButtonDown(button);
      public override bool GetMouseButtonUp(int button) => w.GetMouseButtonUp(button);
      public override Touch GetTouch(int index) => w.GetTouch(index);



      public override bool GetButtonDown(string buttonName) {
         // Console.print($"Debug button " + buttonName);
         return false;
      }
   }

   public void RegisterEarlyCallback() {
   
   }


   public BaseInputModule other_module;

   public NewInput overrideinput;

   public override bool IsModuleSupported() {
      return false;
   }


   public override void Process() {
      Console.print("Process UI tick!");
      if (overrideinput == null) overrideinput = gameObject.AddComponent<NewInput>();
      overrideinput.w = input;
      other_module.inputOverride = overrideinput;
      other_module.enabled = true;
   }
}
