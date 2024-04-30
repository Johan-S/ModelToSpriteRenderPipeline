﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public static class HexagonPointyTop {

   public static float HexSideLength() {
      return 0.5f;
   }
   public static float HexWidth() {
      float sl = HexSideLength();
      float square_res = sl.Square() - (sl / 2).Square();
      return Mathf.Sqrt(square_res) * 2;
   }
   public static float HexRowHeight() {
      float side = HexSideLength();
      return side * 3 / 2;
   }

   public static readonly float HEX_ROW_HEIGHT = HexRowHeight();
   public static readonly float HEX_WIDTH = HexWidth();
   public static readonly float HEX_SIDE = HexSideLength();
   public static readonly Vector2 HEX_SPACE = new Vector2(HexWidth(), HexRowHeight());

   public static Vector2 PositionOf(Vector2Int coord) {
      Vector2 floats = coord;

      floats *= HEX_SPACE;

      bool odd_row = coord.y % 2 == 1;

      if (odd_row) {
         floats.x += HEX_SPACE.x / 2;
      }

      return floats;
   }

   public static Vector2Int RandomNeighbor(Vector2Int p) {
      int r = Random.Range(0, 6);
      if (r == 0) return new Vector2Int(p.x - 1, p.y);
      if (r == 1) return new Vector2Int(p.x + 1, p.y);
      if (r == 2) return new Vector2Int(p.x, p.y - 1);
      if (r == 3) return new Vector2Int(p.x, p.y + 1);

      bool odd_row = p.y % 2 == 1;
      int x = odd_row ? p.x + 1 : p.x - 1;
      if (r == 4) return new Vector2Int(x, p.y - 1);
      return new Vector2Int(x, p.y + 1);
   }

   public static Vector2Int PathTo(Vector2Int p, Vector2Int t) {
      bool odd_row = p.y % 2 == 1;
      if (p == t) return p;
      if (t.y == p.y) {
         if (t.x > p.x) return new Vector2Int(p.x + 1, p.y);
         if (t.x < p.x) return new Vector2Int(p.x - 1, p.y);
      }

      if (t.y < p.y) p.y--;
      if (t.y > p.y) p.y++;

      if (odd_row) {
         if (t.x > p.x) return new Vector2Int(p.x + 1, p.y);
      } else {
         if (t.x < p.x) return new Vector2Int(p.x - 1, p.y);
      }
      return p;
   }

   public static int BirdDistance(Vector2Int a, Vector2Int b) {
      int dy = Mathf.Abs(a.y - b.y);
      int dx;
      dx = Mathf.Abs(a.x - b.x);
      if (dy % 2 != 0) {
         bool a_odd = a.y % 2 == 1;
         if (a_odd) {
            if (b.x > a.x) dx -= 1;
         } else {
            if (b.x < a.x) dx -= 1;
         }
      } else {
         dx = Mathf.Abs(a.x - b.x);
      }
      return dy + Mathf.Max(dx - dy / 2, 0);
   }
}