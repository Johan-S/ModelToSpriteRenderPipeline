using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteCycler : MonoBehaviour {

   public List<Sprite> sprites;

   public SpriteRenderer sprite_renderer;

   public float sprite_cycle_time;

   public int i;
   public float cycle_time;

   // Start is called before the first frame update
   void Start() {
      sprite_renderer = GetComponent<SpriteRenderer>();
   }

   // Update is called once per frame
   void Update() {
      cycle_time += Time.deltaTime;
      while (cycle_time > sprite_cycle_time) {
         i = (i + 1) % sprites.Count;
         cycle_time -= sprite_cycle_time;
      }
      sprite_renderer.sprite = sprites[i];
   }
}
