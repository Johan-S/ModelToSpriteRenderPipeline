using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;


public class UnitAnimationViewerPanel : UISubComponent {
   public GameObject prefab;


   public Transform panel;


   const int ticks_per_sec = 60;


   float tick = 0;


   (string name, GameData.AnimationBundle anim)[] bundles;

   List<Image> images = new();

   void Register(GameObject o) {
      images.Add(o.GetComponentsInChildren<Image>(true).Where(x => !x.name.FastStartsWith("skip_")).Skip(1).First());
   }

   GameData.UnitType cur;
   public void SetUnit(GameData.UnitType u) {
      tick = 0;
      cur = u;

      bundles = u.animation_sprites.GetAllAnimations()
         .Where(x => x.Item2 != null && !x.Item2.sprites.IsEmpty())
         .ToArray();


      for (int i = 0; i < bundles.Length; i++) {
         if (images.Count == i) Register(Instantiate(prefab, panel));

         UnitTypeObject.FillBundleMs(bundles[i].anim);


         var p = images[i].transform.parent;
         p.GetComponentInChildren<TMP_Text>(true).text = $"{bundles[i].name}\n{bundles[i].anim.animation_duration_ms}";
         p.gameObject.SetActive(true);
      }

      for (int i = bundles.Length; i < images.Count; i++) {
         images[i].transform.parent.gameObject.SetActive(false);
      }

      UpdateAnimationState();
   }

   void UpdateAnimationState() {
      for (int i = 0; i < bundles.Length; i++) {
         var b = bundles[i].anim;
         var img = images[i];

         int rend_t = b.GetRend((int)(1000 * tick));

         img.sprite = b.sprites[rend_t];
      }
   }

   // Start is called before the first frame update
   void Awake() {
      prefab.SetActive(false);
      Register(prefab);

      bundles = Array.Empty<(string, GameData.AnimationBundle)>();
      UpdateAnimationState();
   }

   // Update is called once per frame
   void Update() {
      tick += Time.deltaTime;
      UpdateAnimationState();
   }

   public override void SetValue(object o) {
      if (o is UnitTypeObject ut) o = GameData.ParseUnit(ut, null);
      
      if (o is GameData.UnitType tt) {
         if (tt != cur) {
            SetUnit(tt);
            gameObject.SetActive(true);
         }
      } else {
         gameObject.SetActive(false);
      }
   }
}
