using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class UnionFindStruct {

   public UnionFindStruct(int sz) {
      data = new int[sz];
      for (int i = 0; i < sz; ++i) data[i] = i;
   }

   int[] data;


   public int Get(int i) {
      int r = data[i];
      if (r == i) return r;
      r = Get(r);
      data[i] = r;
      return r;
   }

   public void Merge(int a, int b) {
      a = Get(a);
      b = Get(b);
      data[b] = a;
   }

}