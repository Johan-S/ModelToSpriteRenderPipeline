using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class WorkQueue {

   List<System.Action> actions = new List<Action>();

   public void Schedule(System.Action a) {
      actions.Add(a);
   }

   public void ExecuteAll(Func<bool> stop_when) {
      foreach (var a in actions) {
         a();
         if (stop_when()) {
            actions.Clear();
            return;
         }
      }
      actions.Clear();
   }

}