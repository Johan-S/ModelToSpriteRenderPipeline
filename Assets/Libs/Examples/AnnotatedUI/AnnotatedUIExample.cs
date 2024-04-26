using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class AnnotatedUIExample : MonoBehaviour {
   [System.Serializable]
   public class ExampleDataObj_Second : Named {

      public string name { get; set; }
      public Sprite[] sprites;

      [Stat(value_pattern = "{0} stats")]
      public int my_stat;
      [Stat]
      public int attack;
      [Stat]
      public int def;

      public object new_sub;

      public IEnumerable<string> description {
         get {
            yield return "My Super unit Descr";

            yield return "Second descr line";
         }
      }

      public IEnumerable<KeyVal> stats => StatsAnnotations.DefaultStats(this);
   }
   [System.Serializable]
   public class ExampleDataObj : Named {
      public int value;

      public string name { get; set; }
   }

   [System.Serializable]
   public class ExampleDataObj_List {
      public int array_count {
         get => objs.Count;
         set {
            while (value > objs.Count) {
               objs.Add(new ExampleDataObj());
            }

            if (value < objs.Count) {
               objs.RemoveRange(value, objs.Count - value);
            }
         }
      }

      public List<ExampleDataObj> objs = new();


      public int sum => objs.Sum(x => x.value);
   }

   public Sprite sprite;

   public RectTransform apply_to;

   public RectTransform tooltip_gen;

   // Start is called before the first frame update
   void Start() {
      AnnotatedUI.Visit(apply_to, new ExampleDataObj_List { });
      
      AnnotatedUI.Visit(tooltip_gen, new ExampleDataObj_Second {
         name = "My Obj",
         sprites = new []{sprite},
         my_stat = 4,
         attack = 13,
         def = 42,
         
         new_sub = new ExampleDataObj {
            name = "My Subo",
            value = 41,
         }
      });
   }

   // Update is called once per frame
   void Update() {
   }
}