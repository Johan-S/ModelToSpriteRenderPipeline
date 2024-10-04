using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using static System.Runtime.CompilerServices.MethodImplOptions;



public class AngleObjectAwayFromCamera : MonoBehaviour, SpriteCapturePipeline.IBeforeSpriteRender {
   [Range(0, 90)]
   public float min_angle;
   [Range(0, 1)]
   public float adjust_factor = 0.5f;

   Quaternion prev;

   bool init;

   [Header("Debug")]
   public Vector3 my_up;
   public Vector3 cam_forward;
   public float angle;
   public float angle_after;

   public void Cleanup() {
      transform.localRotation = prev;
   }


   public void Handle() {
      prev = transform.localRotation;


      var c = CameraHandle.camera_tr;

      var cf = c.forward;
      var up = transform.up;

      if (up.Dot(cf) < 0) up = -up;

      cam_forward = cf;
      this.my_up = up;
      
      var ang = Vector3.Angle(cf, up);
      angle = ang;
      angle_after = ang;

      if (ang > min_angle) {

         return;
      }

      var ax = Vector3.right;
      if (ang != 0) ax = Vector3.Cross(cf, up).normalized;


      var q = Quaternion.AngleAxis((min_angle - ang) * adjust_factor, ax);

      transform.rotation = q * transform.rotation;
      
      angle_after = Vector3.Angle(cf, transform.up);
   }
}