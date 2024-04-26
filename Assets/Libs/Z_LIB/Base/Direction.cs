using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public struct Direction {

   int val;

   public static Direction from(Vector2Int d) {
      return from(Angle(d));
   }

   public static float Angle(Vector2Int d) {
      return Vector2.SignedAngle(Vector2.right, d);
   }

   public static Direction from(float angle) {
      int dir = Mathf.RoundToInt(angle * 8 / 360);
      return new Direction(dir);
   }

   public static bool prefer_right(float angle) {
      float a = angle * 8 / 360;
      int dir = Mathf.RoundToInt(a);
      return a - dir < 0;
   }

   private Direction(int val) {
      this.val = val & 7;
   }

   public Direction RotateLeft() {
      return new Direction(val + 1);
   }

   public Direction RotateLeft(int times) {
      return new Direction(val + times);
   }
   public Direction RotateRight() {
      return new Direction(val + 7);
   }
   public Direction RotateRight(int times) {
      return new Direction(val + 7 * times);
   }
   public Direction Reverse() {
      return new Direction(val + 4);
   }

   public static Vector2Int operator +(Vector2Int v, Direction dir) {
      return v + dir_lookup(dir.val);
   }
   public static Vector2Int operator +(Direction dir, Vector2Int v) {
      return v + dir_lookup(dir.val);
   }

   public static implicit operator Vector2Int(Direction dir) {
      return dir_lookup(dir.val);
   }

   public bool diagonal => val % 2 == 1;
   public bool straight => val % 2 == 0;

   public Vector2Int vec => dir_lookup(val);

   public static readonly Direction right = new Direction { val = 0 };
   public static readonly Direction up_right = new Direction { val = 1 };
   public static readonly Direction up = new Direction { val = 2 };
   public static readonly Direction up_left = new Direction { val = 3 };
   public static readonly Direction left = new Direction { val = 4 };
   public static readonly Direction down_left = new Direction { val = 5 };
   public static readonly Direction down = new Direction { val = 6 };
   public static readonly Direction down_right = new Direction { val = 7 };


   static Vector2Int dir_lookup(int d) {
      switch (d) {
         case 0:
            return Vector2Int.right;
         case 1:
            return Vector2Int.right + Vector2Int.up;
         case 2:
            return Vector2Int.up;
         case 3:
            return Vector2Int.left + Vector2Int.up;
         case 4:
            return Vector2Int.left;
         case 5:
            return Vector2Int.left + Vector2Int.down;
         case 6:
            return Vector2Int.down;
         case 7:
            return Vector2Int.right + Vector2Int.down;
         default:
            return new Vector2Int();
      }
   }
}
