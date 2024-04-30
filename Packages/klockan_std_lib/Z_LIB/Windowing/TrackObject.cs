using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackCanvasObject : MonoBehaviour {

   public Transform track;
   public Rect extent;
   public RectTransform rectTransform;


   public Rect? fixed_screen_pos;

   public Camera my_camera;

   public Rect ScreenPos() {
      if (fixed_screen_pos != null) return (Rect)fixed_screen_pos;

      var rect_trans = track.GetComponent<RectTransform>();
      if (rect_trans) {
         return rect_trans.ToScreenSpace();
      } else {
         var pos = track.position;
         var sp = my_camera.WorldToScreenPoint(pos);
         var corner = my_camera.WorldToScreenPoint(pos + Vector3.one);
         var one = corner - sp;

         var bl = extent.position * one;
         var sz = extent.size * one;

         var r = new Rect(bl + (Vector2)sp, sz);
         return r;
      }
   }

   void PlaceBelowLeft(Rect r) {
      rectTransform.anchorMin = new Vector2(0, 0);
      rectTransform.anchorMax = new Vector2(0, 0);
      rectTransform.pivot = Vector2.one;
      rectTransform.anchoredPosition = r.position + new Vector2(r.width / 2, 0);
   }
   void PlaceBelowRight(Rect r) {
      rectTransform.anchorMin = new Vector2(0, 0);
      rectTransform.anchorMax = new Vector2(0, 0);
      rectTransform.pivot = new Vector2(0, 1);
      rectTransform.anchoredPosition = r.position + new Vector2(r.width / 2, 0);
   }
   void PlaceAboveRight(Rect r) {
      rectTransform.anchorMin = new Vector2(0, 0);
      rectTransform.anchorMax = new Vector2(0, 0);
      rectTransform.pivot = new Vector2(0, 0);
      rectTransform.anchoredPosition = r.position + new Vector2(r.width / 2, r.height);
   }
   void PlaceAboveLeft(Rect r) {
      rectTransform.anchorMin = new Vector2(0, 0);
      rectTransform.anchorMax = new Vector2(0, 0);
      rectTransform.pivot = new Vector2(1, 0);
      rectTransform.anchoredPosition = r.position + new Vector2(r.width / 2, r.height);
   }

   void Track() {
      if (!track) {
         Destroy(gameObject);
         return;
      }
      var r = ScreenPos();

      var sp = r.center;

      if (sp.y < 400) {
         if (sp.x > Screen.width - 400) {
            PlaceAboveLeft(r);
         } else {
            PlaceAboveRight(r);
         }
      } else {
         if (sp.x > Screen.width - 400) {
            PlaceBelowLeft(r);
         } else {
            PlaceBelowRight(r);
         }
      }
   }

   bool init = false;

   public void Init() {
      if (init) return;
      init = true;
      my_camera = Camera.main;
      rectTransform = GetComponent<RectTransform>();
      Track();
   }

   // Start is called before the first frame update
   void Start() {
      Init();
   }

   // Update is called once per frame
   void Update() {
      Track();
   }
}
