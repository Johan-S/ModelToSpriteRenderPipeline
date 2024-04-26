using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[System.Serializable]
public class NullIsFalse {
   public static implicit operator bool(NullIsFalse a) => a != null;
}
