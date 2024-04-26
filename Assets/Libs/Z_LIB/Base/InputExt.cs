using UnityEngine;

using System.Collections.Generic;

public enum KeyCodeExt {
   none,
   left_mouse,
   right_mouse,
   middle_mouse,
   enter,
   control,
   shift,
   alt,
   space,
   escape,
}
public class InputExt {


   static float last_early = -1;
   public static void LatestUpdate() {
      fake_input_set?.latest_update?.Invoke();
   }
   public static bool MaybeEarlyUpdate() {
      if (last_early == Time.time) return false;

      override_mouse_left = false;
      override_mouse_right = false;

      override_mouse_left_down = false;
      override_mouse_right_down = false;

      override_control = false;
      override_shift = false;
      override_alt = false;
      last_early = Time.time;
      fake_input_set?.MaybeUpdate();

      return true;
   }



   public class FakeCursor {

      public Sprite base_sprite;
      public Sprite cur_sprite;
      public Vector2 hotspot_px;

      public Vector2 mousePosition;

      public System.Action update_callback;

      public Texture2D set_cursor;

      public void SetSprite(Texture2D cursor, Sprite s, Vector2? hotspot_px = null) {
         set_cursor = cursor;
         s = s ?? base_sprite;
         cur_sprite = s;
         this.hotspot_px = hotspot_px ?? new Vector2(0, 0);
         update_callback?.Invoke();
      }

      public void SetPosition(Vector2 pos) {
         mousePosition = pos;
         update_callback?.Invoke();
      }
   }


   static Dictionary<Texture2D, Sprite> cursor_sprites = new Dictionary<Texture2D, Sprite>();

   public static void SetCursor(Texture2D cursor, Vector2 hotspot_px, CursorMode cm) {
      Sprite cursor_sprite = null;
      if (cursor != null) {
         if (!cursor_sprites.ContainsKey(cursor) || !cursor_sprites[cursor]) {
            var s = Sprite.Create(cursor, new Rect(0, 0, cursor.width, cursor.height), new Vector2(0, 1), 100, 0, SpriteMeshType.FullRect, new Vector4(), false);
            cursor_sprites[cursor] = s;
         }
         cursor_sprite = cursor_sprites[cursor];
      }
      fake_cursor.SetSprite(cursor, cursor_sprite, hotspot_px);
      if (!fake_cursor_enabled) {
         Cursor.SetCursor(cursor, hotspot_px, cm);
      }
   }

   public static FakeCursor fake_cursor = new FakeCursor();

   public static Vector2 fakeMousePosition => fake_cursor.mousePosition;


   public static bool fake_cursor_enabled;

   public static void SetFakeCursorEnabled(bool enabled = true, Vector2? pos = null) {
      if (enabled == fake_cursor_enabled) return;
      fake_cursor_enabled = enabled;
      if (enabled) {
         Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
         fake_cursor.mousePosition = pos ?? Input.mousePosition;
      } else {
         Cursor.SetCursor(fake_cursor.set_cursor, fake_cursor.hotspot_px, CursorMode.Auto);
      }
      fake_cursor?.update_callback?.Invoke();
   }

   public static Vector2 mousePosition => fake_cursor_enabled ? fake_cursor.mousePosition : (Vector2)Input.mousePosition;

   public static bool enter_down => GetKeyDown(KeyCodeExt.enter);

   public static bool control => GetKey(KeyCodeExt.control) || override_control;
   public static bool shift => GetKey(KeyCodeExt.shift) || override_shift;
   public static bool alt => GetKey(KeyCodeExt.alt) || override_alt;
   public static bool alt_down => GetKeyDown(KeyCodeExt.alt);

   public static bool mouse_left_down => Input.GetMouseButtonDown(0) || override_mouse_left_down;
   public static bool mouse_right_down => Input.GetMouseButtonDown(1) || override_mouse_right_down;

   public static bool mouse_left => Input.GetMouseButton(0) || override_mouse_left;
   public static bool mouse_right => Input.GetMouseButton(1) || override_mouse_right;

   public static bool override_mouse_left;
   public static bool override_mouse_right;
   public static bool override_mouse_left_down;
   public static bool override_mouse_right_down;

   public static bool override_mouse_left_up;
   public static bool override_mouse_right_up;


   public static bool override_control;
   public static bool override_shift;
   public static bool override_alt;

   private static FakeInput fake_input_base = new FakeInput();

   private static FakeInput fake_input_set;

   private static FakeInput fake_input {
      get {
         if (!fake_input_set?.unity_owner) return fake_input_base;
         MaybeEarlyUpdate();
         return fake_input_set;
      }
   }

   public static FakeInput CreateFakeInput(GameObject owner) {

      FakeInput res = new FakeInput();
      res.unity_owner = owner;
      fake_input_set = res;
      return res;
   }

   static Dictionary<KeyCode, float> keys_eaten = new();

   [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
   static void InitKeyEat() {
      keys_eaten = new();
   }

   public static void EatKey(KeyCode it) {
      keys_eaten[it] = Time.time;
   }

   public static bool GetKeyDown(KeyCode it) {

      if (lock_keys_for_text_input > 0) {
         return false;
      }

      if (keys_eaten.TryGetValue(it, out var r) && r == Time.time) return false;

      return Input.GetKeyDown(it);
   }
   public static bool GetKeyDown(KeyCodeExt it) {

      if (lock_keys_for_text_input > 0) {
         return false;
      }

      foreach (var k in unity_keys[(int)it]) {
         if (GetKeyDown(k)) return true;
      }
      if (fake_input != null) {
         if (fake_input.GetKeyDown(it)) return true;
      }

      return false;
   }
   public static bool GetKey(KeyCodeExt it) {
      if (lock_keys_for_text_input > 0) {
         return false;
      }
      foreach (var k in unity_keys[(int)it]) {
         if (Input.GetKey(k)) return true;
      }
      if (fake_input != null) {
         if (fake_input.GetKey(it)) return true;
      }
      return false;

   }
   public static bool GetKeyUp(KeyCodeExt it) {
      foreach (var k in unity_keys[(int)it]) {
         if (Input.GetKeyUp(k)) return true;
      }
      if (fake_input != null) {
         if (fake_input.GetKeyUp(it)) return true;
      }
      return false;
   }


   private static KeyCode[][] unity_keys {
      get {
         if (unity_keys_impl == null) {

            var vals = EnumUtil.Values<KeyCodeExt>().ToList();
            unity_keys_impl = new KeyCode[vals.Count][];

            foreach (var v in vals) {
               int id = (int)v;
               var maps = UnityKeysFor(v).ToList().ToArray();
               unity_keys_impl[id] = maps;
            }

         }
         return unity_keys_impl;
      }
   }

   private static KeyCode[][] unity_keys_impl;

   public static IEnumerable<KeyCode> UnityKeysFor(KeyCodeExt it) {

      switch (it) {
         case KeyCodeExt.none:
            break;
         case KeyCodeExt.left_mouse:
            yield return KeyCode.Mouse0;
            break;
         case KeyCodeExt.right_mouse:
            yield return KeyCode.Mouse1;
            break;
         case KeyCodeExt.middle_mouse:
            yield return KeyCode.Mouse2;
            break;
         case KeyCodeExt.enter:
            yield return KeyCode.KeypadEnter;
            yield return KeyCode.Return;
            break;
         case KeyCodeExt.control:
            yield return KeyCode.LeftControl;
            yield return KeyCode.RightControl;
            break;
         case KeyCodeExt.alt:
            yield return KeyCode.LeftAlt;
            yield return KeyCode.RightAlt;
            break;
         case KeyCodeExt.shift:
            yield return KeyCode.LeftShift;
            yield return KeyCode.RightShift;
            break;
         case KeyCodeExt.space:
            yield return KeyCode.Space;
            break;
         case KeyCodeExt.escape:
            yield return KeyCode.Escape;
            break;
      }
   }

   public class FakeInput {

      public FakeInput() {
         var vals = EnumUtil.Values<KeyCodeExt>().ToList();
         input_pressed = new bool[vals.Count];
         input_down = new bool[vals.Count];
         input_up = new bool[vals.Count];
      }

      private float last_updated;

      public void MaybeUpdate() {
         if (last_updated != Time.time) {
            early_update?.Invoke();
            last_updated = Time.time;
         }
      }

      public System.Action latest_update;
      public System.Action early_update;

      public GameObject unity_owner;

      public bool[] input_pressed;
      public bool[] input_down;
      public bool[] input_up;


      public bool GetKeyDown(KeyCodeExt it) => input_down[(int)it];
      public bool GetKey(KeyCodeExt it) => input_pressed[(int)it];
      public bool GetKeyUp(KeyCodeExt it) => input_up[(int)it];
   }


   static int lock_keys_for_text_input;

   public class TextInputLock : System.IDisposable {

      public TextInputLock() {
         lock_keys_for_text_input++;
      }

      public void Dispose() {
         lock_keys_for_text_input--;
      }
   }
   public class CircumventTextLock : System.IDisposable {

      public CircumventTextLock() {
         lock_keys_for_text_input -= 1000000;
      }

      public void Dispose() {
         lock_keys_for_text_input += 1000000;
      }
   }


   public static bool GetKey_Clean(KeyCode key) {
      if (lock_keys_for_text_input > 0) return false;
      if (control) return false;
      return Input.GetKey(key);
   }

   public static Vector2Int GetArrow() {
      Vector2Int cur = new Vector2Int();

      if (Input.GetKeyDown(KeyCode.UpArrow)) {
         cur.y += 1;
      }
      if (Input.GetKeyDown(KeyCode.DownArrow)) {
         cur.y -= 1;

      }
      if (Input.GetKeyDown(KeyCode.RightArrow)) {
         cur.x += 1;

      }
      if (Input.GetKeyDown(KeyCode.LeftArrow)) {
         cur.x -= 1;

      }
      return cur;
   }
}
