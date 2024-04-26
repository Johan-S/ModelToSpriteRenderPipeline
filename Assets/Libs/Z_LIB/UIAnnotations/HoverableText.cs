using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;

public class HoverableText : MonoBehaviour {

   public Text text;

   public Vector2 local_pos;

   public Rect screenspace_local_rect;

   public string hovered_subsection;

   public int sub_section_index {
      get {
         int res = sub_sections.IndexOf(first_hover_index);
         if (res == -1) {

            Debug.Log($"Don't call this without data!");
         }
         return res;
      }
   }

   public int sub_section_count => sub_sections.Count;

   float last_updated = -1;

   int first_hover_index;
   int last_hover_index;

   string hover_color_set;
   IList<UICharInfo> gen_chars;

   int last_text_length;

   List<int> sub_sections;

   bool IsStartOfColorBlock(string t, int e) {
      if (e == 0) return false;
      if (t[e - 1] != '>') return false;
      string cs = "<color=#...>";
      int st = e - cs.Length;
      if (st < 0) return false;
      if (t[st] != '<') return false;
      for (int i = 0; i < cs.Length; ++i) {
         if (cs[i] == '.') continue;
         if (t[st + i] != cs[i]) return false;
      }
      return true;
   }

   void MaybeClearVars() {
      if (last_text_length != text.text.Length) {
         first_hover_index = -1;
         hover_color_set = null;
         last_hover_index = -1;
         gen_chars = null;
         sub_sections = null;
      }
      if (sub_sections == null) {
         sub_sections = new List<int>();
         string ttt = text.text;
         for (int i = 0; i < ttt.Length; ++i) {
            if (IsStartOfColorBlock(ttt, i)) {
               sub_sections.Add(i);
            }
         }
      }
      last_text_length = text.text.Length;
   }

   void SetHoverColor(string color) {
      Debug.Assert(color.Length == 3);

      if (!IsStartOfColorBlock(text.text, first_hover_index)) return;
      string cs = "<color=#...>";

      if (first_hover_index < cs.Length) return;

      string t = text.text;

      int hash_symbol_pos = first_hover_index - 5;
      if (t[hash_symbol_pos] != '#') return;
      int first_color_char = hash_symbol_pos + 1;

      hover_color_set = t.Substring(first_color_char, 3);


      var tc = t.ToCharArray();

      for (int i = 0; i < 3; ++i) {
         tc[first_color_char + i] = color[i];
      }
      string new_text = new string(tc);
      text.text = new_text;
   }


   public string GetHoveredSection() {

      UpdateHover();
      return hovered_subsection;
   }

   public void UpdateHover() {
      if (last_updated == Time.time) {
         return;
      }
      MaybeClearVars();

      if (hover_color_set != null) {
         SetHoverColor(hover_color_set);
         hover_color_set = null;
      }

      last_updated = Time.time;

      int i = GetHoverIndex(InputExt.mousePosition);
      string t = GetHoverArea(i);
      hovered_subsection = t;

      if (t != null) {
         SetHoverColor("bbf");
      }
   }


   Vector2 GetPos(int i) {

      if (gen_chars.Count <= i) return new Vector2();

      var gc = gen_chars[i];
      var raw_res = gc.cursorPos;
      raw_res.y -= text.fontSize / 2;
      raw_res.x += gc.charWidth / 2;
      return raw_res;
   }

   float RectDist(Vector2 a, Vector2 b) {
      return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
   }

   char GetCharAt(int i) {
      if (i < 0 || i >= text.text.Length) return ' ';
      return text.text[i];
   }

   int GetHoverIndex(Vector2 pos) {
      var tr = text.rectTransform.ToScreenSpace();


      screenspace_local_rect = tr;

      if (!tr.Pad(5).Contains(pos)) return -1;

      var local_mouse_pos = pos - tr.center;

      local_pos = local_mouse_pos;

      var rp = text.cachedTextGenerator;

      int n = rp.characterCount;

      gen_chars = rp.characters;



      Vector2 first_pos = GetPos(0);

      float bd = 10;
      int best_i = -1;
      Vector2 best_pos = first_pos;

      int leftps = 0;

      for (int i = 0; i < n; ++i) {

         var cur_char = GetCharAt(i);
         if (char.IsWhiteSpace(cur_char)) continue;
         if (cur_char == '<') leftps++;
         if (cur_char == '>') {
            leftps--;
            continue;
         }
         if (leftps > 0) continue;


         var cp = GetPos(i);

         float rd = RectDist(cp, local_mouse_pos);
         if (rd < bd) {
            bd = rd;
            best_i = i;
            best_pos = cp;
         }

      }


      Vector2 best_pos_screespace = best_pos + screenspace_local_rect.center;

      Vector2 char_extend = new Vector2(30, text.fontSize);

      screenspace_local_rect = new Rect(best_pos_screespace - char_extend * 0.5f, char_extend);

      // text3.text = $"{local_mouse_pos} {best_pos}\n{first_pos} {last_pos}\n{bd}\n{best_i}";

      return best_i;
   }

   string GetHoverArea(int start) {


      int last_i = -1;
      int first_i = -1;
      for (int i = start; i < text.text.Length; ++i) {
         var ch = GetCharAt(i);
         if (ch == '<') {
            last_i = i;
            break;
         }
         if (ch == '>') return null;
      }
      if (last_i < 0) return null;
      for (int i = start; i >= 0; --i) {
         var ch = GetCharAt(i);
         if (ch == '>') {
            first_i = i + 1;
            break;
         }
         if (ch == '<') return null;
      }
      if (first_i < 0) return null;

      first_hover_index = first_i;
      last_hover_index = last_i;

      if (IsStartOfColorBlock(text.text, first_hover_index)) {
         return text.text.Substring(first_i, last_i - first_i);
      }
      return null;
   }

   private void Awake() {
      if (text == null) {
         text = GetComponent<Text>();
      }
   }

   // Update is called once per frame
   void Update() {
      UpdateHover();
   }
}
