using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class SpriteGen {
   [Serializable]
   public class BorderData {
      public float border_sz = 1;
      public float border_radius = 4;

      public Color content_color = Color.gray;
      public Color border_color = Color.black;
   }

   public static (Texture2D tex, Sprite sprite) MakeSingleGUItexture(BorderData bd) {

      var d = new GUITextureData();

      d.border_d = new [] {bd};

      var res = d.MakeGUITexture();
      return (res.tex, res.sprites[0]);

   }

   public class GUITextureData {
      public int sprite_size = 32;
      public int pixelsPerUnit = 200;

      public int padding = 1;
      public BorderData[] border_d;


      public (Texture2D tex, Sprite[] sprites) MakeGUITexture() {

         border_d ??= new BorderData[0];


         int namei = 0;

         int size = (int)Mathf.Sqrt(border_d.Length);
      
      
      
         Vector2Int grid_out = new(size, size);

         if (grid_out.x * grid_out.y < border_d.Length) {
            grid_out.x++;
            if (grid_out.x * grid_out.y < border_d.Length) {
               grid_out.y++;
            }
         }
         int sprite_space = sprite_size + padding;

         Texture2D glob_tex = new Texture2D(grid_out.x * sprite_space, grid_out.y * sprite_space, TextureFormat.RGBA32,
            false);

         glob_tex.name = "SpriteGen_MakeGUITexture";

         var all_cols = new Color[glob_tex.width * glob_tex.height];

         glob_tex.SetPixels(all_cols);

         void MakeBorderSprite(Texture2D tex, BorderData b, Rect rect) {
            var sz = rect.size;


            Color Samples1(Vector2 pos) {
               if (pos.x > sz.x * 0.5f) pos.x = sz.x - pos.x;
               if (pos.y > sz.y * 0.5f) pos.y = sz.y - pos.y;
               pos -= new Vector2(1, 1);
               var br = new Vector2(b.border_radius, b.border_radius) - pos;

               float dist = Mathf.Min(pos.x, pos.y);

               if (br.x > 0 && br.y > 0) {
                  dist = b.border_radius - br.magnitude;
               }

               if (dist < 0) {
                  var c = b.border_color;
                  c.a = 0;
                  return c;
               }

               if (dist < b.border_sz) {
                  var c = b.border_color;
                  return c;
               }

               return b.content_color;
            }

            Color Samples(int i, int j) {
               var cl1 = Samples1(new(i + 0.25f, j + 0.25f));
               var cl2 = Samples1(new(i + 0.75f, j + 0.25f));
               var cl3 = Samples1(new(i + 0.25f, j + 0.75f));
               var cl4 = Samples1(new(i + 0.75f, j + 0.75f));


               return (cl1 + cl1 + cl3 + cl4) * 0.25f;
            }


            var bp = rect.position.Round();


            for (int i = 0; i < sprite_size; i++) {
               for (int j = 0; j < sprite_size; j++) {
                  var r = Samples(i, j);
                  tex.SetPixel(bp.x + i, bp.y + j, r);
               }
            }
         }

         // ctx.AddObjectToAsset("tex_1", MakeT(Color.red, Color.blue));
         // ctx.AddObjectToAsset("tex_2", MakeT(Color.blue, Color.gray));

         int id = 0;

         List<Sprite> sres = new();

         foreach (var bd in border_d) {
            int grid_y = id / grid_out.x;
            int grid_x = id % grid_out.x;
            id++;
            Vector4 br = Vector4.one * (bd.border_radius + bd.border_sz);

            int hp = padding / 2;

            Vector2 cp = new(hp + grid_x * sprite_space, hp + grid_y * sprite_space);
            var rect = new Rect(cp, new(sprite_size, sprite_size));
            MakeBorderSprite(glob_tex, bd, rect);
            var sprite = Sprite.Create(glob_tex, rect, new(0.5f, 0.5f), pixelsPerUnit, 2, SpriteMeshType.FullRect, br);

            sprite.name = $"border_sprite_{id}";
            sres.Add(sprite);
         }
         glob_tex.Apply();

         return (glob_tex, sres.ToArray());
      }
   }


   public static Texture2D GridTexture(int w, int h, Color color_1, Color color_2) {
      Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, mipChain: false);
      for (int i = 0; i < w; i++) {
         for (int j = 0; j < h; j++) {
            t.SetPixel(i, j, (i + j) % 2 == 0 ? color_1 : color_2);
         }
      }

      t.filterMode = FilterMode.Point;
      t.Apply();
      return t;
   }

   public static Texture2D BorderTexture(int inner, int border, Color ci, Color cb) {
      int sz = inner + border * 2;
      Texture2D t = new Texture2D(sz, sz, TextureFormat.RGBA32, mipChain: false);
      for (int i = 0; i < sz; i++) {
         for (int j = 0; j < sz; j++) {
            t.SetPixel(i, j, cb);
         }
      }

      for (int i = 0; i < inner; i++) {
         for (int j = 0; j < inner; j++) {
            t.SetPixel(border + i, border + j, ci);
         }
      }

      t.filterMode = FilterMode.Point;
      t.Apply();
      return t;
   }

   public static Sprite GridSprite(Color color_1, Color color_2) {
      return Sprite.Create(SpriteGen.GridTexture(4, 4, color_1, color_2), new Rect(0, 0, 4, 4), new Vector2(0, 0), 1, 0,
         SpriteMeshType.FullRect);
   }

   public static Sprite WhiteSprite() {
      return Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0, 0), 1, 0,
         SpriteMeshType.FullRect);
   }

   public static Sprite WhiteInnerSprite(int inner, int border) {
      int sz = inner + 2 * border;
      return Sprite.Create(BorderTexture(inner, border, Color.white, Color.clear), new Rect(0, 0, sz, sz),
         new Vector2(0, 0), sz, 0, SpriteMeshType.FullRect);
   }

   public static Sprite WhiteInnerSpriteMid(int inner, int border) {
      int sz = inner + 2 * border;
      return Sprite.Create(BorderTexture(inner, border, Color.white, Color.clear), new Rect(0, 0, sz, sz),
         new Vector2(0.5f, 0.5f), sz, 0, SpriteMeshType.FullRect);
   }

   public static Sprite InnerSpriteMid(int inner, int border, Color c1, Color c2) {
      int sz = inner + 2 * border;
      return Sprite.Create(BorderTexture(inner, border, c1, c2), new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz, 0,
         SpriteMeshType.FullRect);
   }

   public static SpriteRenderer MakeRenderer(string name) {
      var sr = new GameObject(name).AddComponent<SpriteRenderer>();
      sr.color = Color.white;
      return sr;
   }
}