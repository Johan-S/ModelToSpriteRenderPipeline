

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;



[System.Serializable]
public abstract class AnnotatedUIBaseClass : NullIsFalse {

   public object[] fields => Std.GetFields(this).Select(x => x.val).ToArray();
}


public class NormalizedCodeType : TextTooltip {

   public string name;

   public string right_side_name;
   public string sub_right_side_name;

   public string mini_header;
   public string mini_right_side_name;
   public IEnumerable<AnnotatedUI.ColoredSprite> sprites;

   public object wrapped_tooltip {
      get; set;
   }

   public string TooltipText => name;
}

public class NormalizedCodeType_Extended : NormalizedCodeType {

   public List<KeyVal> stats = new List<KeyVal>();

   public List<object> sub_tooltips = new List<object>();
   public List<object> sub_short_tooltips = new List<object>();
   public List<string> description = new List<string>();
}

public class StatTable : ParagraphTooltip {


   public string name;
   public List<KeyVal> stats = new List<KeyVal>();
   public List<StatTable> sub_stats = new List<StatTable>();

   public StatTable(string name) {
      this.name = name;
   }

   public StatTable(string name, IEnumerable<KeyVal> stats) {
      this.name = name;
      this.stats.AddRange(stats);
   }

   public StatTable(string name, IEnumerable<KeyVal> stats, IEnumerable<StatTable> tables) {
      this.name = name;
      this.stats.AddRange(stats);
      this.sub_stats.AddRange(tables);
   }

   public IEnumerable<string> paragraphs {
      get {
         yield return $"<b>{name}</b>";
      }
   }
   public IEnumerable<object> sub_tooltips {
      get {
         yield return stats;
         if (sub_stats != null) {
            foreach (var t in sub_stats) yield return t;
         }
      }
   }
}

public interface IKeyVal {

   bool has_color {
      get;
   }
   Color color {
      get; set;
   }

   IKeyVal WithColor(Color c);
   IKeyVal WithTooltip(object c);

   string key {
      get; set;
   }
   string name {
      get; set;
   }
   object val {
      get; set;
   }
   object value {
      get;
   }
}
public class KeyVal<K, T> {
   public KeyVal(K key, T val) {
      this.key = key;
      this.val = val;
   }
   public void Deconstruct(out K x, out T y) {
      x = this.key;
      y = this.val;
   }
   public K key;
   public T val;
   public static implicit operator KeyVal<K, T>((K, T) name) => new KeyVal<K, T>(name.Item1, name.Item2);
}

public class KeyVal<T> : AnnotatedUI.TooltipValueWrapper {

   public bool has_color;
   public Color color;

   public KeyVal(string name, T val) {
      this.name = name;
      this.val = val;
   }

   public KeyVal<T> WithColor(Color c) {
      this.color = c;
      this.has_color = true;
      return this;
   }
   public KeyVal<T> WithTooltip(object c) {
      tooltip_object = c;
      return this;
   }

   public object tooltip_object;

   public object wrapped_tooltip => tooltip_object;

   public string key {
      get => name; set => name = value;
   }
   public string name;
   public T val;
   public T value => val;

   public override string ToString() {
      if (val == null) return name;
      return $"{name}: {val}";
   }


   public static implicit operator KeyVal<T>((string, T) name) => new KeyVal<T>(name.Item1, name.Item2);
}

public class KeyVal : AnnotatedUI.TooltipValueWrapper {

   public bool has_color;
   public Color color;

   public KeyVal(string name, object val) {
      this.name = name;
      this.val = val;
   }

   public KeyVal WithColor(Color c) {
      this.color = c;
      this.has_color = true;
      return this;
   }
   public KeyVal WithTooltip(object c) {
      tooltip_object = c;
      return this;
   }

   public object tooltip_object;

   public object wrapped_tooltip => tooltip_object;

   public string key {
      get => name; set => name = value;
   }
   public string name {
      get; set;
   }
   public virtual object val {
      get; set;
   }
   public object value => val;

   public override string ToString() {
      if (val == null) return name;
      return $"{name}: {val}";
   }

   public IEnumerable<object> unpack {
      get {
         yield return key;
         yield return value;
      }
   }


   public static implicit operator KeyVal((string, string) name) => new KeyVal(name.Item1, name.Item2);
   public static implicit operator KeyVal((string, int) name) => new KeyVal(name.Item1, name.Item2);
}