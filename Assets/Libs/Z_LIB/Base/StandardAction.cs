using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public abstract class StandardAction : Named, ParagraphTooltip {

   public abstract IEnumerable<string> paragraphs {
      get;
   }

   public abstract string name {
      get;
   }

   public abstract bool Valid();
   public abstract bool Execute();


   public bool valid_action => Valid();
   public System.Action execute => () => Execute();


   public static implicit operator Action(StandardAction a) {
      return () => a.Execute();
   }

   public static implicit operator Func<bool>(StandardAction a) {
      return a.Execute;
   }

   public interface Provider {
      IEnumerable<StandardAction> actions {
         get;
      }
   }
}

