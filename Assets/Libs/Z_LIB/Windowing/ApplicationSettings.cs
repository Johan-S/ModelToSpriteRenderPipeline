using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Audio;

public class ApplicationSettings : MonoBehaviour {

   public static List<int> scene_stack = new List<int>();

   public AudioMixer audio_mixer;

   public void QuitGame() {
      Application.Quit();
   }
   public void SetTimeScale(float x) {
      App.timeScaleMod = x;
   }

   public List<ColorTheme> theme_switch;

   public Dropdown color_theme_dropdown;

   public Slider volume;
   public Slider effects_volume;
   public Slider ui_volume;
   public Slider music_volume;

   public bool effects_volume_changed;
   public bool volume_changed;
   public float sound_last_played;

   public float GetVolume(string name) {
      string vn = $"volume_{name}";
      if (PlayerPrefs.HasKey(vn)) {
         return PlayerPrefs.GetFloat(vn);
      }
      return 75.0f;
   }
   public void SetVolume(string name, float val) {
      string vn = $"volume_{name}";
      PlayerPrefs.SetFloat(vn, val);
      audio_mixer.SetFloat(name, -60 + val * 0.8f);
   }

   public void PushVolumes(string name) {
      SetVolume(name, GetVolume(name));
   }

   public void PushVolumes() {
      PushVolumes("master_volume");
      PushVolumes("ui_volume");
      PushVolumes("effects_volume");
      PushVolumes("music_volume");
   }

   public void SetVolume(float val) {
      volume_changed = true;
      SetVolume("master_volume", val);
   }

   public void SetUIVolume(float val) {
      volume_changed = true;
      SetVolume("ui_volume", val);
   }

   public void SetGameEffectVolume(float val) {
      effects_volume_changed = true;
      SetVolume("effects_volume", val);
   }

   public void SetMusicVolume(float val) {
      SetVolume("music_volume", val);
   }

   public static void PushSceneStack() {
      var i = SceneManager.GetActiveScene().buildIndex;
      if (scene_stack.Count == 0) {
         scene_stack.Add(i);
      } else {
         if (scene_stack.Back() != i) scene_stack.Add(i);
      }
   }

   public static void Load(string scene) {
      PushSceneStack();
      SceneManager.LoadScene(scene);
   }
   public static void Reload() {
      SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
   }

   public void ReloadScene() {
      Reload();
   }
   public void LoadScene(Scene scene) {
      PushSceneStack();
      SceneManager.LoadScene(scene.buildIndex);
   }
   public void LoadScene(string scene) {
      PushSceneStack();
      SceneManager.LoadScene(scene);
   }
   public void LoadSceneIndex(int scene) {
      PushSceneStack();
      SceneManager.LoadScene(scene);
   }
   public void LoadSceneObject(Scene scene) {
      PushSceneStack();
      SceneManager.LoadScene(scene.buildIndex);
   }

   public void LoadPrevScene() {
      if (scene_stack.Count == 0) {
         SceneManager.LoadScene(0);
      } else {
         var i = scene_stack.Pop();
         SceneManager.LoadScene(i);
      }
   }

   void SetValues() {
      if (volume) volume.value = GetVolume("master_volume");
      if (music_volume) music_volume.value = GetVolume("music_volume");
      if (effects_volume) effects_volume.value = GetVolume("effects_volume");
      if (ui_volume) ui_volume.value = GetVolume("ui_volume");
      volume_changed = false;
      effects_volume_changed = false;
      if (color_theme_dropdown) {
         int io = theme_switch.IndexOf(ColorTheme.theme);
         if (io >= 0) color_theme_dropdown.SetValueWithoutNotify(io);
      }
   }

   void SetCallbacks() {
      if (volume) volume.onValueChanged.AddListener(SetVolume);
      if (music_volume) music_volume.onValueChanged.AddListener(SetMusicVolume);
      if (effects_volume) effects_volume.onValueChanged.AddListener(SetGameEffectVolume);
      if (ui_volume) ui_volume.onValueChanged.AddListener(SetUIVolume);
      if (color_theme_dropdown) {
         List<Dropdown.OptionData> opts = new List<Dropdown.OptionData>();
         foreach (var ct in theme_switch) {
            opts.Add(new Dropdown.OptionData {
               text = ct.name
            });
         }
         color_theme_dropdown.AddOptions(opts);
         color_theme_dropdown.onValueChanged.AddListener(x => theme_switch[x].Select());
      }
   }

   private void Awake() {
      SetCallbacks();
      SetValues();
   }

   private void OnEnable() {
      SetValues();
   }

   private void Update() {
      if (volume_changed && sound_last_played + 0.2f < Time.time) {
         SoundManager.instance.NiceTestSound();
         volume_changed = false;
         sound_last_played = Time.time;
      }
      if (effects_volume_changed && sound_last_played + 0.2f < Time.time) {
         SoundManager.instance.SwordHit();
         effects_volume_changed = false;
         sound_last_played = Time.time;
      }
   }
}
