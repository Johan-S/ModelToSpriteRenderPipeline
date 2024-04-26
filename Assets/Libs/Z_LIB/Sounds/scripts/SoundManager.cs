using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// [CreateAssetMenu(menuName = "tmp")]
[DefaultExecutionOrder(-500)]
public class SoundManager : MonoBehaviour {

   public static void KeepSilent() {
      silent_ticker++;
   }
   public static void StopSilent() {
      silent_ticker--;
   }


   static int silent_ticker;

   public static bool window_opened_now;
   public static bool window_closed_now;
   public static bool object_selected_now;
   public static bool object_deselected_now;
   public static bool sound_played;


   int ticks_until_first_menu_sound = 3;

   public MusicKit overworld_music;

   public MusicKit battle_music;
   public MusicKit main_menu_music;
   public MusicKit pause_menu_music;


   public MusicKit cur_music;
   public MusicKit last_music_object;

   public float next_music_change;

   public void SetMusicBattle() {
      if (cur_music == battle_music) return;
      cur_music = battle_music;
      next_music_change = Time.realtimeSinceStartup;
   }

   public void SetMusicOverworld() {
      if (cur_music == overworld_music) return;
      cur_music = overworld_music;
      next_music_change = Time.realtimeSinceStartup;
   }
   public void SetMenuMusic() {
      if (cur_music == main_menu_music) return;
      cur_music = main_menu_music;
      next_music_change = Time.realtimeSinceStartup;
   }

   public static void ClickOk() {
      if (instance) instance._ClickOk();
   }
   void _ClickOk() {
      if (!can_play) return;
      last_played_sound = Time.time;
      Instantiate(click);
   }
   public void ButtonPress() {
      if (!can_play) return;
      last_played_sound = Time.time;
      Instantiate(button_press);
   }
   public void Select() {
      if (!can_play) return;
      last_played_sound = Time.time;
      object_selected_now = true;
   }
   public void Deselect() {
      if (!can_play) return;
      last_played_sound = Time.time;
      object_deselected_now = true;
   }
   public void MenuOpened() {
      if (!can_play) return;
      window_opened_now = true;
   }
   public void MenuClosed() {
      if (!can_play) return;
      window_closed_now = true;
   }

   public void PlayActionClip(AudioClip clip) {
      if (!can_play_action) return;
      last_played_action_sound = Time.time;
      sound_played = true;
      var ma = Instantiate(sword_hit.Random());
      ma.pitch = Random.Range(0.95f, 1.05f);
      ma.clip = clip;
      ma.Play();
   }
   public void MagicAttack() {
      if (!can_play_action) return;
      last_played_action_sound = Time.time;
      sound_played = true;
      var hit = Instantiate(magic_attack.Random());
      hit.pitch = Random.Range(0.95f, 1.05f);
   }
   public void Arrow() {
      if (!can_play_action) return;
      sound_played = true;
      last_played_action_sound = Time.time;
      var hit = Instantiate(arrow.Random());
      hit.pitch = Random.Range(0.95f, 1.05f);
   }


   AudioSource last_sword_hit;

   [System.NonSerialized]
   List<AudioSource> battle_sounds;

   public void BattleSounds(float level = 0.2f, float max_delayed = 0.2f) {
      if (!CanPlayBackgroundAtDelay(out float delay)) return;
      sound_played = true;

      if (battle_sounds == null || battle_sounds.Count == 0) {
         battle_sounds = new List<AudioSource>(sword_hit);
         // battle_sounds.AddRange(sword_hit);
         // battle_sounds.AddRange(arrow);
      }

      var shr = battle_sounds.Random();

      if (last_sword_hit == shr) shr = battle_sounds.Random();
      last_sword_hit = shr;
      var sh = Instantiate(shr);
      sh.Stop();
      sh.PlayDelayed(delay);


      last_played_background_sound = Time.time + Random.Range(0.2f, 0.4f) * sh.clip.length + delay;

      sh.volume = level;
      sh.pitch = Random.Range(0.95f, 1.05f);
   }
   public void SwordHit() {
      if (!can_play_action) return;
      last_played_action_sound = Time.time;
      sound_played = true;
      var hit = Instantiate(sword_hit.Random());
      hit.pitch = Random.Range(0.95f, 1.05f);
   }
   public void NiceTestSound(float mag = 1) {
      if (!can_play) return;
      last_played_sound = Time.time;
      var x = Instantiate(production_confirmed);
      x.volume *= mag;
   }
   public float last_slider_sound;
   public void SliderSound() {
      if (this != instance) {
         instance.SliderSound();
         return;
      }
      if (Time.time - last_played_sound < 0.05f) return;
      NiceTestSound(1);
   }
   public void ActionConfirmed() {
      if (!can_play) return;
      last_played_sound = Time.time;
      sound_played = true;
      Instantiate(action_confirmed);
   }
   public void ActionCanceled() {
      if (!can_play) return;
      last_played_sound = Time.time;
      sound_played = true;
      Instantiate(action_canceled);
   }
   public void ProductionConfirmed() {
      if (!can_play) return;
      Instantiate(production_confirmed);
   }
   public void ProductionCancelled() {
      if (!can_play) return;
      last_played_sound = Time.time;
      Instantiate(production_cancelled);
   }
   public void WindowOpened() {
      MenuOpened();
   }
   public void InvalidTarget() {
      if (!can_play) return;
      last_played_sound = Time.time;
      sound_played = true;
      Instantiate(invalid_target);
   }
   public void Book_1() {
      if (!can_play) return;
      last_played_sound = Time.time;
      Instantiate(book_1);
   }
   public void Book_2() {
      if (!can_play) return;
      last_played_sound = Time.time;
      Instantiate(book_2);
   }
   public void Book_3() {
      if (!can_play) return;
      last_played_sound = Time.time;
      Instantiate(book_3);
   }
   public void NewTurn() {
      if (!can_play_important) return;
      last_played_important_sound = Time.time;
      sound_played = true;
      Instantiate(new_turn);
   }
   public void MagicWave() {
      if (!can_play_action) return;
      last_played_action_sound = Time.time;
      sound_played = true;
      Instantiate(magic_wave);
   }
   public void ProgressLong() {
      if (!can_play) return;
      last_played_sound = Time.time;
      sound_played = true;
      Instantiate(progress_long);
   }
   public void ProgressMedium() {
      if (!can_play) return;
      last_played_sound = Time.time;
      sound_played = true;
      Instantiate(progress_medium);
   }
   public void ProgressShort() {
      if (!can_play) return;
      last_played_sound = Time.time;
      sound_played = true;
      Instantiate(progress_short);
   }

   public bool infinite_play;
   public float min_time_between_sounds = 0.05f;
   public static float last_played_sound;
   public static float last_played_action_sound;
   public static float last_played_important_sound;

   public static float last_played_background_sound;
   public bool can_play {
      get {
         if (silent_ticker != 0) {
            return false;
         }
         if (last_played_sound > Time.time) last_played_sound = 0;
         return infinite_play || (Time.time - last_played_sound) > min_time_between_sounds;
      }
   }
   public bool can_play_action {
      get {
         if (silent_ticker != 0) return false;
         if (last_played_action_sound > Time.time) last_played_action_sound = 0;
         return infinite_play || (Time.time - last_played_action_sound) > min_time_between_sounds;
      }
   }
   public bool can_play_background {
      get {
         if (silent_ticker != 0) return false;
         if (last_played_background_sound > Time.time + 10) last_played_background_sound = 0;
         return infinite_play || (Time.time - last_played_background_sound) > min_time_between_sounds;
      }
   }
   public bool CanPlayBackgroundAtDelay(out float delay) {
      delay = 0;
      if (silent_ticker != 0) return false;
      if (last_played_background_sound > Time.time + 10) last_played_background_sound = 0;

      float time_to_next = last_played_background_sound + min_time_between_sounds - Time.time;
      if (time_to_next > 0.5f) {
         return false;
      }
      delay = Mathf.Clamp(time_to_next, 0, 0.5f);
      return true;
   }
   public bool can_play_important {
      get {
         if (silent_ticker != 0) return false;
         if (last_played_important_sound > Time.time) last_played_important_sound = 0;
         return infinite_play || (Time.time - last_played_important_sound) > min_time_between_sounds;
      }
   }

   public static void Play(AudioClip clip) {

      instance.PlayActionClip(clip);
   }

   public static SoundManager instance => GetSoundManager();

   static SoundManager instance_impl;
   static bool instance_init;

   [RuntimeInitializeOnLoadMethod]
   static void ResetInit() {
      instance_init = false;
   }
   
   public static SoundManager GetSoundManager() {
      if (!instance_init) {
         instance_init = true;

         if (!instance_impl) {
            var instance_prefab = Resources.Load<SoundManager>("DataManagers/SoundManager");
            if (instance_prefab) {
               Instantiate(instance_prefab);
            } else {
               Debug.Log("No manager at : DataManagers/SoundManager");
            }
         }
         
      }

      return instance_impl;
   }
   
   public AudioSource click;
   public AudioSource button_press;

   public AudioSource action_confirmed;
   public AudioSource action_canceled;
   public AudioSource select;
   public AudioSource deselect;
   public AudioSource invalid_target;

   public AudioSource production_confirmed;
   public AudioSource production_cancelled;

   public List<AudioSource> arrow;
   public List<AudioSource> magic_attack;

   public List<AudioSource> sword_hit;

   public AudioSource window_opened;

   public AudioSource magic_wave;
   public AudioSource new_turn;

   public AudioSource progress_long;
   public AudioSource progress_short;
   public AudioSource progress_medium;

   public AudioSource book_1;
   public AudioSource book_2;
   public AudioSource book_3;

   private void Awake() {
      AudioListener.volume = 0.25f;
      if (instance_impl && instance_impl != this) {
         Destroy(gameObject);
         return;
      }
      DontDestroyOnLoad(gameObject);
      instance_impl = this;

      SceneManager.sceneLoaded += OnSceneLoaded;
   }
   void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
      next_music_change = 0;
   }

   private void LateUpdate() {
      if (cur_music && next_music_change <= Time.realtimeSinceStartup) {
         if (last_music_object) {
            last_music_object.SetFadeOut();
         }
         last_music_object = Instantiate(cur_music);
         next_music_change = Time.realtimeSinceStartup + cur_music.duration;
      }
      

      if (Time.frameCount < ticks_until_first_menu_sound) {
         object_selected_now = false;
         object_deselected_now = false;
         window_opened_now = false;
         window_closed_now = false;
         return;
      }


      if (!can_play) return;
      if (!sound_played) {
         if (object_selected_now) {
            Instantiate(select);
            last_played_sound = Time.time;
         } else if (window_opened_now) {
            Book_1();
            last_played_sound = Time.time;
         } else if (window_closed_now) {
            Book_2();
            last_played_sound = Time.time;
         } else if (object_deselected_now) {
            Instantiate(deselect);
            last_played_sound = Time.time;
         }
      }
      sound_played = false;
      object_selected_now = false;
      object_deselected_now = false;
      window_opened_now = false;
      window_closed_now = false;
   }


}
