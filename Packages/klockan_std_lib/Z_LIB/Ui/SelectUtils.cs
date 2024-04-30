using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using static AnnotatedUI;


public interface SelectElement {

   bool IsSelected {
      get;
   }
   void Toggle();

}
public interface SelectElement<T> : SelectElement {


   T val {
      get;
   }

}

public class MultiSelectClass<T> : IEnumerable<SelectUtils.ToggleSelectObject<T>> {

   public System.Predicate<T> filter;

   public bool Filter(T t) {
      return filter == null || filter(t);
   }

   public IEnumerable<T> selected => elements.Where(x => Filter(x.val) && x.selected).Map(x => x.val);
   public List<SelectUtils.ToggleSelectObject<T>> elements = new List<SelectUtils.ToggleSelectObject<T>>();
   IEnumerator IEnumerable.GetEnumerator() {
      return elements.Where(x => Filter(x.val)).GetEnumerator();
   }

   IEnumerator<SelectUtils.ToggleSelectObject<T>> IEnumerable<SelectUtils.ToggleSelectObject<T>>.GetEnumerator() {
      return elements.Where(x => Filter(x.val)).GetEnumerator();
   }
   public static implicit operator MultiSelectClass<T>(List<T> vals) => SelectUtils.MakeMultiSelectClass(vals);
}
public class SelectClass {
   public bool selected;
   public bool not_selected => !selected;
   public System.Action select => () => {
      selected = !selected;
   };
}
public class CustomSelectClass<T> : IEnumerable<SelectUtils.SelectObject<T>> {

   public System.Predicate<T> filter;
   public System.Predicate<T> is_selected;
   public System.Action<T> toggle;
   public System.Func<T, bool?> filter_switch;

   public System.Predicate<T> toggleable;

   bool filter_bit;

   public bool Filter(T t) {
      if (filter == null || filter(t)) {
         if (filter_switch != null) {
            bool? nb = filter_switch(t);
            if (nb is bool unwr) {
               filter_bit = unwr;
               return true;
            }
         }
         return filter_bit;
      }
      return false;
   }
   public List<T> selected => vals.Where(x => is_selected(x));
   public List<T> vals = new List<T>();
   public List<SelectUtils.SelectObject<T>> elements => GenElements();

   public List<SelectUtils.SelectObject<T>> GenElements() {
      List<SelectUtils.SelectObject<T>> elements = new List<SelectUtils.SelectObject<T>>();

      filter_bit = true;
      foreach (var x in vals) {
         if (Filter(x)) {
            var cv = x;
            elements.Add(new SelectUtils.SelectObject<T> {
               val = cv,
               select_callback = ab => toggle(cv),
               selected_callback = ab => is_selected(cv),
            });
            if (toggleable != null) {
               bool ok = toggleable(cv);
               if (!ok) elements.Back().select_callback = null;
            }
         }
      }
      return elements;
   }

   IEnumerator IEnumerable.GetEnumerator() {
      filter_bit = true;
      return elements.Where(x => Filter(x.val)).GetEnumerator();
   }

   IEnumerator<SelectUtils.SelectObject<T>> IEnumerable<SelectUtils.SelectObject<T>>.GetEnumerator() {
      filter_bit = true;
      return elements.Where(x => Filter(x.val)).GetEnumerator();
   }
}
public class CustomSingleSelectClass<T> : IEnumerable<SelectUtils.SelectObject<T>> {

   public System.Predicate<T> filter;
   public System.Predicate<T> is_selected;
   public System.Action<T> toggle;

   public System.Predicate<T> not_toggleable;
   public bool Filter(T t) {
      return filter == null || filter(t);
   }
   public T selected => vals.Find(x => is_selected(x));
   public List<T> vals = new List<T>();
   public List<SelectUtils.SelectObject<T>> elements => GenElements();

   public List<SelectUtils.SelectObject<T>> GenElements() {
      List<SelectUtils.SelectObject<T>> elements = new List<SelectUtils.SelectObject<T>>();

      foreach (var x in vals) {
         if (filter == null || filter(x)) {
            var cv = x;
            elements.Add(new SelectUtils.SelectObject<T> {
               val = cv,
               select_callback = ab => toggle(cv),
               selected_callback = ab => is_selected(cv),
            });
            if (not_toggleable != null) {
               bool ok = not_toggleable(cv);
               if (!ok) elements.Back().select_callback = null;
            }
         }
      }
      return elements;
   }

   IEnumerator IEnumerable.GetEnumerator() {
      return elements.Where(x => Filter(x.val)).GetEnumerator();
   }

   IEnumerator<SelectUtils.SelectObject<T>> IEnumerable<SelectUtils.SelectObject<T>>.GetEnumerator() {
      return elements.Where(x => Filter(x.val)).GetEnumerator();
   }
}

public class SelectClass<T> : IEnumerable<SelectUtils.SelectObject<T>> {

   public System.Predicate<T> filter;

   public System.Predicate<T> can_toggle;

   public bool Filter(T t) {
      return filter == null || filter(t);
   }
   public bool CanToggle(T t) {
      return can_toggle == null || can_toggle(t);
   }


   public System.Action extra_callback = null;
   public bool can_toggle_off = false;
   public bool can_click_selected = false;
   public System.Action toggle_off = null;

   public SelectUtils.SelectObject<T> ConstructNewElement(T t) {
      var tt = t;
      SelectUtils.SelectObject<T> g = new SelectUtils.SelectObject<T> {
         can_toggle_callback = () => CanToggle(tt),
         selected_callback = x => {
            return selected_comp == x;
         },
         select_callback = x => {
            var val = (SelectUtils.SelectObject<T>)x;
            if (can_click_selected && val == selected_comp) {
               if (can_toggle_off) {
                  selected_comp = new SelectUtils.SelectObject<T> { };
                  toggle_off?.Invoke();
               }
               extra_callback?.Invoke();
            } else if (val != selected_comp) {
               selected_comp = (SelectUtils.SelectObject<T>)x;
               extra_callback?.Invoke();
               SoundManager.instance.Select();
            }
         },
      };
      g.val = t;
      return g;
   }

   public int selected_index => elements.IndexOf(selected_comp);
   public bool any_selected => selected_comp != null;
   public T selected => any_selected ? selected_comp.val : default;
   public SelectUtils.SelectObject<T> selected_comp;
   public List<SelectUtils.SelectObject<T>> elements = new List<SelectUtils.SelectObject<T>>();
   IEnumerator IEnumerable.GetEnumerator() {
      if (selected_comp != null && !Filter(selected_comp.val)) selected_comp = null;
      return elements.Where(x => Filter(x.val)).GetEnumerator();
   }

   IEnumerator<SelectUtils.SelectObject<T>> IEnumerable<SelectUtils.SelectObject<T>>.GetEnumerator() {
      if (selected_comp != null && !Filter(selected_comp.val)) selected_comp = null;
      return elements.Where(x => Filter(x.val)).GetEnumerator();
   }

   public IEnumerable<T> values => elements.Select(x => x.val);

   public void Remove(T val) {
      var r = elements.Find(x => x.val.Equals(val));
      if (r != null) {
         elements.Remove(r);
         if (selected_comp == r) selected_comp = null;
      }
   }

   public void AddItem(T val) {
      SelectUtils.SelectObject<T> g = ConstructNewElement(val);
      g.val = val;

      elements.Add(g);
   }

   public static implicit operator SelectClass<T>(List<T> vals) => SelectUtils.MakeSelectClass(vals);
   public static implicit operator SelectClass<T>(T[] vals) => SelectUtils.MakeSelectClass(vals);
}

public interface SelectableObjectMarker {
}
public static class SelectUtils {

   public static CustomSelectClass<T> MakeCustomSelect<T>(IList<T> elements,
       System.Predicate<T> is_selected,
       System.Action<T> toggle, System.Predicate<T> filter = null) {
      return new CustomSelectClass<T> {
         vals = elements.ToList(),
         filter = filter,
         toggle = toggle,
         is_selected = is_selected,
      };
   }
   public static CustomSelectClass<T> MakeCustomSelect<T>(List<T> elements,
       System.Predicate<T> is_selected,
       System.Action<T> toggle, System.Predicate<T> filter = null) {
      return new CustomSelectClass<T> {
         vals = elements.ToList(),
         filter = filter,
         toggle = toggle,
         is_selected = is_selected,
      };
   }

   public static CustomSelectClass<T> MakeCustomSelect<T>(
       System.Predicate<T> is_selected,
       System.Action<T> toggle, System.Predicate<T> filter = null) {
      return new CustomSelectClass<T> {
         vals = new List<T>(),
         filter = filter,
         toggle = toggle,
         is_selected = is_selected,
      };
   }

   public static CustomSingleSelectClass<T> MakeCustomSingleSelect<T>(
      List<T> vals,
       System.Predicate<T> is_selected,
       System.Action<T> toggle, System.Predicate<T> filter = null) {
      return new CustomSingleSelectClass<T> {
         vals = vals,
         filter = filter,
         toggle = toggle,
         is_selected = is_selected,
      };
   }

   public static CustomSingleSelectClass<T> MakeCustomSingleSelect<T>(
       System.Predicate<T> is_selected,
       System.Action<T> toggle, System.Predicate<T> filter = null) {
      return new CustomSingleSelectClass<T> {
         vals = new List<T>(),
         filter = filter,
         toggle = toggle,
         is_selected = is_selected,
      };
   }


   public static MultiSelectClass<T> MakeMultiSelectClass<T>(IEnumerable<T> data, System.Action<ToggleSelectObject<T>> on_toggle = null, System.Func<ToggleSelectObject<T>, bool> can_toggle=null) {
      MultiSelectClass<T> res = new MultiSelectClass<T>();
      foreach (var t in data) {
         res.elements.Add(new ToggleSelectObject<T> { val = t, on_toggle = on_toggle, can_toggle = can_toggle});
      }
      return res;
   }

   public static SelectClass<T> MakeSelectClass<T>(IList<T> data, System.Action extra_callback = null, bool can_toggle_off = false, System.Action toggle_off = null, bool can_click_selected = false, int default_selected=0, System.Predicate<T> filter=null) {
      SelectClass<T> res = new SelectClass<T>();
      res.filter = filter;

      res.extra_callback = extra_callback;
      res.can_toggle_off = can_toggle_off;
      res.can_click_selected = can_click_selected || can_toggle_off;
      res.toggle_off = toggle_off;

      foreach (var t in data) {
         var tt = t;
         SelectObject<T> g = res.ConstructNewElement(t);
         res.elements.Add(g);
      }

      res.selected_comp = res.elements.Get(default_selected, null);
      return res;
   }
   public class GlobalSelectClass : SelectElement {

      public System.Func<bool> can_toggle_callback;

      public System.Func<GlobalSelectClass, bool> selected_callback;
      public System.Action<GlobalSelectClass> select_callback;
      public bool selected => selected_callback(this);

      public bool not_selected => !selected;

      public System.Action select {
         get {
            if (select_callback == null) return null;
            if (can_toggle_callback != null && !can_toggle_callback.Invoke()) return null;
            return () => select_callback(this);
         }
      }

      public bool IsSelected => selected;

      public void Toggle() {
         select();
      }
   }
   public class SelectObject<T> : GlobalSelectClass, SelectableObjectMarker, SelectElement<T>, TooltipValueWrapper {
      public T val {
         get;set;
      }
      

      public override string ToString() {
         return $"Select {val}";
      }

      public object wrapped_tooltip => val;
   }
   public class ToggleSelectObject<T> : SelectableObjectMarker {
      public T val;
      public bool selected;
      public bool not_selected => !selected;

      public System.Action<ToggleSelectObject<T>> on_toggle;

      public System.Func<ToggleSelectObject<T>, bool> can_toggle;

      public System.Action select {
         get {
            if (can_toggle != null && !can_toggle(this))
               return null;
            return () => {
               selected = !selected;
               on_toggle?.Invoke(this);
               SoundManager.instance.Select();
            };
         }
      }
      public System.Action select_silent => () => selected = !selected;

      public override string ToString() {
         return $"Select {val}";
      }
   }
}
