using System;
using UnityEngine;

namespace DefaultNamespace {

   [ExecuteAlways]
   public class SetSunScript : MonoBehaviour {
      public Light sun_light;


      void Update() {
         if (sun_light && sun_light.type == LightType.Directional) {
            RenderSettings.sun = sun_light;
         }
      }
   }

}