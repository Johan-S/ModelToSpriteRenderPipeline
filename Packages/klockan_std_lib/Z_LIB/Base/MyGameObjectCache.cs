using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyGameObjectCache : MonoBehaviour {
   public MyGameObjectCache Init(GameObject prefab, Transform transform) {
      this.prefab = prefab;
      this.spawnTrnasform = transform;
      live_objects = new();
      dead_objects = new();
      return this;
   }

   public GameObject prefab;
   public Transform spawnTrnasform;

   public List<GameObject> live_objects;
   public List<GameObject> dead_objects;

   public void CheckDeads() {
      dead_objects.Clear();
      foreach (var o in live_objects) {
         if (!o.activeSelf) dead_objects.Add(o);
      }
   }

   public GameObject Create() {
      if (dead_objects.IsNonEmpty()) {
         var o = dead_objects.Pop();
         o.SetActive(true);
         return o;
      }

      var no = Instantiate(prefab, spawnTrnasform);

      live_objects.Add(no);
      return no;
   }

   // Start is called before the first frame update
   void Start() {
   }

   [SerializeField] float next_cache_update;

   // Update is called once per frame
   void Update() {
      next_cache_update -= Time.deltaTime;
      if (dead_objects.Count == 0) next_cache_update -= Time.deltaTime;
      if (next_cache_update > 0) return;
      if (dead_objects.Count * 2 < live_objects.Count) {
         CheckDeads();
         next_cache_update = 0.1f;
      }
   }
}