using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class WeightedRandom<T> {

   public WeightedRandom(IEnumerable<(int, T)> weight_val) {
      n = 0;
      vals = new List<(int, T)>();
      foreach (var (i, t) in weight_val) {
         if (i <= 0) continue;
         n += i;
         vals.Add((i, t));;
      }
   }

   int n;

   List<(int, T)> vals;

   public T Get() {
      int x = Rand.Range(0, n);

      foreach (var (i, t) in vals) {
         if (x < i) return t;
         x -= i;
      }
      return default;
   }

   public IEnumerable<T> GetEnumerable() {

      var cvals = vals.ToList();

      while (cvals.Count > 0) {

         int n = cvals.Sum(xx => xx.Item1);

         int x = Rand.Range(0, n);

         for (int a = 0; a < cvals.Count; ++a) {
            var (i, t) = cvals[a];
            if (x < i) {
               yield return t;
               cvals.RemoveAt(a);
               break;
            }
            x -= i;
         }
      }
   }

}