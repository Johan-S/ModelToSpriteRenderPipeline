using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class EnumUtil {

   public static IEnumerable<T> Values<T>() {
      foreach (var t in Enum.GetValues(typeof(T))) {
         yield return (T)t;
      }
   }

   public static T Parse<T>(string s) where T : Enum {
      return (T)Enum.Parse(typeof(T), s);
   }

   public static T Parse<T>(string s, T d) where T : struct, Enum {
      if (Enum.TryParse<T>(s, out T res)) return res;
      return d;
   }
}
