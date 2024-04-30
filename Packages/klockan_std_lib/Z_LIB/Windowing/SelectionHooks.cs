using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface SelectionHooks {
   void SetSelected(bool selected);

   void SetHovered(bool hover);

   object GetData();
}
