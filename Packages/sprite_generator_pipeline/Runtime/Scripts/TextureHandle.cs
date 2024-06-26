using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureHandle : MonoBehaviour {

   public int size = 128;
   
   RenderTexture render_texture;

   void Awake() {
      
      render_texture = new RenderTexture(size, size, 32);
   }

   // Start is called before the first frame update
   void Start() {
      
   }

   // Update is called once per frame
   void Update() {
   }
}