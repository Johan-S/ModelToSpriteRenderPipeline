using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FieldSetterCallback : MonoBehaviour {

   public object o;
   public string field;

   public GeneratedElements acc;

   bool connected_to_int;

   bool propagating;

   public void PropagateVal() {
      if (propagating) return;
      propagating = true;
      try {
         var tg = GetComponent<Toggle>();
         var sl = GetComponent<Slider>();
         var inp = GetComponent<InputField>();
         var inpt = GetComponent<TMP_InputField>();
         var drop_d = GetComponent<TMP_Dropdown>();
         var val = AnnotatedUI.GetVal_External_UI(o, field, acc);
         if (tg) {
            tg.isOn = (bool)val;
         }

         if (sl) {
            if (val is float) {
               sl.value = (float)val;
            } else if (val is IntCastable e) {
               sl.value = e.AsInt();
            } else {
               sl.value = (int)val;
            }
         }
         if (inp) {
            if (val is int ival) {
               inp.text = ival.ToString();
               connected_to_int = true;
            } else {
               inp.text = (string)val;
            }
         }
         if (inpt) {
            if (val is int ival) {
               inpt.text = ival.ToString();
               connected_to_int = true;
            } else {
               inpt.text = (string)val;
            }
         }

         {
            var d = GetComponent<Dropdown>();
            if (d) {
               if (val is string vv) {
                  for (int i = 0; i < d.options.Count; ++i) {
                     if (vv == d.options[i].text) {
                        d.value = i;
                        break;
                     }
                  }
               } else if (val is System.Enum) {
                  int ival = (int)val;
                  d.value = ival;
                  connected_to_int = true;
               } else if (val is int ival) {
                  d.value = ival;
                  connected_to_int = true;
               } else {
                  throw new System.Exception($"Invalid dropdown type {val.GetType()}");
               }
            }
            
         }

         {
            var d = GetComponent<TMP_Dropdown>();
            if (d) {
               if (val is string vv) {
                  for (int i = 0; i < d.options.Count; ++i) {
                     if (vv == d.options[i].text) {
                        d.value = i;
                        break;
                     }
                  }
               } else if (val is System.Enum) {
                  int ival = (int)val;
                  d.value = ival;
                  connected_to_int = true;
               } else if (val is int ival) {
                  d.value = ival;
                  connected_to_int = true;
               } else {
                  throw new System.Exception($"Invalid dropdown type {val.GetType()}");
               }
            }
            
         }
      } finally {
         propagating = false;
      }
   }

   void RegisterListeners() {
      var sl = GetComponent<Slider>();
      var tg = GetComponent<Toggle>();
      var inp = GetComponent<InputField>();
      var inpt = GetComponent<TMP_InputField>();
      var d = GetComponent<Dropdown>();
      var b = GetComponent<Button>();
      var drop_d = GetComponent<TMP_Dropdown>();
      if (b) {
         b.onClick.AddListener(() => {
            var val = AnnotatedUI.GetVal_External_UI(o, field, acc);
            if (val is bool bo) {
               AnnotatedUI.SetVal(o, field, !bo, acc);
            } else if (val is null) {
               AnnotatedUI.SetVal(o, field, true, acc);
            }
         });
      }

      if (sl) {
         sl.onValueChanged.AddListener(x => {
            if (o != null) {
               float a = (float)x;
               AnnotatedUI.SetVal(o, field, a, acc);
               PropagateVal();
            }
         });
      }
      if (tg) {
         tg.onValueChanged.AddListener(x => {
            if (o != null) {
               SoundManager.instance.Select();
               AnnotatedUI.SetVal(o, field, x, acc);
               PropagateVal();
            }
         });
      }
      if (inp) {
         inp.onValueChanged.AddListener(x => {
            if (o != null) {

               if (connected_to_int) {
                  if (int.TryParse(x, out int ival)) {
                     AnnotatedUI.SetVal(o, field, ival, acc);
                  }
               } else {
                  AnnotatedUI.SetVal(o, field, x, acc);
               }
            }
         });
      }
      if (inpt) {
         inpt.onValueChanged.AddListener(x => {
            if (o != null) {

               if (connected_to_int) {
                  if (int.TryParse(x, out int ival)) {
                     AnnotatedUI.SetVal(o, field, ival, acc);
                  }
               } else {
                  AnnotatedUI.SetVal(o, field, x, acc);
               }
            }
         });
      }
      if (d) {
         d.onValueChanged.AddListener(x => {
            if (o != null) {
               SoundManager.instance.Select();
               if (connected_to_int) {
                  AnnotatedUI.SetVal(o, field, x, acc);
               } else {
                  AnnotatedUI.SetVal(o, field, d.options[x].text, acc);
               }
               PropagateVal();
            }
         });
      }
      if (drop_d) {
         var val = AnnotatedUI.GetVal_External_UI(o, field, acc);

         if (val is Enum) {
            var vals = Enum.GetValues(val.GetType());
            List<string> names = new();
            foreach (var a in vals) {
               names.Add(a.ToString().ToTitleCase());
            }

            drop_d.options = names.Select(x => new TMP_Dropdown.OptionData(x)).ToList();



         }
         drop_d.onValueChanged.AddListener(x => {
            if (o != null) {
               SoundManager.instance.Select();
               if (connected_to_int) {
                  AnnotatedUI.SetVal(o, field, x, acc);
               } else {
                  AnnotatedUI.SetVal(o, field, drop_d.options[x].text, acc);
               }
               PropagateVal();
            }
         });
      }
   }

   void Start() {
      PropagateVal();
      RegisterListeners();
   }
}
