using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shared;
using UnityEditor;
using UnityEngine;


public static class Data {


   // Rect goes from bottom left here.
   public static Texture2D GetTex_ViaRenderTexture(Texture2D s, Rect rect) {
      // rect = new(0, 0, s.width, s.height);
      var r = Vector2Int.RoundToInt(rect.size);
      var p = Vector2Int.RoundToInt(rect.position);

      // Debug.Log($"Get tex: {s.name}: {s.rect}");
      var out_renderTexture = new RenderTexture(s.width, s.height, 0);
      out_renderTexture.enableRandomWrite = true;
      RenderTexture.active = out_renderTexture;
      Graphics.Blit(s, out_renderTexture);
      
      

      RenderTexture.active = out_renderTexture;
      
      Texture2D ns = new Texture2D(r.x, r.y, TextureFormat.RGBA32, true);
      ns.ReadPixels(rect, 0, 0);
      ns.Apply();
      
      
      for (int i = 0; i < r.x; i++) {
         for (int j = 0; j < r.y; j++) {
            var y = r.y - j - 1;
            var c = ns.GetPixel(i, y);
            var nc = Color.black;
            nc.a = c.a;
            ns.SetPixel(i, y, nc);
         }
      }

      out_renderTexture.Release();
      return ns;
   }

   public static Texture2D GetTex(Sprite s) {
      var r = Vector2Int.RoundToInt(s.rect.size);
      var p = Vector2Int.RoundToInt(s.rect.position);
      Texture2D ns = new Texture2D(r.x, r.y);

      // Debug.Log($"Get tex: {s.name}: {s.rect}");
      var st = s.texture;


      for (int i = 0; i < r.x; i++) {
         for (int j = 0; j < r.y; j++) {
            ns.SetPixel(i, j, st.GetPixel(p.x + i, p.y + j));
         }
      }

      ns.Apply();

      return ns;
   }

   static GeneratedSpritesContainer.UnitCats GetResourceSprite(string name) {
      var res = Resources.LoadAll<Sprite>("" + name.Substring(2));

      var r = new GeneratedSpritesContainer.UnitCats();

      r.unit_name = name;

      r.idle_sprite = res.Find(x => x.name.Contains("idle"));
      r.icon_sprite = res.Find(x => x.name.Contains("icon")) ?? r.idle_sprite;

      r.sprites.Add(new(name, "Idle", 0) {
         sprite = r.idle_sprite,
      });

      int frames_per = 10;

      foreach (var (i, att) in res.filter(x => x.name.StartsWith("Attack1")).Sorted(x => x.name).enumerate()) {
         r.sprites.Add(new(name, "Attack1", i * frames_per) {
            sprite = att,
         });
      }

      foreach (var (i, att) in res.filter(x => x.name.StartsWith("Walk")).Sorted(x => x.name).enumerate()) {
         // Debug.Log($"atto {name}: {att.name}");
         r.sprites.Add(new(name, "Walk", i * frames_per) {
            sprite = att,
         });
      }


      return r;
   }
}