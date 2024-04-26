using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResolutionPicker : MonoBehaviour {

   public Dropdown dropdown;

   List<Resolution> resolutions;

   public Toggle fullscreen;

   bool was_fullscreen;
   bool switching;

   public void SetFullscreen(bool val) {
      was_fullscreen = val;
      Screen.fullScreen = val;
      fullscreen.SetIsOnWithoutNotify(val);
      switching = true;
   }

   // Start is called before the first frame update
   void Start() {
      dropdown = GetComponent<Dropdown>();
      resolutions = Screen.resolutions.ToList().Where(x => x.height >= 720 && x.refreshRate == Screen.currentResolution.refreshRate);
      fullscreen.SetIsOnWithoutNotify(Screen.fullScreen);

      fullscreen.onValueChanged.AddListener(SetFullscreen);

      List<Dropdown.OptionData> opts = new List<Dropdown.OptionData>();
      int picked = 0;
      foreach (var r in resolutions) {
         if (r.height == Screen.height && r.width == Screen.width) {
            picked = opts.Count;
         }
         opts.Add(new Dropdown.OptionData(r.ToString()));
      }
      dropdown.AddOptions(opts);
      dropdown.SetValueWithoutNotify(picked);

      dropdown.onValueChanged.AddListener(v => {
         var reso = resolutions[v];
         Screen.SetResolution(reso.width, reso.height, Screen.fullScreen);
      });
   }

   // Update is called once per frame
   void Update() {
      if (!switching && was_fullscreen != Screen.fullScreen) {
         SetFullscreen(Screen.fullScreen);
      }
      switching = false;
   }
}
