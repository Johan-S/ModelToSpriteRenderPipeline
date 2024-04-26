using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DefaultExecutionOrder(-490)]
public class WindowManager : MonoBehaviour {


   public UnityEngine.UI.Image blink_panel_prefab;
   public static bool MouseInWindow() {
      return ScreenPosInWindow(InputExt.mousePosition);
   }

   public static bool ScreenPosInWindow(Vector2 mp) {

      var safe = Screen.safeArea;
      return safe.Contains(mp);
   }

   public RectTransform front_canvas;

   public RectTransform canvas;

   object last_tooltip;
   public TooltipSpawner cur_tooltip_spawner;
   public TooltipSpawner last_tooltip_spawner;

   object tooltip_candidate;
   float tooltip_candidate_hover_time_left;

   public float tooltip_hover_delay = 0.2f;

   [Header("Current State")] public Object hovered_obj;



   static float last_ui_obj_check = -1;


   public GameObject hovered_ui_object;

   static Button button;

   public InteractableGameObject hovered_game_object;

   public bool ui;
   public bool hover_button;
   public List<WindowContext> registered_contexts = new List<WindowContext>();

   public bool IsTop(WindowContext a) => registered_contexts.Back() == a;

   public WindowContext Peek() {
   
      var r = registered_contexts.Back();
      while (!r && registered_contexts.NotEmpty()) {
         if (!registered_contexts.Empty()) {
            registered_contexts.Pop();
            r = registered_contexts.Back();
         }
      }
      if (r) return r;
      return null;
   }

   IEnumerable<WindowContext> walk_contexts {
      get {
         for (int i = registered_contexts.Count - 1; i >= 0; --i) {
            yield return registered_contexts[i];
         }
      }
   }

   public static void CloseAllMenues(int index, System.Predicate<WindowContext> filter = null) {
      var to_close = instance.registered_contexts.SubList(index);
      if (filter != null) to_close = to_close.Where(filter);
      foreach (var tc in to_close) {
         Destroy(tc.gameObject);
      }
      instance.registered_contexts = instance.registered_contexts.Where(x => !to_close.Contains(x));
      instance.ClearTooltip();
   }

   public static void DetachMenu(GameObject menu) {
      instance.registered_contexts = instance.registered_contexts.Where(x => x.gameObject != menu);
      instance.ClearTooltip();
   }

   public static bool IsInMenu() {
      return instance.registered_contexts.Back() is MenuWindowContext;
   }

   void LockCameraIfMenu() {
      CameraMovement.lock_world = registered_contexts.Back() is MenuWindowContext;
   }

   public static void Register(WindowContext context) {
      Debug.Assert(instance, $"{context} loaded before window manager", context);
      instance.ClearTooltip();
      if (instance.registered_contexts.Contains(context)) {
         return;
      }
      if (instance.registered_contexts.Back().cursor_action) {
         Destroy(instance.registered_contexts.Back().gameObject);
         instance.registered_contexts.Pop();
      }
      instance.registered_contexts.Add(context);
      context.FrontCallback();
      instance.LockCameraIfMenu();
      instance.SortMenuesInCanvas();
   }

   public void SortMenuesInCanvas() {

      int last_sibling_index = -1;

      foreach (var c in registered_contexts) {
         if (c is MenuWindowContext menu) {
            if (!menu) continue;
            int sibling_index = c.transform.GetSiblingIndex();
            if (last_sibling_index != -1 && last_sibling_index > sibling_index) {
               c.transform.SetSiblingIndex(last_sibling_index);
            }
            last_sibling_index = sibling_index;
         }
      }
   }
   public static WindowContext GetTopWindow() {
      var w = instance.Peek();
      return w;
   }

   public static T GetTopWindow<T>() where T : WindowContext {
      var w = instance.Peek();
      if (w is T t) return t;
      return null;
   }


   public static WindowManager instance;

   void Awake() {
      InputExt.SetCursor(null, Vector2.zero, CursorMode.Auto);
      if (instance) {
         Destroy(gameObject);
      } else {
         instance = this;
      }
   }

   public Vector2 MouseWorldPos() {
      return CameraMovement.MouseWorldPos();
   }

   public static void RefreshHovers() {
      last_ui_obj_check = last_ui_obj_check - 1;
      PointerOverUIObject();
   }

   public static bool PointerOverUIObject() {
      var t = Time.time;
      if (last_ui_obj_check != t) {
         last_ui_obj_check = t;
         if (instance) {
            instance.hovered_ui_object = instance.HoverUIGameObject_HitsAnything(InputExt.mousePosition);
         }
      }
      return instance ? instance.hovered_ui_object : false;
   }

   public static System.Func<Vector2, GameObject> custom_world_raycast;


   T HoverWorld<T>() {
      if (!MouseInWindow()) return default;
      if (!CameraMovement.PointerOverWorld()) return default;

      if (custom_world_raycast != null) {

         var o = custom_world_raycast(InputExt.mousePosition);

         if (!o) return default;

         if (typeof(MonoBehaviour).IsAssignableFrom(typeof(T)) || typeof(T).IsInterface) {
            var res = o.GetComponent<T>();
            if (res != null) return res;
         }
         foreach (var valh in o.GetComponents<AnnotatedUI.ValueHolder>()) {
            if (valh.get() is T) {
               return (T)valh.get();
            }
         }

      } else {
         Ray ray = Camera.main.ScreenPointToRay(InputExt.mousePosition);
         RaycastHit2D hitInfo = Physics2D.Raycast(ray.origin, ray.direction);
         if (hitInfo) {
            if (typeof(MonoBehaviour).IsAssignableFrom(typeof(T)) || typeof(T).IsInterface) {
               var res = hitInfo.collider.GetComponent<T>();
               if (res != null) return res;
            }
            foreach (var valh in hitInfo.collider.gameObject.GetComponents<AnnotatedUI.ValueHolder>()) {
               if (valh.get() is T) {
                  return (T)valh.get();
               }
            }
         }
      }

      return default;
   }
   public GameObject HoverUIGameObject(System.Predicate<GameObject> pred) {
      if (!MouseInWindow()) return default;
      if (CameraMovement.PointerOverWorld()) return default;
      return HoverUIGameObject(pred, InputExt.mousePosition);
   }
   public GameObject HoverUIGameObject(System.Predicate<GameObject> pred, Vector2 screen_point) {
      List<RaycastResult> res = new List<RaycastResult>();
      UnityEngine.EventSystems.PointerEventData eventData = new PointerEventData(EventSystem.current);
      eventData.position = screen_point;
      EventSystem.current.RaycastAll(eventData, res);
      Transform last_hit = null;
      foreach (var r in res) {
         if (r.gameObject && r.gameObject.GetComponent<WindowContext>()) break;
         if (r.gameObject && r.gameObject.GetComponent<HoverBlocker>()) break;
         if (last_hit) {
            if (!IsParent(last_hit, r.gameObject.transform)) {
               continue;
            }
         }
         last_hit = r.gameObject.transform;
         if (pred(r.gameObject)) return r.gameObject;
      }
      return default;
   }
   public GameObject HoverUIGameObject_HitsAnything(Vector2 screen_point) {
      List<RaycastResult> res = new List<RaycastResult>();
      UnityEngine.EventSystems.PointerEventData eventData = new PointerEventData(EventSystem.current);
      eventData.position = screen_point;
      EventSystem.current.RaycastAll(eventData, res);
      foreach (var r in res) {
         if (r.gameObject) {
            var o = r.gameObject;
            return o;
         }
      }
      return default;
   }

   public static bool ScreenPointIsOverUI(Vector2 screen_point) {
      if (!ScreenPosInWindow(screen_point)) return false;

      var o = instance.HoverUIGameObject(oo => true, screen_point);

      return o;
   }
   public static bool ScreenPointIsOverWorld(Vector2 screen_point) {

      if (!ScreenPosInWindow(screen_point)) return false;

      var o = instance.HoverUIGameObject_HitsAnything(screen_point);

      return !o;
   }
   public static bool ScreenPointIsOverWorld_Margin(Vector2 screen_point) {
      var safe = Screen.safeArea.Pad(-220, -100);
      if (!safe.Contains(screen_point)) return false;
      var o = instance.HoverUIGameObject_HitsAnything(screen_point);
      return !o;
   }

   public T HoverUI<T>() {
      if (!MouseInWindow()) return default;
      if (CameraMovement.PointerOverWorld()) return default;
      List<RaycastResult> res = new List<RaycastResult>();
      UnityEngine.EventSystems.PointerEventData eventData = new PointerEventData(EventSystem.current);
      eventData.position = InputExt.mousePosition;
      EventSystem.current.RaycastAll(eventData, res);
      Transform last_hit = null;
      if (typeof(MonoBehaviour).IsAssignableFrom(typeof(T)) || typeof(T).IsInterface) {
         foreach (var r in res) {
            if (r.gameObject && r.gameObject.GetComponent<WindowContext>()) break;
            if (r.gameObject && r.gameObject.GetComponent<HoverBlocker>()) break;
            if (last_hit) {
               if (!IsParent(last_hit, r.gameObject.transform)) {
                  continue;
               }
            }
            last_hit = r.gameObject.transform;
            var c = r.gameObject.GetComponent<T>();
            if (c != null) return c;
         }
      }
      last_hit = null;
      foreach (var r in res) {
         if (r.gameObject && r.gameObject.GetComponent<WindowContext>()) break;
         if (r.gameObject && r.gameObject.GetComponent<HoverBlocker>()) break;
         if (last_hit) {
            if (!IsParent(last_hit, r.gameObject.transform)) {
               continue;
            }
         }
         last_hit = r.gameObject.transform;
         foreach (var valh in r.gameObject.GetComponents<AnnotatedUI.ValueHolder>()) {
            if (valh.get() is T) {
               return (T)valh.get();
            }
         }
      }
      return default;
   }

   bool IsParent(Transform b, Transform next) {
      while (b != next) {
         b = b.parent;
         if (!b) {
            return false;
         }
      }
      return true;
   }

   public object HoverUITooltipable() {
      if (!MouseInWindow()) return default;
      if (CameraMovement.PointerOverWorld()) return default;
      List<RaycastResult> res = new List<RaycastResult>();
      UnityEngine.EventSystems.PointerEventData eventData = new PointerEventData(EventSystem.current);
      eventData.position = InputExt.mousePosition;
      EventSystem.current.RaycastAll(eventData, res);
      Transform last_hit = null;
      foreach (var r in res) {
         if (r.gameObject && r.gameObject.GetComponent<WindowContext>()) break;
         if (r.gameObject && r.gameObject.GetComponent<HoverBlocker>()) break;
         if (last_hit) {
            if (!IsParent(last_hit, r.gameObject.transform)) {
               continue;
            }
         }
         last_hit = r.gameObject.transform;
         foreach (var valh in r.gameObject.GetComponents<TooltipSpawner>()) {
            if (TooltipSpawner.HasTooltip(valh.GetUsedTooltip())) {
               return valh.GetUsedTooltip();
            }
         }
      }
      return default;
   }
   public object HoverUITooltipableTransform(out Transform tr) {
      tr = null;
      if (!MouseInWindow()) return default;
      if (CameraMovement.PointerOverWorld()) return default;
      List<RaycastResult> res = new List<RaycastResult>();
      UnityEngine.EventSystems.PointerEventData eventData = new PointerEventData(EventSystem.current);
      eventData.position = InputExt.mousePosition;
      EventSystem.current.RaycastAll(eventData, res);
      Transform last_hit = null;
      foreach (var r in res) {
         if (r.gameObject && r.gameObject.GetComponent<WindowContext>()) break;
         if (r.gameObject && r.gameObject.GetComponent<HoverBlocker>()) break;
         if (last_hit) {
            if (!IsParent(last_hit, r.gameObject.transform)) {
               continue;
            }
         }
         last_hit = r.gameObject.transform;
         foreach (var valh in r.gameObject.GetComponents<TooltipSpawner>()) {
            if (TooltipSpawner.HasTooltip(valh.GetUsedTooltip())) {
               tr = last_hit;
               return valh.GetUsedTooltip();
            }
         }
      }
      return default;
   }
   public T HoverUITransform<T>(out Transform tr) {
      tr = null;
      if (!MouseInWindow()) return default;
      if (CameraMovement.PointerOverWorld()) return default;
      List<RaycastResult> res = new List<RaycastResult>();
      UnityEngine.EventSystems.PointerEventData eventData = new PointerEventData(EventSystem.current);
      eventData.position = InputExt.mousePosition;
      EventSystem.current.RaycastAll(eventData, res);

      Transform last_hit = null;

      if (typeof(MonoBehaviour).IsAssignableFrom(typeof(T)) || typeof(T).IsInterface) {
         foreach (var r in res) {
            if (r.gameObject && r.gameObject.GetComponent<WindowContext>()) break;
            if (r.gameObject && r.gameObject.GetComponent<HoverBlocker>()) break;
            if (last_hit) {
               if (!IsParent(last_hit, r.gameObject.transform)) {
                  continue;
               }
            }
            last_hit = r.gameObject.transform;
            var c = r.gameObject.GetComponent<T>();
            if (c != null) {
               tr = r.gameObject.transform;
               return c;
            }
         }
      }
      last_hit = null;
      foreach (var r in res) {
         if (r.gameObject && r.gameObject.GetComponent<WindowContext>()) break;
         if (r.gameObject && r.gameObject.GetComponent<HoverBlocker>()) break;
         if (last_hit) {
            if (!IsParent(last_hit, r.gameObject.transform)) {
               continue;
            }
         }
         last_hit = r.gameObject.transform;
         foreach (var valh in r.gameObject.GetComponents<AnnotatedUI.ValueHolder>()) {
            if (valh.get() is T) {
               tr = r.gameObject.transform;
               return (T)valh.get();
            }
         }
      }
      return default;
   }

   public T Hover<T>() {
      if (!MouseInWindow()) return default;
      if (CameraMovement.PointerOverWorld()) return HoverWorld<T>();
      return HoverUI<T>();
   }

   public static bool sticky_tooltip => MouseCursorTracker.sticky_tooltip;

   public float tooltip_hover_delay_actual {
      get {
         return tooltip_hover_delay;
      }
   }

   public float TooltipSpeed() => sticky_tooltip ? 2.0f : 1.0f;

   public object GetTooltipCandidate() {
      return tooltip_candidate;
   }

   void ClearTooltip() {
      MouseCursorTracker.ClearStickyTooltips();
      MouseCursorTracker.hoverTooltip = null;
      tooltip_candidate = null;
   }

   public System.Func<GameObject, GameObject> tooltip_init;

   GameObject last_spanwned_tooltip_window;

   void StandardHoverCallbacks() {

      if (!MouseInWindow()) {
         hovered_game_object = null;
      } else if (IsInMenu()) {
         hovered_game_object = HoverUI<InteractableGameObject>();
      } else {
         hovered_game_object = Hover<InteractableGameObject>();
      }
      if (InputExt.fake_cursor_enabled) {
         var sel = Hover<Selectable>();
         if (sel) {
            sel.gameObject.ForceComponent<ButtonPressTrigger>().HoverAnimation();
         }
      }

   }


   public static bool IsHoveredGameObject(WindowManager.InteractableGameObject o) {
      return o == instance?.hovered_game_object;
   }

   void MaybeSpawnTooltip() {

      gameObject.ForceComponent<MouseCursorTracker>();
      TooltipSpawner c;

      if (!MouseInWindow()) {
         c = null;
      } else if (IsInMenu()) {
         c = HoverUI<TooltipSpawner>();
      } else {
         c = Hover<TooltipSpawner>();
      }

      var cur_set_object = override_tooltip ?? c?.GetUsedTooltip();
      if (c && cur_set_object != null) {

         if (cur_set_object == last_tooltip && c == last_tooltip_spawner && last_spanwned_tooltip_window
            && last_spanwned_tooltip_window == MouseCursorTracker.hoverTooltip) {
            MouseCursorTracker.RefreshTooltipMouse();
         } else {
            if (cur_set_object == tooltip_candidate) {
               tooltip_candidate_hover_time_left -= Time.deltaTime * TooltipSpeed();
            } else {
               tooltip_candidate = cur_set_object;
               tooltip_candidate_hover_time_left = tooltip_hover_delay_actual;
            }
            if (InputExt.alt_down) tooltip_candidate_hover_time_left = 0;
            if (tooltip_candidate_hover_time_left <= 0) {
               var tt = c.SpawnTooltipFor(cur_set_object, front_canvas, c.transform, c.corner, tooltip_init);
               tt.ForceComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
               last_spanwned_tooltip_window = tt;
               var g = tt.ForceComponent<CanvasGroup>();
               g.blocksRaycasts = sticky_tooltip;
               MouseCursorTracker.hoverTooltip = tt;
               last_tooltip = cur_set_object;
               last_tooltip_spawner = c;
               tooltip_candidate_hover_time_left = tooltip_hover_delay_actual;
            }
         }
      } else {
         tooltip_candidate_hover_time_left = tooltip_hover_delay_actual;
         tooltip_candidate = null;
      }
   }

   public void DispatchUIAction(bool left_click, bool right_click) {
      RefreshHovers();

      if (CameraMovement.PointerOverWorld()) DispatchWorld(left_click, right_click);
      else DispatchUI(left_click, right_click);
   }

   void DispatchWorld(bool left_click, bool right_click) {
      ui = false;
      var walk_contexts = this.walk_contexts.ToList();


      if (left_click) {
         bool fallthrough = false;
         bool dropme = false;
         foreach (var c in walk_contexts) {
            c.LeftClick(ref fallthrough, ref dropme, this);
            if (!fallthrough) break;
            fallthrough = false;
         }
         // Click world
      }
      if (right_click) {
         bool fallthrough = false;
         bool dropme = false;
         // Left click world
         foreach (var c in walk_contexts) {
            c.RightClick(ref fallthrough, ref dropme, this);
            if (!fallthrough) break;
            fallthrough = false;
         }
      }
      {
         bool fallthrough = false;
         bool dropme = false;
         foreach (var c in walk_contexts) {
            c.Hover(ref fallthrough, ref dropme, this);
            if (!fallthrough) break;
            fallthrough = false;
         }
      }
      {
         bool fallthrough = false;
         bool dropme = false;
         foreach (var c in walk_contexts) {
            c.ContextUpdate(ref fallthrough, ref dropme, this);
            if (!fallthrough) break;
            fallthrough = false;
         }
      }
      MaybeSpawnTooltip();
   }

   public interface InteractableGameObject {

   }

   void DispatchUI(bool left_click, bool right_click) {
      ui = true;
      var walk_contexts = this.walk_contexts.ToList();

      if (left_click) {
         bool fallthrough = false;
         bool dropme = false;
         foreach (var c in walk_contexts) {
            c.LeftClick(ref fallthrough, ref dropme, this);
            if (!fallthrough) break;
            fallthrough = false;
         }
         // Click world
      }
      if (right_click) {
         bool fallthrough = false;
         bool dropme = false;
         // Left click world
         foreach (var c in walk_contexts) {
            c.RightClick(ref fallthrough, ref dropme, this);
            if (!fallthrough) break;
            fallthrough = false;
         }
      }
      {
         bool fallthrough = false;
         bool dropme = false;
         foreach (var c in walk_contexts) {
            c.Hover(ref fallthrough, ref dropme, this);
            if (!fallthrough) break;
            fallthrough = false;
         }
      }
      {
         bool fallthrough = false;
         bool dropme = false;
         foreach (var c in walk_contexts) {
            c.ContextUpdate(ref fallthrough, ref dropme, this);
            if (!fallthrough) break;
            fallthrough = false;
         }
      }

      MaybeSpawnTooltip();
   }

   public float doubleclick_timer = 0.2f;

   public bool doubleclick;

   object override_tooltip;

   public void OverrideTooltip(object override_tooltip) {
      this.override_tooltip = override_tooltip;
   }

   Vector2 last_click;
   float last_click_time;

   public static bool IsDoubleClick() {
      instance.RegisterDoubleClick();
      return instance.doubleclick;
   }

   public object last_single_clicked;

   void RegisterDoubleClick() {
      if (last_click_time == Time.time) return;
      if (Input.GetMouseButtonDown(0)) {
         var new_hovered = Hover<object>();
         if (new_hovered != last_single_clicked) {
            last_click_time = -100;
         }
         last_single_clicked = new_hovered;

         float click_speed = 0.4f;
         float click_dist = 4;
         float click_time = Time.time;
         Vector2 mp = InputExt.mousePosition;
         doubleclick = click_time - last_click_time < click_speed && Vector2.Distance(mp, last_click) < click_dist;
         last_click = mp;
         last_click_time = click_time;
         if (doubleclick) {
            last_click_time = -100;
         }
      }
   }


   void Update() {


      InputExt.MaybeEarlyUpdate();

      override_tooltip = null;
      if (MouseCursorTracker.hoverTooltip) MouseCursorTracker.hoverTooltip.transform.SetAsLastSibling();

      StandardHoverCallbacks();
      RegisterDoubleClick();

      var prev_back = registered_contexts.Back();
      registered_contexts.Filter(a => a && a.isActiveAndEnabled);
      if (prev_back != registered_contexts.Back()) {
         ClearTooltip();
         registered_contexts.Back().FrontCallback();
      }
      if (Input.GetKeyDown(KeyCode.Escape)) {
         bool fallthrough = false;
         bool dropme = false;
         foreach (var c in walk_contexts) {
            c.Escape(ref fallthrough, ref dropme, this);
            if (!fallthrough) break;
            fallthrough = false;
         }
         // Click world
      }

      DispatchUIAction(Input.GetMouseButtonDown(0), Input.GetMouseButtonDown(1));

      LockCameraIfMenu();
      SortMenuesInCanvas();
   }
}
