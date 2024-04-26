using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GeneratedSpritesContainer : ScriptableObject {
   

   static GeneratedSpritesContainer cache;

   public const float FLOOR_PX = 39;

   public const float FLOOR_PIVOT = FLOOR_PX / 256;
   
   public static Sprite GetStableIcon(string unit) {
      var i = GetInstance();
      if (i) return i.lookup.Get(unit)?.icon_sprite;
      return null;
   }

   public static void SetInstance(GeneratedSpritesContainer instance) {

      cache = instance;

   }

   public static void SetInstanceFromData(Texture2D atlas, string meta) {

      var o = MakeFromData(atlas, meta);
      
      SetInstance(o);


   }
   
   
   public static GeneratedSpritesContainer GetInstance() {
      if (!cache) {
         cache = Resources.Load<GeneratedSpritesContainer>("AtlasGen/test_atlas");
         if (!cache) {
            
            cache = Resources.LoadAll<GeneratedSpritesContainer>("").First();
            if (!cache) {
               Debug.LogError("Didn't found test_atlas meta file! Need GeneratedSpritesContainer to fetch sprites!");
               return null;
            }
         }
         if (cache.sprites != null) cache.SetSprites(cache.sprites);
      }

      if (cache.lookup.IsNullOrEmpty()) {
         
         cache.SetSprites(cache.sprites);
      }
      return cache;
   }
   
   [SerializeField]
   List<Sprite> genned_sprites;

   public IReadOnlyList<Sprite> GetSprites() {
      return sprites;
   }
   public IReadOnlyList<Sprite> GetGennedSprites() {
      return genned_sprites;
   }

   public List<Sprite>  SetSprites(Sprite[] sl) {
      genned_sprites ??= new();
      sprites = sl;
      if (sprites != null) {
         lookup = new();
         Dictionary<string, UnitCats> dicts = new();
         foreach (var spr in sprites) {
            var f = spr.name;
            
            var spl = f.Split(".");
            var sc = new SpriteCats {
               full_name = f,
               unit_name = spl[0],
               animation_category = spl[1],
               tex = spr.texture,
               sprite =  spr,
            };
            if (!dicts.TryGetValue(sc.unit_name, out var uc)) {
               uc = new UnitCats();
               uc.unit_name = sc.unit_name;
               dicts[uc.unit_name] = uc;
            }

            uc.sprites.Add(sc);
         }
         foreach (var uc in dicts.Values) {
            var spr = uc.sprites.FirstOrDefault(x => x.animation_category == "Idle") ?? uc.sprites[0];

            uc.idle_sprite = spr.sprite;
            uc.icon_sprite = uc.sprites.FirstOrDefault(x => x.animation_category == "Icon")?.sprite;
            var icon_gen_name = uc.unit_name + ".IconGen";
            if (!uc.icon_sprite) {
               uc.icon_sprite = genned_sprites.Find(x => x.name == icon_gen_name);
            }

            if (!uc.icon_sprite) {
               
               var idl_sprite = uc.idle_sprite;

               var rect = idl_sprite.rect;
               var nsz = rect.size * 0.5f;
               var np = rect.size - nsz;

               float wanted_floor_px = 2; 
               var floor = - rect.size * FLOOR_PIVOT;
               floor.x = 0;
               var mp = np * 0.5f + floor;

               mp.y = (rect.size * FLOOR_PIVOT).y - wanted_floor_px;
               
               var nr = new Rect(rect.position + mp, nsz);

               float new_floor_px = FLOOR_PX - mp.y;
               float new_floor_pivot = wanted_floor_px / nsz.y;
               
               uc.icon_sprite = Sprite.Create(idl_sprite.texture, nr, new Vector2(0.5f, new_floor_pivot), idl_sprite.pixelsPerUnit, 0, SpriteMeshType.FullRect);

               uc.icon_sprite.name = icon_gen_name;

               genned_sprites.Add(uc.icon_sprite);
            }
         }

         lookup = dicts;
      }
      
      

      return genned_sprites;
   }

   public Dictionary<string, UnitCats> lookup;


   public class UnitCats : NullIsFalse {
      public string unit_name;

      public Sprite idle_sprite;
      public Sprite icon_sprite;

      public List<SpriteCats> sprites = new();
   }

   public class SpriteCats {
      public string full_name;
      public string unit_name;
      public string animation_category;

      public RectInt rect;

      public Texture2D tex;

      public Sprite sprite;
   }

   [HideInInspector]
   public Sprite[] sprites;


   static (string file_name, RectInt rect) ParseMetaRow(string row) {
      row = row.Trim();

      var ab = row.Split("\t");
      var fn = ab[0];
      var ri = ab[1].Split(",").map(int.Parse);

      RectInt rect = new(ri[0], ri[1], ri[2], ri[3]);

      return (fn, rect);
   }

   public static GeneratedSpritesContainer MakeFromData(Texture2D atlas, string atlas_meta_text) {
      
      var container = ScriptableObject.CreateInstance<GeneratedSpritesContainer>();

      container.name = "GeneratedSpritesContainer_Made";


      int sprite_i = 1;
      
      
      var rows = atlas_meta_text.SplitLines();


      List<Sprite> sprites = new();

      var data = rows.Select(ParseMetaRow).ToArray();

      foreach (var (f, r) in data) {

         Rect rf = new Rect(r.x, r.y, r.width, r.height);

         var sprite = Sprite.Create(atlas, rf, new Vector2(0.5f, 0.5f), 64, 0, SpriteMeshType.FullRect);

         sprite.name = f;
         
         sprites.Add(sprite);
      }

      container.SetSprites(sprites.ToArray());

      return container;
   }

}