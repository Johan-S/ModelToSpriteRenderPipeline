
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;


public class Buffer<T> : CustomArrBase<T>, IDisposable where T : struct {
   public readonly ComputeBuffer gpu;
   bool disposed;

   public Buffer(int n) : base(n) {
      gpu = new ComputeBuffer(n, Marshal.SizeOf(typeof(T)));
   }

   ~Buffer() {
      if (!disposed) gpu.Dispose();
      disposed = true;
   }

   public void Dispose() {
      if (!disposed) gpu.Dispose();
      disposed = true;
   }

   public Buffer<T> Apply() {
      gpu.SetData(arr);
      return this;
   }

   public Buffer<T> Read() {
      gpu.GetData(arr);
      return this;
   }
}