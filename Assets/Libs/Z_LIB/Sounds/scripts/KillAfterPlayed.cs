using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class KillAfterPlayed : MonoBehaviour {

   public AudioSource src;
   public bool started;

   private void Awake() {
      src = GetComponent<AudioSource>();
      if (src.isPlaying) started = true;
   }


   // Update is called once per frame
   void Update() {
      if (started && !src.isPlaying) Destroy(gameObject);
      if (src.isPlaying) started = true;
   }
}
