using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteLine : MonoBehaviour {

   public Vector3 world_pos_start;
   public Vector3 world_pos_end;

   public SpriteRenderer dots;

   public bool overflow;

   // Start is called before the first frame update
   void Awake() {
      dots = GetComponent<SpriteRenderer>();
   }

   private void Start() {
      FixDots();

   }

   Vector3 pworld_pos_start;
   Vector3 pworld_pos_end;

   void FixDots() {
      if (pworld_pos_end == world_pos_end && pworld_pos_start == world_pos_start) {
         return;
      }
      pworld_pos_end = world_pos_end;
      pworld_pos_start = world_pos_start;


      Vector3 diff = world_pos_end - world_pos_start;

      float scale = transform.localScale.x;

      Vector3 final_pos = diff / 2 + world_pos_start;
      dots.transform.position = final_pos;
      float dist;
      if (overflow) {
         dist = Mathf.Ceil(diff.magnitude / scale);
      } else {
         dist = Mathf.Floor(diff.magnitude / scale);
      }
      dots.size = new Vector2(1, dist);
      var angle = Vector2.SignedAngle(Vector2.up, diff);
      dots.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
   }
   // Update is called once per frame
   void LateUpdate() {
      FixDots();
   }
}
