using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class HexStarPath {

   int[,] move_cost;
   System.Func<Vector2Int, Vector2Int[]> neighbor_steps;
   int max_cost;


   // Dynamic.
   int[,] cur_cost;
   int zero;
   RectInt rect;
   List<List<Vector2Int>> paths = new List<List<Vector2Int>> { };

   public HexStarPath(int[,] move_cost, Func<Vector2Int, Vector2Int[]> neighbor_steps, int max_cost) {
      this.move_cost = move_cost;
      this.neighbor_steps = neighbor_steps;
      this.max_cost = max_cost;
      rect = new RectInt(0, 0, move_cost.GetLength(0), move_cost.GetLength(1));
      cur_cost = new int[rect.width, rect.height];
      zero = 1;
      for (int i = 0; i < 8; ++i) paths.Add(new List<Vector2Int> { });
   }

   void ClearVars() {
      zero = zero + max_cost + 1;
      if (zero > 1000000000) {
         zero = 1;
         foreach (var p in rect.allPositionsWithin) {
            cur_cost[p.x, p.y] = 0;
         }
      }
      for (int i = 0; i < 8; ++i) paths[i].Clear();
   }

   public List<Vector2Int> Path(Vector2Int from, Vector2Int to, bool move_near = false) {
      ClearVars();

      List<Vector2Int> res = new List<Vector2Int>();
      paths[0].Add(from);
      cur_cost[from.x, from.y] = zero;

      int min_cost = from.HexDist(to);

      for (int i = 0; i <= max_cost; ++i) {
         Vector2Int prev = new Vector2Int(-1, -1);
         foreach (var x in paths[i & 7]) {
            if (x == prev) continue;
             prev = x;
            int prev_min_cost = x.HexDist(to);
            foreach (var d in neighbor_steps(x)) {
               var np = d + x;
               if (move_near && np == to) {
                  to = x;
                  goto done;
               }
               if (!rect.Contains(np)) continue;
               ref int cc = ref cur_cost[np.x, np.y];
               if (cc >= zero && cc <= zero + max_cost) continue;
               int next_cost = move_cost[np.x, np.y] + i;
               int next_min_cost = np.HexDist(to);
               if (next_cost > max_cost) continue;
               paths[next_cost & 7].Add(np);
               cc = zero + next_cost;

               if (np == to) {
                  goto done;
               }
            }
         }
         paths[i & 7].Clear();
      }
      done:
      {
         int final_cost = cur_cost[to.x, to.y] - zero;
         if (final_cost < 0 || final_cost > max_cost) return new List<Vector2Int>();
         res.Add(to);

         int c = final_cost + zero;
         Vector2Int x = to;
         int max_tries = max_cost;
         while (x != from && max_tries-- >= 0) {
            c -= move_cost[x.x, x.y];

            foreach (var d in neighbor_steps(x)) {
               var np = d + x;
               if (!rect.Contains(np)) continue;
               if (cur_cost[np.x, np.y] != c) continue;
               res.Add(np);
               x = np;
               break;
            }
         }
      }
      res.Reverse();
      return res;
   }
}
