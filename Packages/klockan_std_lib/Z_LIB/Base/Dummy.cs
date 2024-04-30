using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Dummy {
   public static Vector2Int Clamp(this BoundsInt b, Vector2Int pos) {
      pos.x = Mathf.Clamp(pos.x, b.xMin, b.xMax);
      pos.y = Mathf.Clamp(pos.y, b.yMin, b.yMax);
      return pos;
   }

}
