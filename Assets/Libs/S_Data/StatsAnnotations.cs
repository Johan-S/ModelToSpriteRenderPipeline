using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


public static class Stat {

   public static void Set<T>(T t, string name, object value) {
      Field<T>(name).SetValue(t, value);
   }
   public static FieldInfo Field<T>(string name) {
      return FieldInfos<T>()[name];
   }
   
   public static Dictionary<string, FieldInfo> FieldInfos<T>() {
      return StatsAnnotations.StatFieldInfos<T>();
   }
}


[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class StatAttribute : Attribute {
   public StatAttribute(){}

   public StatAttribute(string name) {
      this.name = name;
   }

   public string name;
   public string value_pattern;

   public string tooltip;

   public bool embed;

   public bool percent_change;
   public bool absolute_change;
   public bool big_integer_string;
   public bool ignore_zero;
   public bool named_link;

   public int denominator;

   public int column_width;
}
public static class StatsAnnotations {

   public static Dictionary<string, FieldInfo> StatFieldInfos<T>() {
      return StatsFields_DUMMY<T>.FieldDict;
   }
   
   public static IEnumerable<string> DefaultStatFields(Type ot) {
      var class_attr = ot.GetCustomAttribute<StatAttribute>();

      foreach (var f in ot.GetFields_Fast()) {
         var sa = f.GetCustomAttribute<StatAttribute>() ?? class_attr;
         if (sa != null) {
            
            string stat_name = f.Name;
            if (sa.name != null) {
               stat_name = sa.name;
            }

            yield return stat_name;
         }
      }
   }

   public static IEnumerable<KeyVal> DefaultStats(object o) {
      var ot = o.GetType();

      var class_attr = ot.GetCustomAttribute<StatAttribute>();

      foreach (var f in ot.GetFields_Fast()) {
         var sa = f.GetCustomAttribute<StatAttribute>() ?? class_attr;
         if (sa != null) {
            string stat_name = f.Name;
            var oval = f.GetValue_Slow(o);
            if (sa.name != null) {
               stat_name = sa.name;
            }

            if (f.FieldType == typeof(string)) {
               

               if (sa.value_pattern != null) {
                  yield return new KeyVal(stat_name.ToTitleCase(), string.Format(sa.value_pattern, oval ?? ""));
               } else {
                  yield return new KeyVal(stat_name.ToTitleCase(), oval);
               }
            } else if (oval != null) {
               {
                  if (oval is IEnumerable ie) {
                     foreach (var val in ie) {
                        if (val == null) continue;
                        if (val is bool bval) {
                           if (bval) {
                              yield return new KeyVal(stat_name.ToTitleCase(), "");
                           }
                        } else {
                           if (val is int ival) {
                              if (sa.ignore_zero && ival == 0) continue;
                           }

                           if (sa.named_link) {
                              var na = (Named)val;
                              yield return new KeyVal(stat_name.ToTitleCase(), na.name).WithTooltip(val);
                           } else {
                              yield return new KeyVal(stat_name.ToTitleCase(), val);
                           }
                        }
                     }
                  } else {
                     object tt = null;
                     var val = oval;
                     if (val is bool bval) {
                        if (bval) {
                           yield return new KeyVal(stat_name.ToTitleCase(), "").WithTooltip(tt);
                        }
                     } else {
                        if (val is int ival) {
                           if (sa.ignore_zero && ival == 0) continue;

                           if (sa.denominator != 0) {
                              int x = ival * 10 / sa.denominator;
                              int r = x % 10;
                              int l = x / 10;
                              if (r != 0) {
                                 yield return new KeyVal(stat_name.ToTitleCase(), $"{l}.{r}").WithTooltip(tt);
                              } else {
                                 yield return new KeyVal(stat_name.ToTitleCase(), $"{l}").WithTooltip(tt);
                              }

                              continue;
                           }

                           if (sa.percent_change) {
                              yield return new KeyVal(stat_name.ToTitleCase(), $"{ival.SignedString()}%")
                                 .WithTooltip(tt);
                              continue;
                           }

                           if (sa.absolute_change) {
                              yield return new KeyVal(stat_name.ToTitleCase(), $"{ival.SignedString()}")
                                 .WithTooltip(tt);
                              continue;
                           }

                           if (sa.big_integer_string) {
                              yield return new KeyVal(stat_name.ToTitleCase(), $"{ival.ToBigString()}").WithTooltip(tt);
                              continue;
                           }
                        }

                        if (sa.value_pattern != null) {
                           yield return new KeyVal(stat_name.ToTitleCase(), string.Format(sa.value_pattern, val));
                        } else {
                           yield return new KeyVal(stat_name.ToTitleCase(), val);
                        }
                     }
                  }
               }
            }
         }
      }
   }


   static class StatsFields_DUMMY<T> {
      
      public static Dictionary<string, FieldInfo> FieldDict = InitFieldTypes();

      static Dictionary<string, FieldInfo> InitFieldTypes() {

         var t = typeof(AnimationBundle);
         var stat_names = StatsAnnotations.DefaultStatFields(t);

         var d = stat_names.ToDictionary(x => x, x => t.GetField(x));


         return d;

      }
   }
}