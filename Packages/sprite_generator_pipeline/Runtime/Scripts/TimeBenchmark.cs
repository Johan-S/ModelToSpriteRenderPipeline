using System.Collections.Generic;
using UnityEngine;

public class TimeBenchmark {
   Dictionary<string, double> times = new();

   double last_t = Time.realtimeSinceStartupAsDouble;
   double first_t = Time.realtimeSinceStartupAsDouble;

   public void Begin() {
      last_t = Time.realtimeSinceStartupAsDouble;
   }

   List<string> steps = new();

   public void Lap(string n) {
      var t = Time.realtimeSinceStartupAsDouble;

      if (!times.TryGetValue(n, out var res)) {
         res = 0;
         steps.Add(n);
      }

      res += t - last_t;
      last_t = t;

      times[n] = res;
   }


   public double LogTimes(int nc) {
      var dt = last_t - first_t;

      if (ExportPipeline.editor_prefs.log_info)
         Debug.Log(
            $"Export Bench tot {dt / nc * 1000:0} ms  per render: {dt:0.0} / {nc} :\n{steps.join("\n", x => $"{x}: {times[x]}")}");

      return dt;
   }
}