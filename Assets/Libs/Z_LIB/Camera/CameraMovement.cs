using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraMovement : MonoBehaviour {

   public static bool lock_world;
   [Header("Utility")]

   public WorldDimensions current_world;

   [Header("Settings")]
   public float speed = 2;
   public float zoom_speed = 0.2f;
   public float zoom_level;

   public float max_zoom = 10;
   Bounds bounds;

   public bool player_controlling_camera;

   float z;

   public bool wrap_x;
   public bool wrap_y;

   public float wrap_x_distance;
   public float wrap_y_distance;

   public static CameraMovement instance;

   public static bool PointerOnScreen() {
      var mp = InputExt.mousePosition;
      if (mp.x < 0 || mp.x > Screen.width || mp.y < 0 || mp.y > Screen.height) return false;
      return true;
   }

   public void Limit(Rect r, bool wrap_x = false, bool wrap_y = false) {
      wrap_x_distance = r.width;
      wrap_y_distance = r.height;
      this.wrap_x = wrap_x;
      this.wrap_y = wrap_y;
      if (r.size.x == 0 || r.size.y == 0) {
         Debug.LogError("Rect can't have zero size");
      }

      if (camera_cache) {
         float h = Mathf.Max(r.height, r.width / camera_cache.aspect);
         if (wrap_x) {
            h = Mathf.Min(h, (r.width - 2) / camera_cache.aspect);
         }
         if (wrap_y) {
            h = Mathf.Min(h, r.height);
         }

         if (wrap_x || wrap_y) {
            max_zoom = Mathf.Log(h / 2);
         } else {
            max_zoom = Mathf.Log(h / 2) + 0.5f;
         }
      }
      if (wrap_x) {
         bounds = new Bounds(r.center + Vector2.right * r.width * 0.5f, r.size);
      } else {
         bounds = new Bounds(r.center, r.size);
      }
   }
   public void LimitZoom(Rect r) {
      if (r.size.x == 0 || r.size.y == 0) {
         Debug.LogError("Rect can't have zero size");
      }
      if (camera_cache) {
         float h = Mathf.Min(r.height, r.width / camera_cache.aspect);
         max_zoom = Mathf.Log(h / 2);
      }
   }

   public float radius => camera_cache.orthographicSize;

   public void View(Vector2 pos, float? radius) {
      if (radius == null) View(pos);
      else {
         var r = (float)radius;
         r = LimitCameraSize(r);
         View(new Rect(pos.x - r, pos.y - r, r * 2, r * 2));
      }
   }

   public void Translate(Vector2 d) {
      camera_cache.transform.position = camera_cache.transform.position + (Vector3)d;
   }
   public void View(Vector2 d) {
      Translate(d - CenterPos());
   }

   public Vector2 CenterPos() {
      return camera_cache.transform.position;
   }

   public void LogVars() {
      var max_sz = MaxSize();
      var min_sz = MinSize();
      var sz = Size();
      Debug.Log($"sz: {sz}, max_sz: {max_sz}, min_sz: {min_sz}");
   }
   public Vector2 ExtentForSize(float size) {
      var h = size;
      var w = h * camera_cache.aspect;
      return new Vector2(w, h);
   }

   public Vector2 Extent() {
      return ExtentForSize(camera_cache.orthographicSize);
   }
   public Vector2 MaxExtent() {
      return ExtentForSize(MaxCameraSize());
   }
   public Vector2 MinExtent() {
      return ExtentForSize(MinCameraSize());
   }
   public Vector2 MaxSize() {
      return MaxExtent() * 2;
   }
   public Vector2 MinSize() {
      return MinExtent() * 2;
   }
   public Vector2 Size() {
      return Extent() * 2;
   }
   public Rect ViewRect() {
      var e = Extent();
      var p = CenterPos();
      return new Rect(p - e, e * 2);
   }
   public Vector2 MinPos() {
      return CenterPos() - Extent();
   }
   public Vector2 MaxPos() {
      return CenterPos() + Extent();
   }

   public void View(Rect r) {
      if (camera_cache) {
         float h = Mathf.Max(r.height, r.width / camera_cache.aspect) / 2;
         h = LimitCameraSize(h);
         zoom_level = Mathf.Log(h);
         camera_cache.transform.position = r.center;
         float camera_sz = Mathf.Exp(zoom_level);
         camera_cache.orthographicSize = camera_sz;
      }
   }
   public Rect GetView() {
      if (camera_cache) {
         float h = camera_cache.orthographicSize * 2;
         float w = h * camera_cache.aspect;


         var r = new Rect(0, 0, h, w);
         r.center = CenterPos();

         return r;

      }
      return new Rect(0, 0, 10, 10);
   }

   Camera camera_cache;

   // Start is called before the first frame update
   void Awake() {
      camera_cache = GetComponent<Camera>();
      z = camera_cache.transform.position.z;
      Limit(camera_cache.WorldRect());
      View(camera_cache.WorldRect());
      instance = this;
   }

   public void SetWorld(WorldDimensions world, bool UpdateView = true) {
      if (world.world_rect.width == 0 || world.world_rect.height == 0) {
         Debug.Log("Uninitialized workd!");
         return;
      }
      current_world = world;
      Limit(current_world.world_rect, current_world.wrap_x, current_world.wrap_y);
      if (UpdateView) View(current_world.world_rect);
   }

   private void Start() {
      if (current_world) {
         SetWorld(current_world);
      }
   }

   Vector2 last_mouse_pos;

   Vector2 WASD() {
      Vector2 res = Vector2.zero;
      if (InputExt.GetKey_Clean(KeyCode.W)) res.y += 1;
      if (InputExt.GetKey_Clean(KeyCode.S)) res.y -= 1;
      if (InputExt.GetKey_Clean(KeyCode.D)) res.x += 1;
      if (InputExt.GetKey_Clean(KeyCode.A)) res.x -= 1;
      return res;
   }

   bool was_dragging = false;

   public int default_drag_button;

   bool special_dragging;

   int special_drag_button;

   bool MouseDown() {

      return (Input.GetMouseButtonDown(default_drag_button) || override_center_down) && PointerOverWorld() && PointerOnScreen();
   }

   bool MouseHeld() {
      if (special_dragging) {
         if (Input.GetMouseButton(special_drag_button)) {
            return true;
         } else {
            special_dragging = false;
         }
      }
      return Input.GetMouseButton(default_drag_button) || override_center;
   }
   public static bool override_left;
   public static bool override_right;
   public static bool override_center;
   public static bool override_center_down;

   public void StartSpecialMouseDrag(int button, Vector2 drag_mouse_pos) {
      special_dragging = true;
      was_dragging = true;
      special_drag_button = button;
      Vector2 mouse_pos = camera_cache.ScreenToWorldPoint(drag_mouse_pos);
      last_mouse_pos = mouse_pos;
   }

   bool IsDragging() {
      if (!was_dragging) {
         if (MouseDown()) {
            was_dragging = true;
         }
         return false;
      }
      var should_drag = MouseHeld();
      var res = was_dragging && should_drag;
      was_dragging = should_drag;
      return res;
   }

   public static Vector2 MouseWorldPos() {
      return instance.camera_cache.ScreenToWorldPoint(InputExt.mousePosition);
   }
   public static Vector2 WorldPosOf(Vector2 vec) {
      return instance.camera_cache.ScreenToWorldPoint(vec);
   }
   public static Vector2 CameraPosOf(Vector2 world_pos) {
      return instance.camera_cache.WorldToScreenPoint(world_pos);
   }

   void UpdateDrag() {
      Vector2 mouse_pos = camera_cache.ScreenToWorldPoint(InputExt.mousePosition);
      camera_cache.transform.Translate(last_mouse_pos - mouse_pos);
   }

   void ResetDrag() {
      Vector2 mouse_pos = camera_cache.ScreenToWorldPoint(InputExt.mousePosition);
      last_mouse_pos = mouse_pos;
   }

   public GameObject selected;

   public static bool PointerOverWorld() {
      if (!InputExt.fake_cursor_enabled) {
         if (!Application.isFocused) {
            return false;
         }
      }

      return !WindowManager.PointerOverUIObject();
   }

   bool updated;

   void MoveCameraWithinBounds() {
      if (wrap_x) {
         var pos = camera_cache.transform.position;
         var cp = bounds.ClosestPoint(pos);
         if (cp.x != pos.x) {
            if (cp.x > pos.x) {
               pos.x += wrap_x_distance;
            } else {
               pos.x -= wrap_x_distance;
            }
         }
         pos.y = cp.y;
         camera_cache.transform.position = pos;
      } else {
         camera_cache.transform.position = bounds.ClosestPoint(camera_cache.transform.position);
      }
   }

   const float min_zoom = 0.7f;

   public float MaxCameraSize() {
      return Mathf.Exp(max_zoom);
   }
   public float MinCameraSize() {
      return Mathf.Exp(min_zoom);
   }
   public float LimitCameraSize(float cur) {
      return Mathf.Min(cur, MaxCameraSize());
   }
   public float BestCameraSizeToView(Vector2 sz) {

      var max_sz = MaxSize();

      float max_fac = Mathf.Max(sz.y / max_sz.y, sz.x / max_sz.x);

      if (max_fac >= 1) {
         max_fac = 1;
      }
      return MaxCameraSize() * max_fac;
   }

   public void PublicUpdate() {
      if (updated) return;
      player_controlling_camera = false;
      updated = true;
      instance = this;

      var pos_in = transform.position;

      if (lock_world) {
         ResetDrag();
      }
      if (IsDragging()) {
         player_controlling_camera = true;
         UpdateDrag();
      }

      ResetDrag();

      if (!lock_world && PointerOverWorld() && PointerOnScreen()) {
         zoom_level -= Input.GetAxis("Mouse ScrollWheel") * zoom_speed;
      }
      zoom_level = Mathf.Clamp(zoom_level, min_zoom, max_zoom);


      float camera_sz = Mathf.Exp(zoom_level);
      camera_cache.orthographicSize = camera_sz;
      UpdateDrag();


      if (!lock_world) camera_cache.transform.Translate(WASD() * speed * Time.deltaTime * camera_sz);
      if (pos_in != camera_cache.transform.position) {
         player_controlling_camera = true;
      }

      MoveCameraWithinBounds();

      ResetDrag();
      var p = camera_cache.transform.position;
      camera_cache.transform.position = new Vector3(p.x, p.y, z);


      override_center = false;
      override_left = false;
      override_right = false;
      override_center_down = false;

   }

   private void Update() {
      InputExt.MaybeEarlyUpdate();
      PublicUpdate();
   }

   public System.Action late_update_once;

   private void LateUpdate() {

      late_update_once?.Invoke();
      late_update_once = null;
      updated = false;
   }
}
