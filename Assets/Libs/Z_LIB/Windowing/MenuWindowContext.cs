
public class MenuWindowContext : WindowContext {

   [System.NonSerialized]
   private string ui_msg_string;
   public void Visit(UnityEngine.Transform tr, object o, System.Action on_revisit = null) {

      if (ui_msg_string == null) {
         var t = GetType();
         ui_msg_string = "UI Menu: " + t.Name;
      }

      AnnotatedUI.Visit(tr, o, ui_msg_string,  on_revisit);
   }

   public void RegisterThis() {
      WindowManager.Register(this);
   }

   private void Awake() {
      RegisterThis();
   }

   public override void LeftClick(ref bool fallthrough, ref bool drop_me, WindowManager manager) {
   }

   public override void RightClick(ref bool fallthrough, ref bool drop_me, WindowManager manager) {
   }

   public override void Escape(ref bool fallthrough, ref bool drop_me, WindowManager manager) {
      Destroy(this.gameObject);
   }
   public override void Hover(ref bool fallthrough, ref bool drop_me, WindowManager manager) {
   }

   public void CloseMe() {
      Destroy(gameObject);
   }
}
