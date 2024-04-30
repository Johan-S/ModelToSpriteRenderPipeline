using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class LabeledValue {

   public object label;
   public object value;

   public LabeledValue(object label, object value) {
      this.label = label;
      this.value = value;
   }
}
