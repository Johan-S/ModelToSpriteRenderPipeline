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


   List<(string name, Shared.AnimationBundle anim)> bundles;

   List<Image> images = new();

   void Register(GameObject o) {
      images.Add(o.GetComponentsInChildren<Image>(true).Where(x => !x.name.FastStartsWith("skip_")).Skip(1).First());
   }

   Shared.UnitAnimationSprites cur;

   public void SetSprites(Shared.UnitAnimationSprites animation_sprites) {
      tick = 0;
      cur = animation_sprites;

      bundles = animation_sprites.GetAllAnimations()
         .Where(x => x.Item2 != null && !x.Item2.sprites.IsEmpty())
         .ToList();
      if (animation_sprites.extra_bundles.IsNonEmpty()) bundles.AddRange(animation_sprites.extra_bundles);

      for (int i = 0; i < bundles.Count; i++) {
         if (images.Count == i) Register(Instantiate(prefab, panel));


         var p = images[i].transform.parent;
         p.GetComponentInChildren<TMP_Text>(true).text = $"{bundles[i].name}\n{bundles[i].anim.animation_duration_ms}";
         p.gameObject.SetActive(true);
      }

      for (int i = bundles.Count; i < images.Count; i++) {
         images[i].transform.parent.gameObject.SetActive(false);
      }

      UpdateAnimationState();
   }

   void UpdateAnimationState() {
      for (int i = 0; i < bundles.Count; i++) {
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

      bundles = new();
      UpdateAnimationState();
   }

   // Update is called once per frame
   void Update() {
      tick += Time.deltaTime;
      UpdateAnimationState();
   }

   public override void SetValue(object o) {
      if (o is Shared.UnitAnimationSprites sprites) {
         gameObject.SetActive(true);
         SetSprites(sprites);
         return;
      }

      gameObject.SetActive(false);
   }
}