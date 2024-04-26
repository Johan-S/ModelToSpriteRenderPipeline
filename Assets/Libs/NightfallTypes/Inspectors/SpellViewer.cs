using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using static DataTypes;
using static EngineDataInit;
using Object = UnityEngine.Object;

public class SpellViewer : MonoBehaviour {
   public Transform to_visit;
   public EngineDataHolder.BaseTypes base_types;

   public class RowSep {
      public string name;
      public object field_values;
   }

   public SelectClass<string> tabs = new[] { "Table", "Cards" };

   public bool view_cards => tabs.selected_index == 1;

   public bool view_table => tabs.selected_index == 0;

   public IEnumerable<MagicSpell> spells_basic_old {
      get {
         foreach (var s in base_types.spells) {
            if (old_spell_filter.selected.All(x => Std.IsNotEmpty(x.GetValue(s)))) {
               yield return s;
            }
         }
      }
   }

   public IEnumerable<object> spells_old_grouped {
      get {
         var fils = old_spell_filter.selected.ToArray();
         if (fils.Length == 0) {
            foreach (var s in base_types.spells) yield return s;
         } else {
            var afilt = base_types.spells.Where(s => fils.All(x => Std.IsNotEmpty(x.GetValue(s))));

            var gs = afilt.Group(x => fils.ArrayMap(fi => fi.GetValue(x) ?? "").join(",")).Sorted(x => x.Key);

            foreach (var kv in gs) {
               yield return new RowSep() { name = $"<color=#aaa>{kv.Key}</color>" };


               foreach (var s in kv.Value) {
                  yield return s;
               }
            }
         }
      }
   }

   public IEnumerable<object> spells {
      get { return spells_basic; }
   }

   Predicate<T> MakeFiltercat<T>(MultiSelectClass<MyFilter<T>> st, bool all_if_none) {
      var l = st.selected.ToArray();
      if (l.Length == 0) return t => all_if_none;
      return t => l.Any(x => x.filter(t));
   }

   public IEnumerable<MagicSpell> spells_basic {
      get {
         var arrs = category_filters.Select(x => x.selected.ToArray()).Where(x => x.Length > 0).ToArray();
         Predicate<MagicSpell> spf = spell => arrs.Length == 0 ? all_if_no_category : arrs.All(x => x.Any(f => f.filter(spell)));
         foreach (var s in base_types.spells) {
            if (spell_filter_single.selected.All(x => x.filter(s))) {
               if (spf(s)) yield return s;
            }
         }
      }
   }

   public class MyFilter<T> {
      public string Name => descr;


      public Predicate<T> filter;
      public string descr;

      public MyFilter(string descr, Predicate<T> filter) {
         this.filter = filter;
         this.descr = descr;
      }

      public override string ToString() {
         return descr;
      }
   }

   public MultiSelectClass<FieldInfo> old_spell_filter;

   public MultiSelectClass<MyFilter<MagicSpell>> spell_filter_single;

   public List<MultiSelectClass<MyFilter<MagicSpell>>> category_filters = new();

   public bool all_if_no_category;


   public IEnumerable<object> objects => spells;

   public IEnumerable<MultiSelectClass<MyFilter<MagicSpell>>> objects_filters {
      get {
         yield return spell_filter_single;
         foreach (var cf in category_filters) {
            yield return cf;
         }
      }
   }

   public string[] column_names => DataTypes.MagicSpell.field_names;

   Action on_destroy;

   IEnumerator Start() {
      
      
      
      old_spell_filter = SelectUtils.MakeMultiSelectClass(DataTypes.MagicSpell.field_infos);


      List<MyFilter<MagicSpell>> filters = new() {
         new("Combat", s => s.combat_spell == "C"),
      };

      {
         MultiSelectClass<MyFilter<MagicSpell>> category_filter = null;
         category_filter = SelectUtils.MakeMultiSelectClass(gear_data.magic_spells.Select(x => x.School).Unique()
            .Select(x => new MyFilter<MagicSpell>(x, s => s.School == x)), on_toggle: msp => {
            if (!InputExt.control) {
               category_filter!.elements.ForEach(x => x.selected = false);
               msp.selected = true;
            }
         });
         
         // category_filters.Add(category_filter);
      }
      {
         MultiSelectClass<MyFilter<MagicSpell>> category_filter = null;
         category_filter = SelectUtils.MakeMultiSelectClass(gear_data.magic_spells.Select(x => x.Path1.Split("+")[0]).Unique()
            .Select(x => new MyFilter<MagicSpell>(x, s => s.Path1.FastStartsWith(x))), on_toggle: msp => {
            if (!InputExt.control) {
               category_filter!.elements.ForEach(x => x.selected = false);
               msp.selected = true;
            }
         });
         
         category_filters.Add(category_filter);
      }
      {
         MultiSelectClass<MyFilter<MagicSpell>> category_filter = null;
         category_filter = SelectUtils.MakeMultiSelectClass(gear_data.magic_spells.Select(x => x.Category).Unique()
            .Select(x => new MyFilter<MagicSpell>(x, s => s.Category == x)), on_toggle: msp => {
            if (!InputExt.control) {
               category_filter!.elements.ForEach(x => x.selected = false);
               msp.selected = true;
            }
         });
         
         category_filters.Add(category_filter);
      }


      spell_filter_single = SelectUtils.MakeMultiSelectClass(filters);
      spell_filter_single.elements[0].select_silent();
      base_types = EngineDataInit.base_types;


      tabs.selected_comp =
         tabs.Find(x => x.val == PlayerPrefs.GetString(SPELL_FILTER_TAB_SV, "")) ?? tabs.selected_comp;
      
      
      string ofilter = PlayerPrefs.GetString(SPELL_FILTER_SV, "Combat");

      var to_select = ofilter.SplitE(",").ToHashSet();

      foreach (var of in objects_filters.FlatMap(x=>x)) {
         of.selected = to_select.Contains(of.val.descr);
      }
      
      AnnotatedUI.Visit(to_visit, this);


      on_destroy += () => {
         string filter_str = "";
         try {
            var strs = objects_filters.FlatMap(x => x).Where(x => x.selected).join(",", x => x.val.descr);

            filter_str = strs;
         }
         catch (Exception e) {
            Debug.LogError(e);
         }
         PlayerPrefs.SetString(SPELL_FILTER_SV, filter_str);
         try {
            PlayerPrefs.SetString(SPELL_FILTER_TAB_SV, tabs.selected);
         }
         catch (Exception e) {
            Debug.LogError(e);
         }
      };

      yield return null;



      var txts = Object.FindObjectsOfType<TMP_Text>(true);
   }
   const string SPELL_FILTER_SV = "EDITOR_SpellViewer_FilterSave";
   const string SPELL_FILTER_TAB_SV = "EDITOR_SpellViewer_TabSave";

   void OnDestroy() {
      on_destroy?.Invoke();
      
      
   }

   // Update is called once per frame
   void Update() {
   }
}