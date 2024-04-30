

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CodeBridge {


   public delegate void FieldSetter(object o, object val);

   public delegate object FieldGetter(object o);

   public class ParsedCodeType {

      public System.Type type;

      public string[] fields_to_take;
      public SpecialFieldClass[] field_specials;
      public FieldSetter[] field_setters;
      public FieldGetter[] field_getters;
   }

   public ParsedCodeType ParseType(System.Type t) {
      var fields = new List<(string, FieldSetter, FieldGetter)>();

      foreach (var f in t.GetFields_Fast()) {
         if (special_field_dict.TryGetValue(f.Name, out var field_special)) {

            var ft = f.FieldType;
            if (!f.FieldType.Is(field_special.type)) {
               Debug.LogError($"Bad field type for {t.Name}.{f.Name}, is {f.FieldType} but wants {field_special.type}");
            }

            FieldSetter fs = f.SetValue;
            FieldGetter fg = f.GetValue_Slow;
            fields.Add((f.Name, fs, fg));
         }
      }
      foreach (var f in t.GetProperties()) {
         if (special_field_dict.TryGetValue(f.Name, out var field_special)) {

            var ft = f.PropertyType;
            if (!ft.Is(field_special.type)) {
               Debug.LogError($"Bad field type for {t.Name}.{f.Name}, is {ft} but wants {field_special.type}");
            }

            FieldSetter fs = f.SetValue;
            FieldGetter fg = f.GetValue;
            fields.Add((f.Name, fs, fg));
         }
      }
      var res = new ParsedCodeType();
      res.type = t;
      res.fields_to_take = new string[fields.Count];
      res.field_setters = new FieldSetter[fields.Count];
      res.field_getters = new FieldGetter[fields.Count];
      res.field_specials = new SpecialFieldClass[fields.Count];

      for (int i = 0; i < fields.Count; ++i) {
         var (n, fs, fg) = fields[i];
         res.fields_to_take[i] = n;
         res.field_setters[i] = fs;
         res.field_getters[i] = fg;
         res.field_specials[i] = special_field_dict[n];
      }

      return res;
   }

   HashSet<string> sf = new HashSet<string>(special_fields);


   public class SpecialFieldClass {

      public string name;

      public System.Type type;

      public bool expected_list;
   }

   static SpecialFieldClass Create<T>(string name, bool list) {
      return new SpecialFieldClass {
         name = name,
         type = typeof(T),
         expected_list = list,
      };
   }


   public static Dictionary<string, SpecialFieldClass> special_field_dict = GetSpecialFields().ToKeyDictUnique(x => x.name);


   public static List<SpecialFieldClass> GetSpecialFields() {

      var res = new List<SpecialFieldClass>();


      res.Add(Create<string>("name", false));
      res.Add(Create<string>("right_side_name", false));
      res.Add(Create<string>("sub_right_side_name", false));
      res.Add(Create<string>("mini_header", false));
      res.Add(Create<string>("mini_right_side_name", false));
      res.Add(Create<string>("right_side_name", false));

      res.Add(Create<IEnumerable<Sprite>>("sprites", list: true));
      res.Add(Create<IEnumerable<KeyVal>>("stats", list: true));
      res.Add(Create<IEnumerable>("sub_tooltips", list: true));
      res.Add(Create<IEnumerable>("sub_short_tooltips", list: true));
      res.Add(Create<IEnumerable<string>>("description", list: true));
      return res;
   }


   public static List<string> special_fields = new List<string> {
     "name",
     "right_side_name",
     "sub_right_side_name",
     "mini_header",
     "mini_right_side_name",
     "sprites",
     "stats",
     "sub_tooltips",
     "sub_short_tooltips",
     "description",
   };
}