using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

public static class StringSimilarity {


   public static float Score_Main(string a, string b) {

      if (a.Length == 0) return 1;
      if (b.Length == 0) return 1;


      int[,] res = new int[a.Length + 1, b.Length + 1];
      for (int i = 0; i < a.Length + 1; ++i) {
         for (int j = 0; j < b.Length + 1; ++j) {
            res[i, j] = 9999999;
         }
      }
      res[0, 0] = 0;

      for (int i = 0; i < a.Length; ++i) {
         for (int j = 0; j < b.Length; ++j) {
            int cur = res[i, j];

            char x = a[i];
            char y = b[j];

            if (y == ' ') {
               res[i, j + 1] = Mathf.Min(res[i, j + 1], cur);
            } else {
               res[i, j + 1] = Mathf.Min(res[i, j + 1], cur + 1);
            }
            if (x == ' ') {
               res[i + 1, j] = Mathf.Min(res[i + 1, j], cur);
            } else {
               res[i + 1, j] = Mathf.Min(res[i + 1, j], cur + 1);
            }


            if (char.ToLower(x) == char.ToLower(y) || x == ' ' || y == ' ') {
               res[i + 1, j + 1] = Mathf.Min(res[i + 1, j + 1], cur);
            } else {
               res[i + 1, j + 1] = Mathf.Min(res[i + 1, j + 1], cur + 1);
            }
         }
      }

      float diff = res[a.Length, b.Length];

      float score = diff / Mathf.Max(a.Length, b.Length);
      return score;
   }
   public static float Score_Main_PrepFront(string a, string b) {
      float score = Score_Main(a, b);
      if (b.StartsWith(a)) {
         return score / 2;
      }
      return score;
   }
   public static float Score_Main_PrepFront_Twice(string a, string b) {
      float score = Score_Main(a, b);
      if (b.StartsWith(a)) {
         return score / 3;
      }
      if (b.ToLower().StartsWith(a.ToLower())) {
         return score / 2;
      }
      return score;
   }

   static string[] last_split;

   static string last_reverse_sentence;
   static object last_ref;

   public static float Score(string a, string b) {

      var res = Score_Main_PrepFront(a, b);
      if (res > 0) {
         object o = a;
         if (o != last_ref) {
            last_ref = o;
            last_split = a.Split(' ');
            last_reverse_sentence = last_split.Reverse().Join(" ");
         }

         var alt = Score_Main_PrepFront(last_reverse_sentence, b) + 0.2f;

         if (alt < res) return alt;
      }
      return res;
   }
   public static float Score_ExtraWeightFront(string a, string b) {

      var res = Score_Main_PrepFront_Twice(a, b);
      if (res > 0) {
         object o = a;
         if (o != last_ref) {
            last_ref = o;
            last_split = a.Split(' ');
            last_reverse_sentence = last_split.Reverse().Join(" ");
         }

         var alt = Score_Main_PrepFront_Twice(last_reverse_sentence, b) + 0.2f;

         if (alt < res) return alt;
      }
      return res;
   }
   public static T Closest<T>(string name, IEnumerable<T> objs) where T : Named {
      float best_score = 9999;

      T res = default;

      foreach (var t in objs) {
         float c = Score(name, t.name);
         if (c < best_score) {
            best_score = c;
            res = t;
         }
      }
      return res;
   }
   public static (float score, string best) GetClosestScore(string name, IEnumerable<string> objs) {
      float best_score = 9999;

      string res = null;

      foreach (var t in objs) {
         float c = Score(name, t);
         if (c < best_score) {
            best_score = c;
            res = t;
         }
      }
      return (best_score, res);
   }
   public static (float, T) GetClosestScore<T>(string name, IEnumerable<T> objs) where T : Named {
      float best_score = 9999;

      T res = default;

      foreach (var t in objs) {
         float c = Score(name, t.name);
         if (c < best_score) {
            best_score = c;
            res = t;
         }
      }
      return (best_score, res);
   }
   public static List<(float score, T val)> GetAllScores_PrioFront<T>(string name, IList<T> objs) where T : Named {
      var res = objs.Map(x => (score: Score_ExtraWeightFront(name, x.name), val: x));
      return res;
   }

   // Lower is better!
   public static List<(float score, T val)> GetAllScores<T>(string name, IList<T> objs) where T : Named {
      var res = objs.Map(x => (score: Score(name, x.name), val: x));
      return res;
   }
   public static List<string> SortedByCloseness(string name, IList<string> objs) {

      return objs.Sorted(x => Score(name, x));

   }
   public static string Closest(string name, IEnumerable<string> objs) {
      float best_score = 9999;

      string res = default;

      foreach (var t in objs) {
         float c = Score(name, t);
         if (c < best_score) {
            best_score = c;
            res = t;
         }
      }
      return res;
   }


}
