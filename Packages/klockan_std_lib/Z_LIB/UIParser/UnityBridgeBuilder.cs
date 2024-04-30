using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UIParser;
using static AnnotatedUI;

public class UnityBridgeBuilder : MonoBehaviour {

   Dictionary<System.Type, UnityBridge> tooltip_mapper = new Dictionary<System.Type, UnityBridge>();
   Dictionary<Component, UnityBridge> tooltip_mapper_2 = new Dictionary<Component, UnityBridge>();

   Dictionary<System.Type, UnityBridge> short_tooltip_mapper = new Dictionary<System.Type, UnityBridge>();
   Dictionary<Component, UnityBridge> short_tooltip_mapper_2 = new Dictionary<Component, UnityBridge>();
   public static UnityBridge GetTooltipFor(object val, bool is_short) {

      if (val == null) return null;


      if (is_short) {
         var cand = instance.short_tooltip_mapper.Get(val.GetType(), null);
         if (cand) return cand;
         var tt = TooltipSwitch.instance.GetShortTooltipFor(val);

         cand = instance.short_tooltip_mapper_2.Get(tt, null);
         if (cand != null) return cand;

         var node = instance.MakeBridgePrefab(tt);

         instance.short_tooltip_mapper[val.GetType()] = node;
         instance.short_tooltip_mapper_2[tt] = node;
         // Console.print($"Make short tooltip {val}!");

         return node;

      } else {
         var cand = instance.tooltip_mapper.Get(val.GetType(), null);
         if (cand) return cand;
         var tt = TooltipSwitch.instance.GetTooltipFor(val);

         cand = instance.tooltip_mapper_2.Get(tt, null);
         if (cand != null) return cand;

         var node = instance.MakeBridgePrefab(tt);

         instance.tooltip_mapper[val.GetType()] = node;
         instance.tooltip_mapper_2[tt] = node;

         // Console.print($"Make long tooltip {val}!");

         return node;
      }
   }

   public static UnityBridgeBuilder instance;

   private void Awake() {
      instance = this;
   }

   public Transform transform_parse;


   public UnityBridge result;

   void Print(ModelNode n, int i, List<string> acc) {

      if (n.symbol != null) {
         acc.Add(' '.ToCharString(i * 3) + $"{n.name}");
      }

      foreach (var ch in n.child) {
         Print(ch, i + 1, acc);
      }
   }

   void Print(ModelNode top_node) {
      List<string> acc = new List<string>();
      Print(top_node, 0, acc: acc);
      Console.print(acc.Join("\n"));
   }

   void AttachBridges(ModelNode n) {
      foreach (var ch in n.child) {
         AttachBridges(ch);
      }
      var t = n.unity_source;

      var br = t.gameObject.AddComponent<UnityBridge>();
      n.bridge = br;
      foreach (var ch in n.child) {
         br.children.Add(ch.bridge);
      }
      br.value_holders = n.value_holders.ToArray();

      br.symbol_part = n.symbol;
      if (br.symbol_part != null) {

         var bh = UIOperations.GetSymbolFor(br.symbol_part);
         br.special_symbol = bh;
         bh?.Compile(br);
         br.fetch_part = br.name.Substring(br.symbol_part.Length);
         int arr = br.fetch_part.IndexOf("->");
         if (arr > 0) {
            br.sub_name = br.fetch_part.Substring(arr + 2);
            br.fetch_part = br.fetch_part.Substring(0, arr);
         }
            // Console.print(br.symbol_part, br.fetch_part);
         }
   }

   public ModelNode Parse(Transform prefab_transform) {

      var tr = Instantiate(prefab_transform, transform);

      var top_node = UIParser.CreateTopNode(tr);

      UIParser.Visit_AnnotatedUI_2(tr, top_node);

      AttachBridges(top_node);

      top_node.bridge.gameObject.SetActive(false);

      return top_node;
   }

   public UnityBridge MakeBridgePrefab(Transform prefab_transform) {
      var cp = Instantiate(prefab_transform, this.transform);
      var res = Parse(cp);

      var bridge = res.bridge;

      bridge.gameObject.SetActive(false);

      return bridge;
   }

}