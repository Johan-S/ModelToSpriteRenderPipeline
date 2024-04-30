using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static class HexExtensions {

   public static int HexDist(this Vector2Int a, Vector2Int b) {
      return Hexagon.BirdDistance(a, b);
   }
}