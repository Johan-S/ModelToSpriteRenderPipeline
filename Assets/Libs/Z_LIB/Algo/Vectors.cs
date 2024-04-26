using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;


public static class VectorOverloads {
   public static IEnumerable<int2> Rect(this (int, int) tup, int2? step = null) {
      int2 size = tup;
      if (step is int2 s) {
         foreach (var kv in size.Times()) {
            yield return kv * s;
         }
      } else {
         foreach (var kv in size.Times()) {
            yield return kv;
         }
      }
   }

   public static List<int2> HexNeighbors(this int2 p) {
      List<int2> res = new List<int2>();
      foreach (var nb in Hexagon.rNeighborSteps(p)) {
         res.Add(nb + p);
      }
      return res;
   }

   public static List<int2> HexPosWithin(this int2 p, int d) {
      List<int2> res = new List<int2>();
      foreach (var nb in new Vector2Int(p.x, p.y).HexPosWithin(d)) {
         res.Add(nb);
      }
      return res;
   }

}


public struct int2 {

   public override string ToString() {
      return $"({x}, {y})";
   }

   public int2(int x, int y) {
      this.x = x;
      this.y = y;
   }

   public int x;
   public int y;
   public void Deconstruct(out int x, out int y) {
      x = this.x;
      y = this.y;
   }

   public IEnumerable<int2> Times() {
      for (int i = 0; i < x; ++i) {
         for (int j = 0; j < y; ++j) {
            yield return new int2(i, j);
         }
      }
   }

   public IEnumerable<int2> Rect(int2? step = null) {
      var size = this;
      if (step is int2 s) {
         foreach (var kv in size.Times()) {
            yield return kv * s;
         }
      } else {
         foreach (var kv in size.Times()) {
            yield return kv;
         }
      }
   }
   public static IEnumerable<int2> operator +(int2 a, IEnumerable<int2> b) {
      foreach (var x in b) yield return a + x;
   }
   public static IEnumerable<int2> operator +(IEnumerable<int2> b, int2 a) {
      foreach (var x in b) yield return x + a;
   }

   static int2 make_3(int x) => new int2(x, x);

   public static int2 operator +(int2 a, Vector2Int b) {
      return new int2(a.x + b.x, a.y + b.y);
   }
   public static int2 operator +(Vector2Int a, int2 b) {
      return new int2(a.x + b.x, a.y + b.y);
   }

   public static int2 operator +(int2 a, int2 b) {
      return new int2(a.x + b.x, a.y + b.y);
   }
   public static int2 operator -(int2 a, int2 b) {
      return new int2(a.x - b.x, a.y - b.y);
   }
   public static int2 operator *(int2 a, int2 b) {
      return new int2(a.x * b.x, a.y * b.y);
   }
   public static int2 operator /(int2 a, int2 b) {
      return new int2(a.x / b.x, a.y / b.y);
   }

   public static int2 operator *(int a, int2 b) {
      return make_3(a) * b;
   }
   public static int2 operator *(int2 a, int b) {
      return a * make_3(b);
   }

   public int2 max(int2 o) {
      return new int2(Mathf.Max(o.x, x), Math.Max(o.y, y));
   }
   public int2 min(int2 o) {
      return new int2(Mathf.Min(o.x, x), Math.Min(o.y, y));
   }


   public static implicit operator int2((int, int) a) => new int2(a.Item1, a.Item2);
   public static implicit operator (int, int)(int2 a) => (a.x, a.y);

   public static implicit operator int2(Vector2Int a) => new int2(a.x, a.y);
   public static implicit operator Vector2Int(int2 a) => new Vector2Int(a.x, a.y);

   public static explicit operator int2((int, int, int) a) => new int2(a.Item1, a.Item2);
   public static implicit operator (int, int, int)(int2 a) => (a.x, a.y, 0);

   public static explicit operator int2(Vector3Int a) => new int2(a.x, a.y);
   public static implicit operator Vector3Int(int2 a) => new Vector3Int(a.x, a.y, 0);
}

public struct int3 {

   public int3(int x, int y = 0, int z = 0) {
      this.x = x;
      this.y = y;
      this.z = z;
   }

   public int x;
   public int y;
   public int z;
   public void Deconstruct(out int x, out int y, out int z) {
      x = this.x;
      y = this.y;
      z = this.z;
   }

   static int3 make_3(int x) => new int3(x, x, x);


   public static int3 operator +(int3 a, int3 b) {
      return new int3(a.x + b.x, a.y + b.y, a.z + b.z);
   }
   public static int3 operator -(int3 a, int3 b) {
      return new int3(a.x - b.x, a.y - b.y, a.z - b.z);
   }
   public static int3 operator *(int3 a, int3 b) {
      return new int3(a.x * b.x, a.y * b.y, a.z * b.z);
   }
   public static int3 operator /(int3 a, int3 b) {
      return new int3(a.x / b.x, a.y / b.y, a.z / b.z);
   }

   public static int3 operator *(int a, int3 b) {
      return make_3(a) * b;
   }
   public static int3 operator *(int3 a, int b) {
      return a * make_3(b);
   }

   public static implicit operator int3((int, int) a) => new int3(a.Item1, a.Item2);
   public static explicit operator (int, int)(int3 a) => (a.x, a.y);

   public static implicit operator int3(Vector2Int a) => new int3(a.x, a.y, 0);
   public static explicit operator Vector2Int(int3 a) => new Vector2Int(a.x, a.y);

   public static implicit operator int3((int, int, int) a) => new int3(a.Item1, a.Item2, a.Item3);
   public static implicit operator (int, int, int)(int3 a) => (a.x, a.y, a.z);

   public static implicit operator int3(Vector3Int a) => new int3(a.x, a.y, a.z);
   public static implicit operator Vector3Int(int3 a) => new Vector3Int(a.x, a.y, a.z);
}


public struct float2 {

   public float2(float x, float y) {
      this.x = x;
      this.y = y;
   }

   public float x;
   public float y;


   public void Deconstruct(out float x, out float y) {
      x = this.x;
      y = this.y;
   }

   static float2 make_3(float x) => new float2(x, x);


   public static float2 operator +(float2 a, float2 b) {
      return new float2(a.x + b.x, a.y + b.y);
   }
   public static float2 operator -(float2 a, float2 b) {
      return new float2(a.x - b.x, a.y - b.y);
   }
   public static float2 operator *(float2 a, float2 b) {
      return new float2(a.x * b.x, a.y * b.y);
   }
   public static float2 operator /(float2 a, float2 b) {
      return new float2(a.x / b.x, a.y / b.y);
   }



   public static float2 operator +(Vector2 a, float2 b) {
      return new float2(a.x + b.x, a.y + b.y);
   }
   public static float2 operator -(Vector2 a, float2 b) {
      return new float2(a.x - b.x, a.y - b.y);
   }
   public static float2 operator *(Vector2 a, float2 b) {
      return new float2(a.x * b.x, a.y * b.y);
   }
   public static float2 operator /(Vector2 a, float2 b) {
      return new float2(a.x / b.x, a.y / b.y);
   }



   public static float2 operator +(float2 a, Vector2 b) {
      return new float2(a.x + b.x, a.y + b.y);
   }
   public static float2 operator -(float2 a, Vector2 b) {
      return new float2(a.x - b.x, a.y - b.y);
   }
   public static float2 operator *(float2 a, Vector2 b) {
      return new float2(a.x * b.x, a.y * b.y);
   }
   public static float2 operator /(float2 a, Vector2 b) {
      return new float2(a.x / b.x, a.y / b.y);
   }

   public static float2 operator *(float a, float2 b) {
      return make_3(a) * b;
   }
   public static float2 operator *(float2 a, float b) {
      return a * make_3(b);
   }


   public static implicit operator float2((float, float) a) => new float2(a.Item1, a.Item2);
   public static implicit operator (float, float)(float2 a) => (a.x, a.y);

   public static implicit operator float2(Vector2 a) => new float2(a.x, a.y);
   public static implicit operator Vector2(float2 a) => new Vector2(a.x, a.y);

   public static explicit operator float2((float, float, float) a) => new float2(a.Item1, a.Item2);
   public static implicit operator (float, float, float)(float2 a) => (a.x, a.y, 0);

   public static explicit operator float2(Vector3 a) => new float2(a.x, a.y);
   public static implicit operator Vector3(float2 a) => new Vector3(a.x, a.y, 0);
}