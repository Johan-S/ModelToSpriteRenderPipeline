using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using static ComputeShaderUtils;

namespace DefaultNamespace {

   public class RunTestOnReload {
      [UnityEditor.InitializeOnLoadMethod]
      [RuntimeInitializeOnLoadMethod]
      static void TestK() {
         // RunTestSum();
         // RunTestKernel();
         TestGetRect();
         
      }

      static void TestGetRect() {
         
         var tex = new Texture2D(256, 256);
         
         tex.Clear(new Color(0, 0, 0, 0));
         tex.SetPixel(32, 48, Color.black);
         tex.SetPixel(64, 16, Color.black);
         tex.SetPixel(10, 150, Color.black);
         tex.SetPixel(151, 50, Color.black);
         tex.Apply();
         
         using var res = tex.gpu_GetVisibleRect();
         
         
         res.Read();
         Debug.Log($"rect: {res[0]}");
      }

      static void RunTestSum() {
         int k = shader.SetKernel("SumKernel");
         using var data = new Buffer<float>(256 * 256);
         using var res = new Buffer<float>(1);


         for (int i = 0; i < data.Count; i++) {
            data[i] = i % 4;
         }

         data.Apply();

         shader.SetBuffer("buffer", data);
         shader.SetBuffer("result_buffer", res);

         shader.SetInt("N", data.Length);
         shader.Dispatch();

         res.Read();

         Debug.Log($"kernel res: {res[0]}, actual: {data.Sum()}");
      }

      static void RunTestKernel() {
         int kernel = shader.SetKernel("TestKernel");

         using Buffer<float4> buffer4 = new(256 * 256);
         using Buffer<float> buffer = new(64);


         for (int i = 0; i < buffer4.Count; i++) {
            buffer4[i] = new float4(i % 4, 0, 0, 0);
         }

         buffer4.Apply();
         shader.SetBuffer("buffer4", buffer4.gpu);
         shader.SetBuffer("buffer", buffer.gpu);
         shader.SetInts("N", buffer4.Length / 64);

         shader.Dispatch();
         float real_r = buffer4.Sum(x => x.x);

         buffer4.Read();
         buffer.Read();

         // Debug.Log($"kernel res: {buffer4.join(", ")}\nfd: {buffer.join(", ")}");
         Debug.Log($"kernel res: {buffer.Sum()}, actual: {real_r}\nfd: {buffer.join("\n")}");
      }
   }

}