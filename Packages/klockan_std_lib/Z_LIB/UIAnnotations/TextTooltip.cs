using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface TextTooltip {
   string TooltipText { get; }
}

public interface ParagraphTooltip {

   IEnumerable<string> paragraphs {
      get;
   }
}



public interface StandardTooltip {
   public string name { get; }
}
