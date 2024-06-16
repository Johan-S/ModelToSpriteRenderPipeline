using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UnitViewer : MonoBehaviour {
   public bool show_animations;


   static bool NightfallHeaderRow(string[] row) {
      var name = row[0];
      if (name == "Unit_Number") return true;
      return name.EndsWith("_ID");
   }

   [NonSerialized] GameObject prefab;

   public EventSystem event_system => EventSystem.current;


   GameObject last_select;

   public class UnitTypeDetails {
      public object unit;
      public object type;

      public string name;

      public string corner_msg;
      public Sprite sprite;
      public Shared.UnitAnimationSprites[] animation_sprites;

      public AnnotatedUI.ColoredSprite sized_sprite => new AnnotatedUI.ColoredSpriteImpl {
         sprite = sprite,
         size = sprite.rect.size / sprite.pixelsPerUnit * 64,
      };
   }

   public static UnitTypeDetails FromUnitSprites(Shared.UnitSprites sprites) {
      return new() {
         name = sprites.name,
         corner_msg = sprites.name,
         sprite = sprites.sprite,
         animation_sprites = sprites.animation_sprites,
      };
   }

   bool init_done;

   public void AddUnit(UnitTypeDetails u) {
      if (units == null) {
         SetUnits(new[] { u });
         return;
      }
      var g = units.ConstructNewElement(u);
      units.elements.Add(g);
      AnnotatedUI.MarkDirty_UI(transform);
   }


   public void SetUnits(IEnumerable<UnitTypeDetails> units_to_view) {
      units = SelectUtils.MakeSelectClass(units_to_view.ToList(), can_click_selected: true, can_toggle_off: true,
         default_selected: -1);
      units.extra_callback = () => { };

      units.selected_comp = null;

      init_done = true;
      AnnotatedUI.Visit(transform, this);
   }

   public SelectClass<UnitTypeDetails> units;

   public bool include_no_sprites;

   void LoadAllSpriteAtlasUnits() {
      init_done = true;

      var cats = GeneratedSpritesContainer.GetAll().ToArray();

   }

   // Start is called before the first frame update
   void Start() {
      if (!init_done) {
         
         
         
     
         LoadAllSpriteAtlasUnits();
         
      }
   }

   // Update is called once per frame
   void Update() {
      var a = event_system.currentSelectedGameObject;
      if (a && a != last_select) {
         var b = a.GetComponent<Button>();
         if (b) b.onClick.Invoke();
      }
   }
   
}