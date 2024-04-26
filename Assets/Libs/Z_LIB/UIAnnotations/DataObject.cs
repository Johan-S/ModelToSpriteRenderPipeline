using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface DataObject<T> {

   ObjectAction<T> Action(string type, string name=null, int cost=0);
}
