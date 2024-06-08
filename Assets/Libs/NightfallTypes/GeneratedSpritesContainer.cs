using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GeneratedSpritesContainer : ScriptableObject {
   public static void ClearCache() {
      _cache = null;
   }

   static List<GeneratedSpritesContainer> _cache;
   static List<GeneratedSpritesContainer> instances => GetInstance();

   static GeneratedSpritesContainer extra_instance;
   

   public const float FLOOR_PX = 39;

   public const float FLOOR_PIVOT = FLOOR_PX / 256;


   public static readonly Vector2 DEFAULT_PIVOT = new Vector2(0.5f, FLOOR_PIVOT);

   public static Sprite GetStableIcon(string unit) {
      return Get(unit)?.icon_sprite;
   }

   public static UnitCats Get(string name) {
      {
         if (extra_instance && extra_instance.lookup.TryGetValue(name, out var k)) return k;
      }
      foreach (var c in GetInstance()) {
         if (c.lookup.TryGetValue(name, out var k)) return k;
      }

      return null;
   }

   public static void SetExtra(GeneratedSpritesContainer instance) {
      extra_instance = instance;
   }

   public static void SetExtra(Texture2D atlas, string meta) {
      var o = MakeFromData(atlas, meta);

      SetExtra(o);
   }

   static List<GeneratedSpritesContainer> GetInstance() {
      if (_cache == null) {
         _cache = Resources.LoadAll<GeneratedSpritesContainer>("").ToList();
         if (_cache.IsEmpty()) {
            Debug.LogError("Didn't found test_atlas meta file! Need GeneratedSpritesContainer to fetch sprites!");
            return null;
         }

         foreach (var c in _cache) {
            if (c.sprites != null) c.SetSprites(c.sprites);
         }
      }

      foreach (var c in _cache) {
         if (c.lookup.IsEmpty()) {
            c.SetSprites(c.sprites);
         }
      }

      return _cache;
   }

   [SerializeField] List<Sprite> genned_sprites;

   public IReadOnlyList<Sprite> GetSprites() {
      return sprites;
   }

   public IReadOnlyList<Sprite> GetGennedSprites() {
      return genned_sprites;
   }

   public List<Sprite> SetSprites(Sprite[] sl) {
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
               sprite = spr,
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
               var floor = -rect.size * FLOOR_PIVOT;
               floor.x = 0;
               var mp = np * 0.5f + floor;

               mp.y = (rect.size * FLOOR_PIVOT).y - wanted_floor_px;

               var nr = new Rect(rect.position + mp, nsz);

               float new_floor_px = FLOOR_PX - mp.y;
               float new_floor_pivot = wanted_floor_px / nsz.y;

               uc.icon_sprite = Sprite.Create(idl_sprite.texture, nr, new Vector2(0.5f, new_floor_pivot),
                  idl_sprite.pixelsPerUnit, 0, SpriteMeshType.FullRect);

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

   [HideInInspector] public Sprite[] sprites;


   static (string file_name, RectInt rect, Vector2 pivot) ParseMetaRow(string row) {
      row = row.Trim();

      var ab = row.Split("\t");
      var fn = ab[0];
      var ri = ab[1].Split(",").map(int.Parse);

      float[] pivot_d = ab.Get(2)?.Split(",")?.map(float.Parse);

      RectInt rect = new(ri[0], ri[1], ri[2], ri[3]);

      Vector2 pivot = pivot_d == null ? DEFAULT_PIVOT : new Vector2(pivot_d[0], pivot_d[1]);

      return (fn, rect, pivot);
   }

   public static GeneratedSpritesContainer MakeFromData(Texture2D atlas, string atlas_meta_text) {
      var container = ScriptableObject.CreateInstance<GeneratedSpritesContainer>();

      container.name = "GeneratedSpritesContainer_Made";


      int sprite_i = 1;


      var rows = atlas_meta_text.SplitLines();


      List<Sprite> sprites = new();

      var data = rows.Select(ParseMetaRow).ToArray();

      foreach (var (f, r, pivot) in data) {
         Rect rf = new Rect(r.x, r.y, r.width, r.height);

         var sprite = Sprite.Create(atlas, rf, pivot, 64, 0, SpriteMeshType.FullRect);

         sprite.name = f;

         sprites.Add(sprite);
      }

      container.SetSprites(sprites.ToArray());

      return container;
   }
}