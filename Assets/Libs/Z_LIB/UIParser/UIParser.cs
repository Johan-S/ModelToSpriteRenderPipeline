using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static AnnotatedUI;

public class UIParser {

   public static ModelNode Visit_AnnotatedUI_2(Transform tr, ModelNode node_travel) {

      string name = tr.name;


      string symbol = null;

      for (int i = 1; i < symbols.Count; ++i) {
         if (name.StartsWith(symbols[i])) {
            symbol = symbols[i];
            break;
         }
      }

      ModelNode continue_with;

      if (symbol == null) {
         continue_with = node_travel;
      } else {

         if (node_travel.unity_source == tr) {
            continue_with = node_travel;
            node_travel.symbol = symbol;
            node_travel.name = name;
         } else {
            continue_with = new ModelNode {
               symbol = symbol,
               name = name,
               unity_source = tr,
            };

         }

         string fetch_name = continue_with.name;

         int arr = fetch_name.IndexOf("->");
         if (arr >= 0) {
            continue_with.sub_name = fetch_name.Substring(arr + 2);
            continue_with.name = fetch_name.Substring(0, arr);
         }
         node_travel.child.Add(continue_with);
      }
      continue_with.value_holders.AddRange(tr.GetComponents<ValueHolder>().Map(x => (MonoBehaviour)x));

      int n = tr.childCount;
      for (int i = 0; i < n; ++i) {
         var ch = tr.GetChild(i);
         var ret = Visit_AnnotatedUI_2(ch, continue_with);
      }

      return continue_with;
   }

   public static ModelNode CreateTopNode(Transform tr) {
      var continue_with = new ModelNode {
         symbol = null,
         name = tr.name,
         unity_source = tr,
      };

      return continue_with;
   }

   public static (string, string) GetSymbolRestSplit(string s) {
      string name = s;
      string symbol = null;

      for (int i = 1; i < symbols.Count; ++i) {
         if (name.StartsWith(symbols[i])) {
            symbol = symbols[i];
            return (symbol, s.Substring(symbol.Length));
         }
      }
      return (null, s);
   }

   public static ModelNode Visit_AnnotatedUI(Transform tr) {

      string name = tr.name;


      string symbol = null;

      for (int i = 1; i < symbols.Count; ++i) {
         if (name.StartsWith(symbols[i])) {
            symbol = symbols[i];
            break;
         }
      }

      var node = new ModelNode {
         symbol = symbol,
         name = name,
         unity_source = tr,
      };

      int n = tr.childCount;
      for (int i = 0; i < n; ++i) {
         var ch = tr.GetChild(i);
         node.child.Add(Visit_AnnotatedUI(ch));
      }

      return node;
   }


   public class ModelNode {
      public string symbol;
      public string name;
      public string sub_name;

      public Transform unity_source;

      public UnityBridge bridge;

      public List<ModelNode> child = new List<ModelNode>();
      public List<MonoBehaviour> value_holders = new List<MonoBehaviour>();
   }

   public class ExeclNode {

      public ModelNode parsed_node;


      public UnityBridge Execute() {
         return null;
      }

      public List<ModelNode> sub_nodes;
   }

   public class ExecutionContext {
      public List<ModelNode> model = new List<ModelNode>();
      public List<UnityBridge> unity = new List<UnityBridge>();
      public List<object> data = new List<object>();
   }

   public class UIExecutionContext {
      public UnityBridge unity_bridge;
      public object traversing_data;

      public object val;
      public string val_field_path;

      public int iteration;

      public bool val_true;
   }

   public abstract class SymbolBehavior {

      public string symbol;
      public virtual void Compile(UnityBridge br) {

      }

      protected virtual IEnumerable<UIExecutionContext> Execute_Impl(UnityBridge br, UIExecutionContext ctx) {
         yield break;
      }

      public IEnumerable<UIExecutionContext> Execute(UnityBridge br, UIExecutionContext ctx) {
         return Execute_Impl(br, ctx);
      }

      public override string ToString() {
         return $"UI<{GetType().Name}>";
      }
   }





   public static List<string> symbols = new List<string> {
     "interactable_",
     "inter_",
     "shorttooltip_",
     "tooltip_",
     "this_",
     "rep_",
     "next_if_",
     "if_",
     "parent_if_",
     "sub_if_",
     "format_",
     "opts_",
     "val_",
     "invoke_",
     "title_",
     "text_",
     "percent_width_",
     "color_",
     "label_",
     "image_",
     "first_",
     "sub_",
   };
}
