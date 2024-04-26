using UnityEngine;

public static class Rand {

   public static string RandString(int n) {
      char[] ch = new char[n];
      for (int i = 0; i < n; ++i) {
         ch[i] = (char)Rand.Range('a', 'z');
      }
      return new string(ch);
   }
   public static string RandNumberString(int n) {
      char[] ch = new char[n];
      for (int i = 0; i < n; ++i) {
         ch[i] = (char)Rand.Range('0', '9' + 1);
      }
      return new string(ch);
   }
   public static long DoubleRange() {

      long a = Random.Range(0, 1000000000);
      long b = Random.Range(0, 1000000000);

      return a * 1000000000 + b + 1;
   }

   public static int FullInt() {
      return Random.Range(0, 2000000000);
   }

   public static int Range(int l, int r) {
      return Random.Range(l, r);
   }
   public static float Range(float l, float r) {
      return Random.Range(l, r);
   }

   public static Vector2Int Pos(float dist) {
      float miss_dist = Random.Range(0, dist);
      return Vector2Int.RoundToInt(Random.insideUnitCircle * miss_dist);
   }
   public static Vector2 FPos(float dist) {
      float miss_dist = Random.Range(0, dist);
      return Random.insideUnitCircle * miss_dist;
   }
   public static Vector2 OnUnitCircle(float dist) {
      float a = Random.Range(0, 2 * Mathf.PI);
      return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * dist;
   }

   public static bool Percent(int percent) {
      return Range(0, 100) < percent;
   }
   public static bool Chance(int a, int b) {
      return a > Range(0, b);
   }
}

public struct RandStruct {

   public int seed;

   public int Rand() {
      long lseed = seed;
      lseed = (lseed * 14165063) + 217645199;
      lseed = lseed % 715225741;
      seed = (int)lseed;
      return seed;
   }

   public int Range(int l, int r) {
      int sz = r - l;
      int roll = Rand() % sz;
      return l + roll;
   }
   public float Range(float l, float r) {
      float sz = r - l;
      return l + Rand() * sz / 715225741;
   }
   public Vector2 InsideUnitCircle() {
      float a = Range(0, 2 * Mathf.PI);
      return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
   }

   public Vector2 FPos(float dist) {
      float miss_dist = Range(0, dist);
      return InsideUnitCircle() * miss_dist;
   }

   public Vector2Int Pos(float dist) {
      return Vector2Int.RoundToInt(FPos(dist));
   }
}
