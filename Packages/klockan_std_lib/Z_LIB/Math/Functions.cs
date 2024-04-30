using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public static class Functions {


   // high value isn't included in range.
   public static int BinarySearch(int lo, int hi, System.Func<int, bool> need_higher) {
      hi--;
      while (lo <= hi) {

         int mid = (lo + hi) / 2;

         if (need_higher(mid)) {
            lo = mid + 1;
         } else {
            hi = mid - 1;
         }
      }
      return lo;
   }


   public class Oscillator {

      public float frequency_hz;

      float val;

      public void Update() {
         Update(Time.deltaTime);
      }
      public void Update(float t) {
         val += t * frequency_hz;
         val -= (int)val;
      }

      public float Sinus(float min = 0, float max = 1) {

         float res = Mathf.Sin(val * Mathf.PI * 2);
         res = (res + 1) * 0.5f;
         float sz = max - min;
         return (res * sz + min);
      }
      public float Blink(float min = 0, float max = 1) {
         if (val < 0.5f) return max;
         return min;
      }

      public SinusOscillator Clone() {
         return (SinusOscillator)MemberwiseClone();
      }
   }

   [System.Serializable]
   public class SinusOscillator {

      public float frequency_hz;

      [Range(0, 100)]
      public float min_percent;
      [Range(0, 100)]
      public float max_percent;

      float val;

      public float GetAndUpdate() {
         return GetAndUpdate(Time.deltaTime);
      }
      public float GetAndUpdate(float t) {

         val += t * frequency_hz;

         float res = Mathf.Sin(val * Mathf.PI * 2);

         if (val > 1) val -= 1;

         res = (res + 1) * 0.5f;
         float sz = max_percent - min_percent;
         return (res * sz + min_percent) * 0.01f;
      }

      public SinusOscillator Clone() {
         return (SinusOscillator) MemberwiseClone();
      }
   }


   public static float Sawtooth(float freq, float? time = null) {
      float x = time ?? Time.time;
      x = x * freq % 2;
      float res = x > 1 ? 2 - x : x;
      return res;
   }
   public static float Harmonic(float freq, float? time = null) {
      float x = time ?? Time.time;
      x = x * freq * Mathf.PI * 2;
      return Mathf.Cos(x) * -0.5f + 0.5f;
   }
}
