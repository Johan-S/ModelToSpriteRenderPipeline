using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public static class App {

   public static float timeScale => Time.timeScale * timeScaleMod;
   public static float deltaTime => Time.deltaTime * timeScaleMod;
   public static float timeScaleMod = 1;


   public static Action refresh_action;
}
