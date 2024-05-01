using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Object;


public class FieldStorageMember {
   public string Name => field?.Name ?? property?.Name;

   public System.Type FieldType => field?.FieldType ?? property?.PropertyType;

   public FieldInfo field;
   public PropertyInfo property;
}

public class GetterSetterContainer {
   public string name;
   public System.Type field_type;

   public System.Func<object, object> getter;
   public System.Action<object, object> setter;
}

public static class AnnotatedUI {
   static IEnumerable<FieldStorageMember> GetFieldStorageMembers_impl(System.Type t, BindingFlags? flags) {
      BindingFlags b = BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance;
      if (flags is BindingFlags ob) b = ob;

      foreach (var f in t.GetFields(b)) {
         yield return new FieldStorageMember { field = f };
      }

      foreach (var f in t.GetProperties(b)) {
         if (f.CanWrite && f.CanRead) {
            yield return new FieldStorageMember { property = f };
         }
      }
   }

   static IEnumerable<FieldStorageMember> GetFieldStorageMembers_impl_FieldsOnly(System.Type t, BindingFlags? flags) {
      BindingFlags b = BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance;
      if (flags is BindingFlags ob) b = ob;

      foreach (var f in t.GetFields(b)) {
         yield return new FieldStorageMember { field = f };
      }
   }

   public static FieldStorageMember[]
      GetFieldStorageMembers_FieldsOnly(this System.Type t, BindingFlags? flags = null) {
      return GetFieldStorageMembers_impl_FieldsOnly(t, flags).ToList().ToArray();
   }

   public static FieldStorageMember[] GetFieldStorageMembers(this System.Type t, BindingFlags? flags = null) {
      return GetFieldStorageMembers_impl(t, flags).ToList().ToArray();
   }


   public interface ReverseLinkObject {
      void SetInterfaceObject(Transform o);
   }

   public class ReverseLinkObject_Impl : ReverseLinkObject {
      public Transform linked_transform;
      public void SetInterfaceObject(Transform o) => linked_transform = o;
   }


   public class SettableFlag {
      public SettableFlag WithDisplayname(string name) {
         var a = (SettableFlag)MemberwiseClone();
         a.name = name;
         return a;
      }

      public SettableFlag(object o, string field_name) {
         var t = o.GetType();
         {
            var f = t.GetField(field_name);
            if (f != null) {
               Init(o, f);
               return;
            }
         }
         {
            var f = t.GetProperty(field_name);
            if (f != null) {
               Init(o, f);
               return;
            }
         }
         throw new System.Exception($"Bad flag name: {t}:{field_name}");
      }

      public SettableFlag(object o, PropertyInfo field) {
         Init(o, field);
      }

      public SettableFlag(object o, FieldInfo field) {
         Init(o, field);
      }

      private void Init(object o, PropertyInfo field) {
         setter = b => { field.SetValue(o, b); };
         getter = () => { return (bool)field.GetValue(o); };
         name = field.Name.ToTitleCase();
      }

      private void Init(object o, FieldInfo field) {
         setter = b => { field.SetValue(o, b); };
         getter = () => { return (bool)field.GetValue(o); };
         name = field.Name.ToTitleCase();
      }

      public string name;

      System.Action<bool> setter;
      System.Func<bool> getter;

      public bool val {
         get { return getter(); }
         set { setter(value); }
      }
   }

   public static object GetValue_Slow(this FieldInfo fi, object o) {
         return fi.GetValue(o);
   }

   public class ObjectMemberRef {
      public string name;

      public System.Func<object, object> getter;
      public System.Action<object, object> setter;

      public object GetValue(object o) => getter(o);
      public void SetValue(object o, object value) => setter(o, value);

      public bool implemented => getter != null;

      public static implicit operator bool(ObjectMemberRef r) => r?.getter != null;
   }

   public class ObjectFieldRef : ObjectMemberRef {
      public ObjectFieldRef(FieldInfo field) {
         this.field = field;
         this.getter = field.GetValue_Slow;
         this.setter = field.SetValue;
      }

      FieldInfo field;
   }

   public class ObjectPropertyRef : ObjectMemberRef {
      public ObjectPropertyRef(PropertyInfo field) {
         this.field = field;
         this.getter = field.GetValue;
         this.setter = field.SetValue;
      }

      PropertyInfo field;
   }

   public static ObjectMemberRef GetFieldFor(System.Type t, string name) {
      foreach (var m in t.GetMember(name)) {
         if (m is FieldInfo fi) {
            return new ObjectFieldRef(fi) {
               name = name,
            };
         }

         if (m is PropertyInfo p) {
            return new ObjectPropertyRef(p) {
               name = name,
            };
         }
      }

      return new ObjectMemberRef { name = name };
   }


   public class TypeParserClass {
      public TypeParserClass(System.Type t) {
         this.type = t;
      }

      public readonly System.Type type;

      public ObjectMemberRef GetField(string name) {
         if (!field_ref.TryGetValue(name, out var res)) {
            res = GetFieldFor(type, name);
            if (getter_setter_ref != null) {
               var gs = getter_setter_ref.Get(name, null);
               if (gs != null) {
                  res.getter = gs.getter;
                  res.setter = gs.setter;
               }
            }

            field_ref[name] = res;
         }

         return res;
      }

      public void SetGetterSetters(List<GetterSetterContainer> gsc) {
         getter_setters = gsc;
         getter_setter_ref = new Dictionary<string, GetterSetterContainer>();
         foreach (var fr in gsc) {
            getter_setter_ref[fr.name] = fr;

            var field = field_ref.Get(fr.name, null);
            if (field != null) {
               field.getter = fr.getter;
               field.setter = fr.setter;
            }
         }
      }

      List<GetterSetterContainer> getter_setters;

      Dictionary<string, GetterSetterContainer> getter_setter_ref;

      FieldInfo[] fields;

      public FieldInfo[] GetFields() {
         if (fields == null) {
            fields = type.GetFields();
         }

         return fields;
      }


      Dictionary<string, ObjectMemberRef> field_ref = new Dictionary<string, ObjectMemberRef>();
   }

   public static T GetValue<T>(this FieldInfo f) {
      return (T)f.GetValue(null);
   }

   public static FieldInfo[] GetFields_Fast(this System.Type t) {
      return t.GetTypeParser().GetFields();
   }

   static Dictionary<System.Type, TypeParserClass> type_parsers = new Dictionary<System.Type, TypeParserClass>();

   public static TypeParserClass GetTypeParser(this System.Type t) {
      if (!type_parsers.TryGetValue(t, out var res)) {
         res = new TypeParserClass(t);
         type_parsers[t] = res;
      }

      return res;
   }

   public static ObjectMemberRef GetField_Fast(this System.Type t, string name) {
      var parser = t.GetTypeParser();
      return parser.GetField(name);
   }


   public interface StatsObject {
      IEnumerable<KeyVal> stats { get; }
   }

   public interface ValueHolder {
      object get();
      void set(object o);
   }

   public class MonoString {
      public string text;
   }

   public interface TooltipGen {
      RectTransform GenTooltip(RectTransform in_parent);
   }

   public class NoDisplay : System.Attribute {
   }

   public interface TooltipValueWrapper {
      object wrapped_tooltip { get; }
   }

   public class ClickableObject : TooltipValueWrapper {
      public object val => o;

      public object wrapped_tooltip => o;

      public object o;
      public System.Action on_click;

      public System.Action invoke => can_activate() ? on_click : null;

      public System.Func<bool> can_activate = () => true;

      public System.Action activate => invoke;
   }

   public class ClickableButton {
      public string name;

      public System.Action invoke;
      public System.Action activate => invoke;

      public ClickableButton(string name, System.Action invoke) {
         this.name = name;
         this.invoke = invoke;
      }
   }

   public static ClickableObject MakeClickable<T>(T o, System.Action<T> call) {
      Debug.Assert(o != null);
      if (call == null) return new ClickableObject { o = o };
      return new ClickableObject { o = o, on_click = () => call(o) };
   }


   public class UIBoxLayoutVars {
      public UIBoxLayoutVars() {
      }

      public UIBoxLayoutVars(RectTransform rt) {
         pivot = rt.pivot;
         anchor_min = rt.anchorMin;
         anchor_max = rt.anchorMax;
         size = rt.rect.size;
      }

      public Vector2? pivot;
      public Vector2? anchor_min;
      public Vector2? anchor_max;
      public Vector2? size;

      public void ApplyTo(RectTransform rt) {
         rt.pivot = pivot ?? rt.pivot;
         rt.anchorMin = anchor_min ?? rt.anchorMin;
         rt.anchorMax = anchor_max ?? rt.anchorMax;

         if (size is Vector2 sz) {
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, sz.x);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sz.y);
         }
      }
   }

   public interface ColoredSprite {
      Sprite GetSprite();
      Color GetColor();
      Vector2? GetOffset();
      Vector2? MaybeGetSize();
      Vector2? GetUIPivot();

      bool UseAlpha();

      bool Dynamic();

      UIBoxLayoutVars BoxLayout();
   }

   public class ColoredSpriteImpl : ColoredSprite {
      public Sprite sprite;
      public Color color = new Color(1, 1, 1, 1);
      public Vector2? offset;

      public UIBoxLayoutVars box_layout_vars;
      public bool UseAlpha() => false;

      public Vector2? size;
      public Sprite GetSprite() => sprite;
      public Color GetColor() => color;
      public Vector2? GetOffset() => offset;
      public Vector2? MaybeGetSize() => size;

      public bool Dynamic() => false;

      public Vector2? GetUIPivot() => null;

      public UIBoxLayoutVars BoxLayout() => box_layout_vars;
   }

   public static void ReVisitAll(Transform canvas) {
      var els = canvas.GetComponentsInChildren<GeneratedElements>();
      foreach (var e in els) {
         if (e) ReVisit(e.transform);
      }
   }

   public static void MarkDirty_UI(Transform tr) {
      PropagateDirty(tr);
   }

   public static void PropagateDirty(Transform tr) {
      var acc = tr.gameObject.GetComponent<GeneratedElements>();
      if (acc) acc.dirty = true;
      if (tr.parent) PropagateDirty(tr.parent);
   }

   public static void ReVisit(Transform tr) {
      var acc = tr.GetComponentInParent_Safe<GeneratedElements>();
      if (acc == null) return;
      Visit(acc.transform, acc.original_val, acc.original_visit_reason);
   }

   public static void VisitIfActive(Transform tr, object o, string visit_reason) {
      if (tr.gameObject.activeInHierarchy) {
         Visit(tr, o, visit_reason);
      }
   }

   public static void Visit_IfDifferent(Transform tr, object o, string visit_reason, System.Action on_revisit = null) {
      var acc = tr.GetComponent<GeneratedElements>();

      if (acc?.original_val == o) {
         return;
      }

      Visit(tr, o, visit_reason, on_revisit);
   }

   public static void Visit(Transform tr, object o, string visit_reason="NoReason", System.Action on_revisit = null,
      bool eager = false) {
      var acc = tr.gameObject.ForceComponent<GeneratedElements>();
      acc.original_visit_reason = visit_reason;
      acc.original_val = o;

      acc.dirty = true;

      if (on_revisit != null) {
         acc.OnDirty(on_revisit);
      }

      if (currently_visiting != 0 || eager) {
         EagerRevisit(tr, o, visit_reason, acc);
      }
      // EagerRevisit(tr, o, visit_reason, acc);
   }


   static int currently_visiting = 0;

   public static void EagerVisit(Transform tr, object o, string visit_reason, System.Action on_revisit = null) {
      Visit(tr, o, visit_reason, on_revisit, eager: true);
   }

   public static void EagerRevisit_IfDirty(Transform tr) {
      var acc = tr.GetComponent<GeneratedElements>();
      if (acc.dirty) {
         EagerRevisit(tr, acc.original_val, acc.original_visit_reason, acc);
      }
   }

   public static void EagerRevisit(Transform tr, object o, string visit_reason, GeneratedElements acc) {
      // Debug.Log("Visit!");
      try {
         currently_visiting++;

            acc.Clear();
            acc.dirty = false;

            if (o is DirtyUpdater.DirtyTracker) {
               tr.gameObject.ForceComponent<DirtyUpdater>().Track((DirtyUpdater.DirtyTracker)o);
            }

            bool switch_next = false;
            bool? disable_next = null;
            bool stop = false;
            VisitRef(tr, ref o, acc, ref switch_next, ref disable_next, ref stop);
      }
      finally {
         currently_visiting--;
      }
   }

   static object[] empty_parameters = { };

   static object GetValSingle_OrNull(object o, string field) {
      if (field == "this") return o;
      if (char.IsDigit(field[0])) {
         if (int.TryParse(field, out var index)) {
            if (o is System.Array arr) {
               if (index >= 0 && index < arr.Length) return arr.GetValue(index);
               return null;
            }
            if (o is IList ilist) {
               if (index >= 0 && index < ilist.Count) return  ilist[index];
               return null;
            }

            if (o == null) return null;
         }
         Debug.LogError($"Invalid field name: {field} for type: {o.GetType()}");
         return null;
      }
      var tp = o.GetType();
      var fi = tp.GetField_Fast(field);
      if (!fi) {
         return null;
      }

      return fi.getter(o);
   }

   static object GetValSingle(object o, string field) {
      if (field == "this") return o;
      if (char.IsDigit(field[0])) {
         if (int.TryParse(field, out var index)) {
            if (o is System.Array arr) {
               if (index >= 0 && index < arr.Length) return arr.GetValue(index);
               return null;
            }
            if (o is IList ilist) {
               if (index >= 0 && index < ilist.Count) return  ilist[index];
               return null;
            }

            if (o == null) return null;
         }
         Debug.LogError($"Invalid field name: {field} for type: {o.GetType()}");
         return null;
      }
      var tp = o.GetType();
      var f = tp.GetProperty(field);
      if (f == null) {
         var fi = tp.GetField(field);
         if (fi == null) {
            MethodInfo fu = tp.GetMethod(field);
            if (fu != null) {
               var return_type = fu.ReturnType;
               var param = fu.GetParameters();
               if (return_type != typeof(void) && param.Length == 0) {
                  return fu.Invoke(o, empty_parameters);
               }
            }

            var tn = o.GetType().Name;
            throw new System.Exception($"\"{field}\" doesn't exist on {tn}");
         }

         return fi.GetValue_Slow(o);
      }

      var val = f.GetValue(o);
      return val;
   }

   static void SetValSingle(object o, string field, object val) {
      if (char.IsDigit(field[0])) {
         if (int.TryParse(field, out var index)) {
            if (o is System.Array arr) {
               arr.SetValue(val, index);
               return;
            }
            if (o is IList ilist) {
               ilist[index] = val;
               return;
            }
         }
         Debug.LogError($"Invalid field name: {field} for type: {o.GetType()}");
      }
      var tp = o.GetType();
      var f = tp.GetProperty(field);
      if (f == null) {
         var fi = tp.GetField(field);
         if (fi == null) {
            var tn = o.GetType().Name;
            throw new System.Exception($"\"{field}\" doesn't exist on {tn}");
         }

         if (fi.FieldType.IsEnum && val is string) {
            val = System.Enum.Parse(fi.FieldType, (string)val);
         }

         if (fi.FieldType.Is(typeof(IntCastable)) && val is System.Int32) {
            val = ((IntCastable)fi.FieldType.ConstructEmpty()).FromInt((int)val);
         }

         if (fi.FieldType.Is(typeof(int)) && val is float) {
            float a = (float)val;
            val = (int)a;
         }

         fi.SetValue(o, val);
         return;
      } else {
         var fi = f;
         if (fi.PropertyType.IsEnum && val is string) {
            val = System.Enum.Parse(fi.PropertyType, (string)val);
         }

         if (fi.PropertyType.Is(typeof(IntCastable)) && val is System.Int32) {
            val = ((IntCastable)fi.PropertyType.ConstructEmpty()).FromInt((int)val);
         }

         if (fi.PropertyType.Is(typeof(int)) && val is float) {
            float a = (float)val;
            val = (int)a;
         }

         fi.SetValue(o, val);
      }

      if (f.PropertyType.IsEnum && val is string) {
         val = System.Enum.Parse(f.PropertyType, (string)val);
      }

      f.SetValue(o, val);
   }

   public static object GetVal_External(object o, string field) {
      if (field == "this") return o;
      var fs = field.Split('.');
      foreach (var fi in fs) {
         o = GetValSingle(o, fi);
      }

      return o;
   }


   public static bool FastStartsWith(this string a, string b) {
      if (a.IsEmpty()) return b.IsEmpty();
      if (a.Length < b.Length) return false;
      for (int i = 0; i < b.Length; i++) {
         if (a[i] != b[i]) return false;
      }

      return true;
   }

   public static object GetVal_External_UI(object o, string field, GeneratedElements acc) {
      if (field == "this") return o;
      if (field.FastStartsWith("ui.")) {
         return acc.ui_vals.Get(field, null);
      }

      if (field.FastStartsWith("croot.")) {
         o = acc.original_val;
         field = field.Substring("croot.".Length);
      }

      var fs = field.Split('.');
      foreach (var fi in fs) {
         o = GetValSingle(o, fi);
      }

      return o;
   }


   public static object GetVal_External_UI_OrNull(object o, string field, GeneratedElements acc) {
      if (field == "this") return o;
      if (field.FastStartsWith("ui.")) {
         return acc.ui_vals.Get(field, null);
      }
      if (field.FastStartsWith("croot.")) {
         o = acc.original_val;
         field = field.Substring("croot.".Length);
      }

      if (field.Contains(".")) {
         var fs = field.Split('.');
         foreach (var fi in fs) {
            if (o == null) return null;
            o = GetValSingle_OrNull(o, fi);
         }
      } else {
         var res = GetValSingle_OrNull(o, field);
         return res;
      }

      return o;
   }

   private static object GetVal(object o, string field, GeneratedElements acc) {
      if (field == "this") return o;
         return GetVal_External_UI(o, field, acc);
   }

   private static object GetVal_OrNull(object o, string field, GeneratedElements acc) {
      if (field == "this") return o;
         return GetVal_External_UI_OrNull(o, field, acc);
   }

   public static void SetVal(object o, string field, object val, GeneratedElements acc) {
      if (field.FastStartsWith("ui.")) {
         acc.ui_vals[field] = val;
         return;
      }
      if (field.FastStartsWith("croot.")) {
         o = acc.original_val;
         field = field.Substring("croot.".Length);
      }

      var fs = field.Split('.');
      var last = fs[fs.Length - 1];
      foreach (var fi in fs) {
         if (fi == last) {
            SetValSingle(o, fi, val);
         } else {
            o = GetValSingle(o, fi);
         }
      }
   }

   public static bool CastBool(object val) {
      if (val is null) return false;
      {
         if (val is ICollection ic) {
            return ic.Count > 0;
         }
      }
      {
         if (val is bool ic) {
            return ic;
         }
      }
      {
         if (val is IEnumerable) {
            var en = (IEnumerable)val;
            bool ok = en.GetEnumerator().MoveNext();
            return ok;
         }
      }
      return true;
   }

   static bool HasValSingle(object o, string field) {
      try {
         if (field == "this") return CastBool(o);
         if (o == null) return false;
         var tp = o.GetType();
         var f = tp.GetProperty(field);
         if (f == null) {
            var fi = tp.GetField(field);
            if (fi == null) return false;
            return CastBool(fi.GetValue_Slow(o));
         }

         var val = f.GetValue(o);
         return CastBool(val);
      }
      catch (System.Exception e) {
         Debug.Log($"{o.GetType().Name}.{field}, {e}");
      }

      return false;
   }

   static bool HasVal_External(object o, string field, GeneratedElements acc) {
      if (field == "this") return CastBool(o);
      if (field.FastStartsWith("ui.")) {
         return CastBool(acc.ui_vals.Get(field, null));
      }
      if (field.FastStartsWith("croot.")) {
         o = acc.original_val;
         field = field.Substring("croot.".Length);
      }

      var fs = field.Split('.');
      foreach (var fi in fs) {
         if (!HasValSingle(o, fi)) return false;
         o = GetValSingle(o, fi);
      }

      var res = CastBool(o);
      return res;
   }

   private static bool HasVal(object o, string field, GeneratedElements acc) {
      var t = o.GetType();
      // var fn = $"{t.Name}.{field}";

      // var bench = BenchHolder(t.Name, t.FullName);

      try {
         var val = GetVal_OrNull(o, field, acc);
         return CastBool(val);
      }
      catch (System.Exception e) {
         Debug.LogError($"Bad has val: {e}");
         return false;
      }
   }

   private static bool HasVal_Slow(object o, string field, GeneratedElements acc) {
      return HasVal_External(o, field, acc);
   }

   public struct LazyGetHasVal {
      public bool has_val;
      public object val;
   }

   public static LazyGetHasVal LazyGet(object o, string field) {
      if (field == "this") return new LazyGetHasVal { has_val = CastBool(o), val = o };
      var fs = field.Split('.');
      foreach (var fi in fs) {
         if (!HasValSingle(o, fi)) return new LazyGetHasVal { has_val = false, val = null };
         o = GetValSingle(o, fi);
      }

      var res = CastBool(o);
      return new LazyGetHasVal { has_val = res, val = o, };
   }


   static void Continue(Transform tr, ref object o, GeneratedElements acc) {
      {
         var sc = tr.GetComponent<UISubComponent>();
         if (sc && sc.GetComponent<GeneratedElements>() != acc) {
            sc.SetValue(o);
            return;
         }
      }
      bool switch_next = false;
      bool? disable_next = null;
      bool stop = false;
      foreach (var ch in tr.ChildList()) {
         VisitRef(ch, ref o, acc, ref switch_next, ref disable_next, ref stop);
         if (stop) break;
      }
   }

   static void VisitRef(Transform tr, ref object o, GeneratedElements acc, ref bool switch_next, ref bool? disable_next,
      ref bool stop) {
      VisitRef_Impl(tr, ref o, acc, ref switch_next, ref disable_next, ref stop);
   }

   static void VisitRef_Impl(Transform tr, ref object o, GeneratedElements acc, ref bool switch_next,
      ref bool? disable_next, ref bool stop) {
      string tr_name = tr.name;

      if (tr_name.FastStartsWith("skip_")) return;
      if (disable_next != null) {
         var dn = (bool)disable_next;
         disable_next = null;
         tr.gameObject.SetActive(!dn);
         if (dn) {
            return;
         }
      }

      if (tr_name == ("else") && !switch_next) {
         switch_next = false;
         return;
      }

      switch_next = false;
      tr.gameObject.ForceComponent<TagButtonRevisit>();
      string name = null;
      try {
         if (tr_name.StartsWith("interactable_", out name)) {
            bool ok = HasVal(o, name, acc);

            var parent = tr;
            while (parent) {
               var cg = parent.GetComponent<CanvasGroup>();
               if (cg) {
                  cg.interactable = ok;
                  break;
               }

               var s = parent.GetComponent<Selectable>();
               if (s) {
                  s.interactable = ok;
                  break;
               }

               parent = parent.parent;
            }

            return;
         }

         if (tr_name.StartsWith("inter_", out name)) {
            bool ok = HasVal(o, name, acc);
            tr.GetComponentInParent_Safe<Selectable>().interactable = ok;
            return;
         }

         if (tr_name.StartsWith("shorttooltip_", out name)) {
            var val = GetVal(o, name, acc);
               var prefab = TooltipSwitch.instance.GetShortTooltipFor(val);
               Debug.Assert(prefab, $"No short tooltip for type: {val.GetType().Name}, full val: {val}");
               var elements = tr.gameObject.ForceComponent<RepElements>();
               Transform sub;
               var expected_name = "skip_" + prefab.name;
               if (elements.elements.Count == 0) {

                  sub = Instantiate(prefab, tr.parent);
                  elements.elements.Add(sub);
                  sub.name = expected_name;
               } else {
                  sub = elements.elements[0];
               }

               if (sub.name != expected_name) {
                  Destroy(sub.gameObject);
                  sub = Instantiate(prefab, tr.parent);
                  elements.elements[0] = sub;
                  sub.name = expected_name;
               }

               if (sub.GetComponent<ContentSizeFitter>()) {
                  sub.GetComponent<ContentSizeFitter>().enabled = false;
               }

               sub.SetSiblingIndex(tr.GetSiblingIndex() + 1);
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

               sub.name = "sub_this";
               VisitRef(sub, ref val, acc, ref switch_next, ref disable_next, ref stop);
               sub.name = expected_name;
               return;
         }

         if (tr_name.StartsWith("tooltip_", out name)) {
            var val = GetVal(o, name, acc);

               var prefab = TooltipSwitch.instance.GetTooltipFor(val);
               var elements = tr.gameObject.ForceComponent<RepElements>();
               Transform sub;
               var expected_name = "skip_" + prefab.name;
               if (elements.elements.Count == 0) {
                  sub = Instantiate(prefab, tr.parent);
                  elements.elements.Add(sub);
                  sub.name = expected_name;
               } else {
                  sub = elements.elements[0];
               }

               if (sub.name != expected_name) {
                  Destroy(sub.gameObject);
                  sub = Instantiate(prefab, tr.parent);
                  elements.elements[0] = sub;
                  sub.name = expected_name;
               }

               if (sub.GetComponent<ContentSizeFitter>()) {
                  sub.GetComponent<ContentSizeFitter>().enabled = false;
               }

               sub.SetSiblingIndex(tr.GetSiblingIndex() + 1);
               if (sub.GetComponent<ValueHolder>() != null) {
                  foreach (var x in tr.GetComponents<ValueHolder>()) x.set(val);
               }

               sub.name = "sub_this";
               VisitRef(sub, ref val, acc, ref switch_next, ref disable_next, ref stop);
               sub.name = expected_name;
               return;
         }

         if (tr_name.StartsWith("this_", out name)) {
            var val = GetVal(o, name, acc);
            o = val;
            if (tr.GetComponent<ValueHolder>() != null) {
               foreach (var x in tr.GetComponents<ValueHolder>()) x.set(o);
            }

            return;
         }

         if (tr_name.StartsWith("rep_", out name)) {
            string sub_name = "x";
            int arr = name.IndexOf("->");
            if (arr > 0) {
               sub_name = name.Substring(arr + 2);
               name = name.Substring(0, arr);
            }

            var val = GetVal(o, name, acc);
            IEnumerable e = (IEnumerable)val;

            if (val == null) {
               e = new List<object>();
            }

               int i = 0;
               var si = tr.GetSiblingIndex();
               var elements = tr.gameObject.ForceComponent<RepElements>();
               foreach (var ob in e) {
                  Transform sub = null;
                  if (i < elements.elements.Count) {
                     sub = elements.elements[i];
                  } else {
                     sub = Instantiate(tr, tr.parent);
                     sub.SetSiblingIndex(si + i + 1);
                     elements.elements.Add(sub);
                     sub.gameObject.SetActive(true);
                     sub.name = $"skip_{name}";
                  }

                  i++;

                  string s = sub.name;
                  sub.name = sub_name;
                  object sub_o = ob;
                  if (ob is DirtyUpdater.DirtyTracker) {
                     sub.gameObject.ForceComponent<DirtyUpdater>().Track((DirtyUpdater.DirtyTracker)ob);
                  }

                  if (sub.GetComponent<ValueHolder>() != null) {
                     foreach (var x in sub.GetComponents<ValueHolder>()) x.set(sub_o);
                  }

                  if (sub_o is ReverseLinkObject sio) {
                     sio.SetInterfaceObject(sub);
                  }

                  VisitRef(sub, ref sub_o, acc, ref switch_next, ref disable_next, ref stop);
                  sub.name = s;
               }

               while (i < elements.elements.Count) {
                  Destroy(elements.elements.Back().gameObject);
                  elements.elements.Pop();
               }

               tr.gameObject.SetActive(false);
               return;
         }

         if (tr.GetComponent<ValueHolder>() != null) {
            foreach (var x in tr.GetComponents<ValueHolder>()) x.set(o);
         }

         if (tr_name.StartsWith("next_if_", out name)) {
            if (HasVal(o, name, acc)) {
               disable_next = false;
            } else {
               disable_next = true;
               switch_next = true;
            }

            tr.gameObject.SetActive(false);
            return;
         }

         if (tr_name.StartsWith("if_", out name)) {
            {
               if (!HasVal(o, name, acc)) {
                  switch_next = true;
                  tr.gameObject.SetActive(false);
                  return;
               }
            }
            tr.gameObject.SetActive(true);
            Continue(tr, ref o, acc);
            return;
         }

         if (tr_name.StartsWith("ifnot_", out name)) {
            {
               if (HasVal(o, name, acc)) {
                  switch_next = true;
                  tr.gameObject.SetActive(false);
                  return;
               }
            }
            tr.gameObject.SetActive(true);
            Continue(tr, ref o, acc);
            return;
         }

         if (tr_name.StartsWith("parent_if_", out name)) {
            if (HasVal(o, name, acc)) {
               tr.parent.gameObject.SetActive(true);
            } else {
               stop = true;
               tr.parent.gameObject.SetActive(false);
            }

            return;
         }

         if (tr_name.StartsWith("sub_if_", out name)) {
            object new_o = null;
            {
               if (HasVal(o, name, acc)) {
                  var val = GetVal(o, name, acc);
                  if (tr.GetComponent<ValueHolder>() != null) {
                     foreach (var x in tr.GetComponents<ValueHolder>()) x.set(val);
                  }

                  if (val is DirtyUpdater.DirtyTracker) {
                     tr.gameObject.ForceComponent<DirtyUpdater>().Track((DirtyUpdater.DirtyTracker)val);
                  }

                  if (val is ReverseLinkObject sio) {
                     sio.SetInterfaceObject(tr);
                  }

                  new_o = val;
                  tr.gameObject.SetActive(true);
               } else {
                  switch_next = true;
                  tr.gameObject.SetActive(false);
                  return;
               }
            }
            Continue(tr, ref new_o, acc);
            return;
         }

         if (tr_name.StartsWith("format_", out name)) {
            var val = GetVal(o, name, acc);
            object[] vals = {val};
            if (!(val is string) && val is IEnumerable ie) {
               vals = ie.ToGeneric().ToArray();
            }
            var f = tr.gameObject.ForceComponent<FormatString>();
            if (f.s == null || f.s.Length == 0) {
               f.s = tr.GetComponent<TMP_Text>().text;
            }

            if (val == null) {
               tr.gameObject.SetActive(false);
            } else {
               tr.GetComponent<TMP_Text>().text = System.String.Format(f.s, vals);
               tr.gameObject.SetActive(true);
            }

            return;
         }

         if (tr_name.StartsWith("opts_", out name)) {
            var val = (IEnumerable<string>)GetVal(o, name, acc);
            var dropdown = tr.GetComponentInParent_Safe<TMP_Dropdown>();
            dropdown.options = val.Map(x => new TMP_Dropdown.OptionData(x));
            var fs = tr.GetComponentInParent_Safe<FieldSetterCallback>();
            fs.acc = acc;
            if (fs) fs.PropagateVal();
            return;
         }

         if (tr_name.StartsWith("val_", out name)) {
            if (!name.Contains(" ")) {
               var fs = tr.gameObject.ForceComponent<FieldSetterCallback>();
               fs.o = o;
               fs.field = name;
               fs.acc = acc;
               fs.PropagateVal();
               {
                  Continue(tr, ref o, acc);
               }
               return;
            }
         }

         if (tr_name.StartsWith("cinvoke_", out name)) {
            var ot = acc.original_val.GetType();

            var holders = tr.GetComponentsInParent_Safe<ValueHolderBase>().Where(x => x.save_in_context);

            var htypes = holders.Select(x => x.get().GetType()).ToArray();
            
            var meth = ot.GetMethod(name);

            if (!meth.CanCastToAction(htypes)) {
               Debug.Log($"Can't invoke {meth.Name} on {", ".Join(htypes)}");
            }

            bool ok = meth != null;
            
            var inter = tr.GetComponentInParent_Safe<Selectable>();
            if (!inter) {
               inter = tr.parent.GetComponent<Selectable>();
            }

            inter.interactable = ok;
            var bp = tr.GetComponent<Button>() ?? tr.parent.GetComponent<Button>();
            bp.interactable = ok;
            if (ok) {
               var c = bp.gameObject.ForceComponent<OnClickCallback>();
               c.action = () => {
                  meth.Invoke(acc.original_val, holders.Select(x => x.get()).ToArray());
               };
            }

            Continue(tr, ref o, acc);
            return;
         }
         if (tr_name.StartsWith("invoke_", out name)) {
            bool ok = HasVal(o, name, acc);
            var inter = tr.GetComponentInParent_Safe<Selectable>();
            if (!inter) {
               inter = tr.parent.GetComponent<Selectable>();
            }

            inter.interactable = ok;
            var bp = tr.GetComponent<Button>() ?? tr.parent.GetComponent<Button>();
            bp.interactable = ok;
            if (ok) {
               System.Action val = (System.Action)GetVal(o, name, acc);
               var c = bp.gameObject.ForceComponent<OnClickCallback>();
               c.action = val;
            }

            Continue(tr, ref o, acc);
            return;
         }

         if (tr_name.StartsWith("title_", out name)) {
            var val = GetVal(o, name, acc);
            if (val == null) {
               tr.gameObject.SetActive(false);
            } else {
               tr.GetComponent<TMP_Text>().text = val.ToString().ToTitleCase();
               tr.gameObject.SetActive(true);
            }

            return;
         }
         if (tr_name.StartsWith("ftext_", out name)) {
            var val = GetVal(o, name, acc);
            if (val == null) {
               tr.gameObject.SetActive(false);
            } else {
               tr.GetComponent<TMP_Text>().text = val.ToString().Replace("_", " ");
               tr.gameObject.SetActive(true);
            }

            return;
         }

         if (tr_name.StartsWith("text_", out name)) {
            var val = GetVal(o, name, acc);
            if (val == null) {
               tr.gameObject.SetActive(false);
            } else {
               tr.GetComponent<TMP_Text>().text = val.ToString();
               tr.gameObject.SetActive(true);
            }

            return;
         }

         if (tr_name.StartsWith("percent_width_", out name)) {
            var val = GetVal(o, name, acc);
            float c = (float)val;
            var p = tr.parent.GetComponent<RectTransform>();
            var pp = p.parent.GetComponent<RectTransform>();
            p.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, pp.rect.width * Mathf.Clamp(c, 0, 1));
            return;
         }

         if (tr_name.StartsWith("color_", out name)) {
            var val = GetVal(o, name, acc);
            Color c = (Color)val;
            var img = tr.GetComponentInParent_Safe<Image>();
            img.color = img.color.KeepAlpha(c);
            return;
         }

         if (tr_name.StartsWith("label_", out name)) {
            var val = GetVal(o, name, acc);
            tr.GetComponent<TMP_Text>().text = $"{name}: {val}";
            return;
         }

         if (tr_name.StartsWith("image_", out name)) {
            var val = GetVal(o, name, acc);
            if (val is ColoredSprite cs) {
                  var dyn = tr.gameObject.ForceComponent<DynamicSpriteUpdater>();
                  var img = tr.GetComponent<Image>();
                  dyn.SetVars(img, cs);
            } else {
                  var dyn = tr.GetComponent<DynamicSpriteUpdater>();

                  if (dyn) {
                     dyn.CleanupSprite();
                     Destroy(dyn);
                  }

                  tr.GetComponent<Image>().sprite = (Sprite)val;
            }

            return;
         }

         if (tr_name.StartsWith("first_", out name)) {
            var val = GetVal(o, name, acc);
            IEnumerable e = (IEnumerable)val;

            var it = e.GetEnumerator();
            if (it.MoveNext()) {
               val = it.Current;
               tr.gameObject.SetActive(true);
               if (tr.GetComponent<ValueHolder>() != null) {
                  foreach (var x in tr.GetComponents<ValueHolder>()) x.set(val);
               }

               if (val is DirtyUpdater.DirtyTracker) {
                  tr.gameObject.ForceComponent<DirtyUpdater>().Track((DirtyUpdater.DirtyTracker)val);
               }

               Continue(tr, ref val, acc);
            } else {
               val = null;
               tr.gameObject.SetActive(false);
            }

            return;
         }

         if (tr_name.StartsWith("sub_", out name)) {
            var val = GetVal(o, name, acc);
            if (tr.GetComponent<ValueHolder>() != null) {
               foreach (var x in tr.GetComponents<ValueHolder>()) x.set(val);
            }

            if (val is DirtyUpdater.DirtyTracker) {
               tr.gameObject.ForceComponent<DirtyUpdater>().Track((DirtyUpdater.DirtyTracker)val);
            }

            if (val is ReverseLinkObject sio) {
               sio.SetInterfaceObject(tr);
            }

            Continue(tr, ref val, acc);
            return;
         }

         {
            Continue(tr, ref o, acc);
         }
      }
      catch (System.NullReferenceException e) {
         throw new System.NullReferenceException($"NULL ##### ==== GameObject: {tr.gameObject}, object: {o}", e);
      }
      catch (System.InvalidCastException e) {
         throw new System.NullReferenceException($"TYPE ##### ==== GameObject: {tr.gameObject}, object: {o}", e);
      }
   }
}