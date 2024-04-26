using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public abstract class TooltipRefImpl<T> : TooltipSpawner.TooltipRef {

   public abstract T MakeRefTooltip();
   T res_cache;
   public T ref_tooltip {
      get {
         if (res_cache == null) res_cache = MakeRefTooltip();
         return res_cache;
      }
   }
   object TooltipSpawner.TooltipRef.ref_tooltip => ref_tooltip;
}