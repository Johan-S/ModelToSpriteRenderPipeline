using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using static UIParser;
using static AnnotatedUI;

using static UnityEngine.Object;

public class UIOperations {


   static HashSet<string> reported_missing = new HashSet<string>();

   public static SymbolBehavior GetSymbolFor(string name) {
      if (name == null || name.Length == 0) return null;
      var res = behaviors.Get(name, null);
      if (res == null) {
         if (!reported_missing.Contains(name)) {
            reported_missing.Add(name);
            Debug.LogError($"Missing behavior: {name}!");
         }
      }
      return res;
   }


   static Dictionary<string, SymbolBehavior> behaviors = new Dictionary<string, SymbolBehavior> {
      { "interactable_", new SetInteractable() },
      { "inter_", new SetInteractable() },
      { "shorttooltip_", new SpawnTooltipSub{ is_short = true} },
      { "tooltip_", new SpawnTooltipSub() },
      { "this_", new Sub_This() },
     {"rep_", new RepeatElement()},
     {"next_if_", new NoOp()},
     {"if_", new If_Direct()},
     {"parent_if_", new ParentIf()},
     {"sub_if_", new SubTree{ sub_if = true} },
     {"format_", new SetString{ format = true} },
     {"opts_", new SetInteractable()},
     {"val_", new InteractableValue()},
     {"invoke_", new InvokableButton()},
     {"title_", new SetString{ title_case =true} },
     {"text_", new SetString()},
     {"percent_width_", new PercentWidth()},
     {"color_", new SetColor()},
     // {"label_", new SetInteractable()},
     {"image_", new ImageSymbol()},
     {"first_", new SubTree{ first_iter = true} },
     {"sub_", new SubTree()},
   };

   public class NoOp : SymbolBehavior {
   }
   public class SetInteractable : SymbolBehavior {
      public override void Compile(UnityBridge br) {
         Behaviour search_for = null;
         var parent = br.transform;
         while (parent) {
            search_for = parent.GetComponent<CanvasGroup>();
            if (search_for) {
               break;
            }
            search_for = parent.GetComponent<Selectable>();
            if (search_for) {
               break;
            }
         }

         br.symbol_behaviors = new Behaviour[] {
            search_for
         };
      }
      protected override IEnumerable<UIExecutionContext> Execute_Impl(UnityBridge br, UIExecutionContext ctx) {

         var c = br.symbol_behaviors[0];
         if (c) {
            if (c is CanvasGroup cg) {
               cg.interactable = ctx.val_true;
            } else if (c is Selectable sel) {
               sel.interactable = ctx.val_true;
            }
         }

         return br.Continue(ctx.val);
      }
   }
   public class NextIf : SymbolBehavior {
   }

   public class RepeatElement : SymbolBehavior {
      public override void Compile(UnityBridge br) {

      }
      protected override IEnumerable<UIExecutionContext> Execute_Impl(UnityBridge br, UIExecutionContext ctx) {



         bool ie = ctx.val is IEnumerable;

         br.gameObject.SetActive(false);

         string name = br.name;
         var tr = br.transform;
         string sub_name = "x";
         var parent = tr.parent;
         int arr = name.IndexOf("->");
         if (arr > 0) {
            sub_name = name.Substring(arr + 2);
            name = name.Substring(0, arr);
         }

         var val = ctx.val;
         if (val is IEnumerable e) {
            int i = 0;
            var si = tr.GetSiblingIndex();
            var elements = br;
            foreach (var ob in e) {
               UnityBridge sub = null;
               if (i < elements.elements.Count) {
                  sub = elements.elements[i];
               } else {
                  sub = Instantiate(br, parent);
                  sub.tmp_clone = true;
                  sub.transform.SetSiblingIndex(si + i + 1);
                  elements.elements.Add(sub);
                  sub.gameObject.SetActive(true);

                  sub.special_symbol = null;
               }
               i++;

               sub.SetSymbolName(sub_name);


               object sub_o = ob;
               if (ob is DirtyUpdater.DirtyTracker) {
                  sub.gameObject.ForceComponent<DirtyUpdater>().Track((DirtyUpdater.DirtyTracker)ob);
               }
               for (int aa = 0; aa < sub.value_holders.Length; ++aa) {
                  var x = (AnnotatedUI.ValueHolder)sub.value_holders[aa];
                  x.set(sub_o);
               }
               if (sub_o is AnnotatedUI.ReverseLinkObject sio) {
                  sio.SetInterfaceObject(sub.transform);
               }

               var sub_ctx = new UIExecutionContext();

               sub_ctx.unity_bridge = sub;
               sub_ctx.traversing_data = sub_o;

               yield return sub_ctx;
            }
            while (i < elements.elements.Count) {
               Destroy(elements.elements.Back().gameObject);
               elements.elements.Pop();
            }
            tr.gameObject.SetActive(false);
         }
      }
   }
   public class SpawnTooltipSub : SymbolBehavior {

      public bool is_short;

      protected override IEnumerable<UIExecutionContext> Execute_Impl(UnityBridge br, UIExecutionContext ctx) {

         var val = ctx.val;
         var tr = br.transform;

         var o = ctx.traversing_data;


         {
            var prefab = UnityBridgeBuilder.GetTooltipFor(val, is_short: is_short);
            Debug.Assert(prefab, $"No tooltip for type: {val.GetType().Name}, full val: {val}");
            var elements = br;
            UnityBridge sub;

            if (elements.elements.Count == 0) {
               sub = Instantiate(prefab, br.transform.parent);
               sub.gameObject.SetActive(true);
               sub.tmp_clone = true;
               sub.special_symbol = null;
               elements.elements.Add(sub);
            } else {
               sub = elements.elements[0];
            }

            // TODO: some bug happened here before?

            // if (sub.name != expected_name) {
            if (sub.GetComponent<ContentSizeFitter>()) {
               sub.GetComponent<ContentSizeFitter>().enabled = false;
            }
            sub.transform.SetSiblingIndex(tr.GetSiblingIndex() + 1);
            if (sub.GetComponent<ValueHolder>() != null) {
               foreach (var x in tr.GetComponents<ValueHolder>()) x.set(val);
            }
            if (o is ClickableObject && !(val is ClickableObject)) {
               var im = sub.GetComponent<Image>();
               if (im) {
                  im.color = new Color();
               }
            }
            if (o is SelectableObjectMarker && !(val is SelectableObjectMarker)) {
               var im = sub.GetComponent<Image>();
               if (im) {
                  im.color = new Color();
               }
            }
            yield return new UIExecutionContext {
               unity_bridge = sub,
               traversing_data = val,
            };
         }
      }
   }

   public class Sub_This : SymbolBehavior {
      protected override IEnumerable<UIExecutionContext> Execute_Impl(UnityBridge br, UIExecutionContext ctx) {

         var val = ctx.val;
         ctx.traversing_data = val;
         return br.Stop();
      }
   }

   public class If_Direct : SymbolBehavior {
      protected override IEnumerable<UIExecutionContext> Execute_Impl(UnityBridge br, UIExecutionContext ctx) {

         if (!ctx.val_true) {
            br.gameObject.SetActive(false);
            return br.Stop();
         }
         br.gameObject.SetActive(true);
         return br.Continue(ctx.traversing_data);
      }
   }

   public class ParentIf : SymbolBehavior {
      protected override IEnumerable<UIExecutionContext> Execute_Impl(UnityBridge br, UIExecutionContext ctx) {
         br.transform.parent.gameObject.SetActive(ctx.val_true);
         yield break;
      }
   }

   public class SubTree : SymbolBehavior {

      public bool sub_if;

      public bool first_iter;

      protected override IEnumerable<UIExecutionContext> Execute_Impl(UnityBridge br, UIExecutionContext ctx) {
         if (sub_if) {
            if (!ctx.val_true) return new UIExecutionContext[0];
         }
         var data = ctx.val;
         if (first_iter) {
            if (data is IEnumerable ie) {
               var it = ie.GetEnumerator();
               if (it.MoveNext()) {
                  data = it.Current;
                  br.gameObject.SetActive(true);
               } else {
                  br.gameObject.SetActive(false);
                  return br.Stop();
               }
            }
         }
         return br.Continue(data);
      }
   }
   public class PercentWidth : SymbolBehavior {
      public override void Compile(UnityBridge br) {

         br.symbol_behaviors = new Behaviour[1] {
            br.transform.GetComponentInParent_Safe<Image>()
      };
      }
      protected override IEnumerable<UIExecutionContext> Execute_Impl(UnityBridge br, UIExecutionContext ctx) {
         float c = (float)ctx.val;
         var p = br.transform.parent.GetComponent<RectTransform>();
         var pp = p.parent.GetComponent<RectTransform>();
         p.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, pp.rect.width * Mathf.Clamp(c, 0, 1));
         yield break;
      }
   }
   public class SetColor : SymbolBehavior {
      public override void Compile(UnityBridge br) {

         br.symbol_behaviors = new Behaviour[1] {
            br.transform.GetComponentInParent_Safe<Image>()
      };
      }
      protected override IEnumerable<UIExecutionContext> Execute_Impl(UnityBridge br, UIExecutionContext ctx) {
         var c = (Color)ctx.val;
         var img = (Image)br.symbol_behaviors[0];
         img.color = img.color.KeepAlpha(c);
         yield break;
      }
   }
   public class SetString : SymbolBehavior {

      public bool title_case;

      public bool format;


      public override void Compile(UnityBridge br) {

         var t = br.GetComponent<Text>();
         br.symbol_behaviors = new Behaviour[1] {
            t
         };
         br.s1 = t.text;
      }
      protected override IEnumerable<UIExecutionContext> Execute_Impl(UnityBridge br, UIExecutionContext ctx) {
         var t = (Text)br.symbol_behaviors[0];
         var data = ctx.val;

         if (data == null) {
            br.gameObject.SetActive(false);
            yield break;
         }
         br.gameObject.SetActive(true);

         string s = data.ToString();
         if (title_case) {
            s = s.ToTitleCase();
         }
         if (format) {
            s = string.Format(br.s1, s);
         }
         t.text = s;
         yield break;
      }
   }
   public class ImageSymbol : SymbolBehavior {
      public override void Compile(UnityBridge br) {
         var dyn = br.gameObject.ForceComponent<DynamicSpriteUpdater>();

         var t = br.GetComponent<Image>();
         dyn.image = t;
         br.symbol_behaviors = new Behaviour[] {
            dyn, t,
         };
      }
      protected override IEnumerable<UIExecutionContext> Execute_Impl(UnityBridge br, UIExecutionContext ctx) {
         var val = ctx.val;

         var img = br.GetComponent<Image>();
         if (val is ColoredSprite cs) {
            var dyn = br.gameObject.ForceComponent<DynamicSpriteUpdater>();
            dyn.enabled = true;
            dyn.SetVars(img, cs);
         } else {
            var dyn = br.GetComponent<DynamicSpriteUpdater>();

            if (dyn) {
               dyn.CleanupSprite();
               dyn.enabled = false;
            }

            img.sprite = (Sprite)val;
         }
         return br.Continue(ctx.val);
      }
   }
   public class InteractableValue : SymbolBehavior {
      public override void Compile(UnityBridge br) {
         var fs = br.gameObject.ForceComponent<FieldSetterCallback>();

         br.symbol_behaviors = new Behaviour[] {
            fs,
         };
      }
      protected override IEnumerable<UIExecutionContext> Execute_Impl(UnityBridge br, UIExecutionContext ctx) {

         var val = ctx.val;
         var o = ctx.traversing_data;
         var fs = (FieldSetterCallback)br.symbol_behaviors[0];
         fs.o = o;
         fs.field = ctx.val_field_path;
         fs.PropagateVal();

         return br.Continue(ctx.val);
      }
   }
   public class InvokableButton : SymbolBehavior {
      public override void Compile(UnityBridge br) {
         var inter = br.transform.GetComponentInParent_Safe<Selectable>();
         if (!inter) {
            inter = br.transform.parent.GetComponent<Selectable>();
         }
         var bp = br.GetComponent<Button>() ?? br.transform.parent.GetComponent<Button>();
         var callback = bp.gameObject.ForceComponent<OnClickCallback>();
         br.symbol_behaviors = new Behaviour[] {
            inter, bp, callback,
         };
      }
      protected override IEnumerable<UIExecutionContext> Execute_Impl(UnityBridge br, UIExecutionContext ctx) {
         var inter = (Selectable)br.symbol_behaviors[0];
         var bp = (Button)br.symbol_behaviors[1];
         var callback = (OnClickCallback)br.symbol_behaviors[2];

         bool ok = ctx.val_true;
         if (ok) {
            System.Action val = (System.Action)ctx.val;
            callback.action = val;
         }

         return br.Continue(ctx.val);
      }
   }
}
