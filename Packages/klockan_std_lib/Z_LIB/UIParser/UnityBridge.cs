using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UIParser;
using static AnnotatedUI;

public class UnityBridge : MonoBehaviour {
   public List<MonoBehaviour> behaviors_to_visit = new List<MonoBehaviour>();
   public List<UnityBridge> children = new List<UnityBridge>();

   public UIParser.SymbolBehavior special_symbol;
   public Behaviour[] symbol_behaviors;

   public string symbol_part;
   public string fetch_part;
   public string sub_name;

   public bool tmp_clone;

   public MonoBehaviour[] value_holders;

   public List<UnityBridge> elements = new List<UnityBridge>();

   public UIExecutionContext prev_context;

   public string s1;

   private void Awake() {
      if (symbol_part != null) {
         special_symbol = UIOperations.GetSymbolFor(symbol_part);
      }
   }

   public void SetSymbolName(string n) {
      (symbol_part, fetch_part) = UIParser.GetSymbolRestSplit(n);


      int arr = fetch_part.IndexOf("->");
      if (arr >= 0) {
         sub_name = fetch_part.Substring(arr + 2);
         fetch_part = fetch_part.Substring(0, arr);
      }

      special_symbol = UIOperations.GetSymbolFor(symbol_part);
   }

   public IEnumerable<UIExecutionContext> Stop() {
      yield break;
   }


   public void SetValueHolders(object val) {
      foreach (var vh in value_holders) {
         ((ValueHolder)vh).set(val);
      }
   }

   public IEnumerable<UIExecutionContext> Continue(object val) {
      SetValueHolders(val);
      foreach (var ch in children) {
         yield return new UIExecutionContext {
            traversing_data = val,
            unity_bridge = ch,
         };
      }
   }

   public void EvalTop(object val) {
      UIExecutionContext ctx = new UIExecutionContext {
         traversing_data = val,
         unity_bridge = this,
      };
      Eval(this, ctx);
   }


   static HashSet<string> get_strings = new HashSet<string>();

   public static void Eval(UnityBridge br, UIExecutionContext ctx) {
      if (!br.tmp_clone) {
         if (br.special_symbol == null && 0 < (br.symbol_part?.Length ?? 0)) {
            br.special_symbol = UIOperations.GetSymbolFor(br.symbol_part);
            // Console.Log($"has_special: {br.special_symbol != null}, {br.symbol_part}?", "{br.fetch_part}, {br.symbol_part}, , {ctx.traversing_data}, {ctx.val}");
         }
      }

      var o = ctx.traversing_data;
      {
         var res = AnnotatedUI.LazyGet(o, br.fetch_part);
         if (!get_strings.Contains(br.fetch_part)) {
            get_strings.Add(br.fetch_part);
         }

         ctx.val = res.val;
         ctx.val_true = res.has_val;
      }
      // Console.Log(' '.ToCharString(ctx.iteration * 4) + $"br:\"{br.name}\"", $"has_special: {br.special_symbol != null}, {br.symbol_part}?" , "{br.fetch_part}, {br.symbol_part}, , {ctx.traversing_data}, {ctx.val}");


      if (br.special_symbol == null) {
         foreach (var ch in br.Continue(o)) {
            ch.iteration = ctx.iteration + 1;
            Eval(ch.unity_bridge, ch);
         }
      } else {
         // Console.print($"Trying to fetch tooltip for {br.name}, {ctx.val}, ctx {ctx.traversing_data}");


         foreach (var ch in br.special_symbol.Execute(br, ctx)) {
            ch.iteration = ctx.iteration + 1;
            Eval(ch.unity_bridge, ch);
         }
      }

      br.prev_context = ctx;
   }
}