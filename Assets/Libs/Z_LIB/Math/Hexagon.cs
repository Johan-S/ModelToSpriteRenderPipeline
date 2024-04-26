using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public static class Hexagon {

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

      bool odd_row = (coord.x & 1) == 1;

      if (odd_row) {
         floats.y += HEX_SPACE.y / 2;
      }

      return floats;
   }

   public static int SortingHeight(Vector2Int coord) {
      int res = coord.y * 2;
      bool odd_row = (coord.x & 1) == 1;
      if (odd_row) res += 1;
      return -res;
   }

   public static Vector2Int RandomNeighbor(Vector2Int p) {
      int r = Random.Range(0, 6);
      return NeighborSteps(p)[r] + p;
   }

   public static Vector2Int PathTo(Vector2Int p, Vector2Int t) {
      bool odd_row = (p.x & 1) == 1;
      if (p == t) return p;
      if (t.x == p.x) {
         if (t.y > p.y) return new Vector2Int(p.y + 1, p.x);
         if (t.y < p.y) return new Vector2Int(p.y - 1, p.x);
      }

      if (t.x < p.x) p.x--;
      if (t.x > p.x) p.x++;

      if (odd_row) {
         if (t.y > p.y) return new Vector2Int(p.y + 1, p.x);
      } else {
         if (t.y < p.y) return new Vector2Int(p.y - 1, p.x);
      }
      return p;
   }

   public static int BirdDistance(Vector2Int a, Vector2Int b) {
      int dy = Mathf.Abs(a.x - b.x);
      int dx;
      dx = Mathf.Abs(a.y - b.y);
      if ((dy & 1) != 0) {
         bool a_odd = (a.x & 1) == 1;
         if (a_odd) {
            if (b.y > a.y) dx -= 1;
         } else {
            if (b.y < a.y) dx -= 1;
         }
      } else {
         dx = Mathf.Abs(a.y - b.y);
      }
      return dy + Mathf.Max(dx - dy / 2, 0);
   }

   static Vector2Int[] hex_neighbors = {
      new Vector2Int(1, -1),
      new Vector2Int(1, 0),
      new Vector2Int(0, 1),
      new Vector2Int(-1, 0),
      new Vector2Int(-1, -1),
      new Vector2Int(0, -1),
   };

   static Vector2Int[] hex_neighbors_odd = {
      new Vector2Int(-1, 1),
      new Vector2Int(-1, 0),
      new Vector2Int(0, -1),
      new Vector2Int(1, 0),
      new Vector2Int(1, 1),
      new Vector2Int(0, 1),
   };

   static Vector2Int[] hex_neighbors_straight = {
      new Vector2Int(1, 0),
      new Vector2Int(1, -1),
      new Vector2Int(-1, 0),
      new Vector2Int(-1, -1),
      new Vector2Int(0, 1),
      new Vector2Int(0, -1),
   };

   static Vector2Int[] hex_neighbors_straight_odd = {
      new Vector2Int(1, 0),
      new Vector2Int(1, 1),
      new Vector2Int(-1, 0),
      new Vector2Int(-1, 1),
      new Vector2Int(0, 1),
      new Vector2Int(0, -1),
   };


   public static bool InPos(Vector2Int pos, Vector2 world_pos) {
      var cur_p = PositionOf(pos);

      var dist = Vector2.Distance(cur_p, world_pos);
      foreach (var ns in NeighborSteps(pos)) {
         var fp = PositionOf(ns + pos);
         if (Vector2.Distance(fp, world_pos) <= dist) return false;
      }
      return true;
   }


   public static Vector2[] Edges() {

      var ns = NeighborSteps(Vector2Int.zero);

      Vector2[] res = new Vector2[6];


      for (int i = 0; i < 6; ++i) {
         var a = ns[i];
         var b = ns[(i + 1) % 6];
         var p = PositionOf(a) + PositionOf(b) + PositionOf(Vector2Int.zero);
         res[i] = p / 3;
      }
      return res;
   }
   public static Vector2Int[] NeighborSteps_Straight(Vector2Int p) {
      bool odd_row = (p.x & 1) == 1;
      return odd_row ? hex_neighbors_straight_odd : hex_neighbors_straight;
   }
   public static Vector2Int[] NeighborSteps(Vector2Int p) {
      bool odd_row = (p.x & 1) == 1;
      return odd_row ? hex_neighbors_odd : hex_neighbors;
   }
   public static Vector2Int North(this Vector2Int pos) {
      return new Vector2Int(pos.x, pos.y + 1);
   }
   public static Vector2Int South(this Vector2Int pos) {
      return new Vector2Int(pos.x, pos.y - 1);
   }

   public static Vector2Int NorthEast(this Vector2Int p) {
      bool odd_row = (p.x & 1) == 1;
      if (odd_row) return new Vector2Int(p.x + 1, p.y + 1);
      return new Vector2Int(p.x + 1, p.y);
   }
   public static Vector2Int NorthWest(this Vector2Int p) {
      bool odd_row = (p.x & 1) == 1;
      if (odd_row) return new Vector2Int(p.x - 1, p.y + 1);
      return new Vector2Int(p.x - 1, p.y);
   }


   public static Vector2Int SouthEast(this Vector2Int p) {
      bool odd_row = (p.x & 1) == 1;
      if (odd_row) return new Vector2Int(p.x + 1, p.y);
      return new Vector2Int(p.x + 1, p.y - 1);
   }
   public static Vector2Int SouthWest(this Vector2Int p) {
      bool odd_row = (p.x & 1) == 1;
      if (odd_row) return new Vector2Int(p.x - 1, p.y);
      return new Vector2Int(p.x - 1, p.y - 1);
   }
   public static Vector2Int[] rNeighborSteps(Vector2Int p) {
      bool odd_row = (p.x & 1) == 1;
      return odd_row ? rhex_neighbors_odd : rhex_neighbors;
   }



   static Vector2Int[] GetNeighborArea(Vector2Int p, int r) {
      if (r < 0) return new Vector2Int[0];
      int n = 1 + r * (r + 1) * 3;
      Vector2Int[] res = new Vector2Int[n];

      int out_i = 0;
      res[out_i++] = new Vector2Int();

      if (r >= 1) {
         foreach (var mb in rNeighborSteps(p)) res[out_i++] = mb;
      }

      for (int cc = 2; cc <= r; ++cc) {
         int dia = cc * 2 + 1;
         foreach (var (x, y) in dia.Times(dia)) {
            var vec = new Vector2Int(x - cc, y - cc);
            if ((vec + p).HexDist(p) == cc) {
               res[out_i++] = vec;
            }
         }
      }
      Debug.Assert(out_i == res.Length);
      return res;
   }
   static Vector2Int[] rhex_neighbors_witin_2;

   static Vector2Int[] rhex_neighbors_witin_2_odd;

   static Vector2Int[] rhex_neighbors = {
      new Vector2Int(0, 1),
      new Vector2Int(1, 0),
      new Vector2Int(1, -1),
      new Vector2Int(0, -1),
      new Vector2Int(-1, -1),
      new Vector2Int(-1, 0),
   };

   static Vector2Int[] rhex_neighbors_odd = {
      new Vector2Int(0, 1),
      new Vector2Int(1, 1),
      new Vector2Int(1, 0),
      new Vector2Int(0, -1),
      new Vector2Int(-1, 0),
      new Vector2Int(-1, 1),
   };

   static Hexagon() {
      rhex_neighbors_witin_2 = GetNeighborArea(Vector2Int.zero, 2);
      rhex_neighbors_witin_2_odd = GetNeighborArea(new Vector2Int(1, 1), 2);
   }

   public static Vector2Int[] GetNeighborArea(int2 p) {
      bool odd_row = (p.x & 1) == 1;
      return odd_row ? rhex_neighbors_witin_2_odd : rhex_neighbors_witin_2;
   }

}
