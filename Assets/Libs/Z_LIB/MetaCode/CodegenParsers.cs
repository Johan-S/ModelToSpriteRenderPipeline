using System.IO;
using System.Collections.Generic;
using System.Text;

using System.Reflection;

using SystemType = System.Type;
using Method = System.Reflection.MethodInfo;
using Field = System.Reflection.FieldInfo;
using Property = System.Reflection.PropertyInfo;
using Parameter = System.Reflection.ParameterInfo;


using static Codegen;

public class TypeInfo : InfoClass {

   public static TypeInfo Get<T>() {

      return Codegen.Parse(typeof(T));
   }
   public static TypeInfo Get(SystemType t) {

      return Codegen.Parse(t);
   }

   public TypeInfo(SystemType system_in) {
      original_system_type = system_in;
      code_type_name = original_system_type.GetCodeTypeName();

      system = system_in;

      if (system.IsArray) {
         IsArray = true;
         system = system.GetElementType();
      }

      if (system.IsNullableType()) {
         nullable_type = true;
         system = system.GetGenericArguments()[0];
      }
      if (system.IsByRef) {
         system = system.GetElementType();
      }

      if (system.IsVoidType()) {
         IsVoid = true;
      }


      IsPublic = system.IsPublic;
      IsStatic = system.IsStatic();



   }

   public bool IsArray;

   public bool IsVoid;


   public bool nullable_type;

   public bool ref_type;

   public string code_type_name;

   public string arg_mod = "";

   private SystemType original_system_type;
   private SystemType system;



   Codegen.MethodInfo[] methods_impl;
   Codegen.PropertyInfo[] properties_impl;
   Codegen.FieldInfo[] fields_impl;

   public Codegen.MethodInfo[] methods {
      get {
         InitLate();
         return methods_impl;
      }
   }
   public Codegen.PropertyInfo[] properties {
      get {
         InitLate();
         return properties_impl;
      }
   }
   public Codegen.FieldInfo[] fields {
      get {
         InitLate();
         return fields_impl;
      }
   }


   public void InitLate() {
      if (methods_impl != null) return;

      HashSet<Method> prop_methods = new HashSet<Method>();
      properties_impl = system.GetProperties().ArrayMap(Parse);

      foreach (var p in properties_impl) {
         if (p.getter) prop_methods.Add(p.getter.system);
         if (p.setter) prop_methods.Add(p.setter.system);
      }

      methods_impl = system.GetMethods().WhereArray(prop_methods.NotContains).ArrayMap(Parse);
      fields_impl = system.GetFields().ArrayMap(Parse);
   }

   public override void Init() {

   }
   protected override SystemType GetReturnType() {
      return system;
   }
   public override string RefName => code_type_name;

   public override string Declaration() {

      return $"{DeclarationFlag}class {RefName} ";
   }

   public override string DefaultImplementation() {

      List<string> body = new List<string>();

      foreach (var m in fields) {
         body.Add(m.DefaultFullDelcaration());
      }
      foreach (var m in properties) {
         body.Add(m.DefaultFullDelcaration());
      }
      foreach (var m in methods) {
         body.Add(m.DefaultFullDelcaration());
      }

      return body.WrapIndentJoin();
   }
   public override string ReferenceWrapperImplementation(string reference_object) {

      List<string> body = new List<string>();

      foreach (var m in fields) {
         body.Add(m.DefaultFullReferenceWrapperImplementation(reference_object));
      }
      foreach (var m in properties) {
         body.Add(m.DefaultFullReferenceWrapperImplementation(reference_object));
      }
      foreach (var m in methods) {
         body.Add(m.DefaultFullReferenceWrapperImplementation(reference_object));
      }

      return body.WrapIndentJoin();
   }
}


public static partial class Codegen {

   public static void PrintValues<T>(T para) {

      Console.WriteLine("");
      Console.WriteLine($"\n## PrintValues < {typeof(T).Name} > ( {para} )\n");
      Console.WriteLine("");
      string ends = "";

      foreach (var (name, val, type) in Codegen.GetAllValues(para)) {


         if (!(val is string) && val is System.Collections.IEnumerable ie) {
            Console.WriteLine($"  {name,24}:     {type.Name}");

            int ic = 0;
            foreach (var e in ie) {
               Console.WriteLine($"  {ends,26}[{ic++,4}]: {e}");
            }

            Console.WriteLine($"  {ends,24}}}");

         } else {
            if (val is bool b && b) {
               Console.WriteLine($"  {name,24}: {val,-24}  ~|~  type: {type.Name}");
            } else {

               Console.WriteLine($"  {name,24}:     {val,-24}  ~|~  type: {type.Name}");
            }
         }

      }
      Console.WriteLine("");
      Console.WriteLine($"\n## END PrintValues < {typeof(T).Name} > ( {para} )\n");
      Console.WriteLine("");
   }
   public static List<(string name, object ob, SystemType type)> GetAllValues<T>(T o) {

      var res = new List<(string name, object o, SystemType type)>();
      var st = typeof(T);

      if (o == null) {
         res.Add(($"this", null, st));
         return res;
      }


      foreach (var ap in st.GetProperties()) {
         if (ap.GetMethod?.IsStatic != false) continue;
         try {
            res.Add((ap.Name, ap.GetValue(o), ap.PropertyType));
         } catch (System.Exception) {

         }
      }

      foreach (var ap in st.GetFields()) {
         try {
            res.Add((ap.Name, ap.GetValue(o), ap.FieldType));
         } catch (System.Exception) {

         }

      }
      return res;
   }


   static Dictionary<object, object> info_class_cache = new Dictionary<object, object>();


   static MethodInfo CacheParse<T, MethodInfo>(T mi, System.Func<T, MethodInfo> f) where MethodInfo : InfoClass {
      if (mi == null) return null;
      var val = (MethodInfo)info_class_cache.Get(mi, null);
      if (val == null) {
         val = f(mi);
         info_class_cache[mi] = val;
         val.Init();
      }
      return val;
   }

   public static MethodInfo Parse(Method mi) {
      return CacheParse(mi, x => new MethodInfo(x));
   }
   public static ParameterInfo Parse(Parameter mi) {
      return CacheParse(mi, x => new ParameterInfo(x));
   }
   public static TypeInfo Parse(SystemType mi) {
      return CacheParse(mi, x => new TypeInfo(x));

   }
   public static FieldInfo Parse(Field mi) {
      return CacheParse(mi, x => new FieldInfo(x));

   }
   public static PropertyInfo Parse(Property mi) {
      return CacheParse(mi, x => new PropertyInfo(x));
   }


   public abstract class InfoClass : NullIsFalse {

      public string Name => RefName;


      public bool IsStatic;
      public bool IsPublic = true;


      public string DeclarationFlag => (IsPublic ? "public " : "") + (IsStatic ? "static " : "");


      public abstract void Init();

      protected abstract SystemType GetReturnType();
      public abstract string RefName {
         get;
      }
      public abstract string Declaration();
      public virtual string DefaultImplementation() {

         return "";
      }
      public virtual string ReferenceWrapperImplementation(string reference_object) {

         return "";
      }
      public string DefaultFullDelcaration() {
         return Declaration() + DefaultImplementation();
      }
      public virtual string DefaultFullReferenceWrapperImplementation(string reference_object) {
         return Declaration() + ReferenceWrapperImplementation(reference_object);
      }

      public SystemType ReturnType => GetReturnType();

      public string TypeName() => ReturnType.Name;
      public override string ToString() {
         return RefName;
      }
   }

   public static bool IsVoidType(this SystemType t) => t == typeof(void);


   public class FieldInfo : InfoClass {
      public FieldInfo(Field system) {
         this.system = system;
         IsStatic = system.IsStatic;
         IsPublic = system.IsPublic;
         IsReadOnly = system.IsInitOnly;
         IsConstLiteral = system.IsLiteral;
         if (IsConstLiteral) {
            literal_val = AsCSharpCode(system.GetValue(null));
         }
      }

      string literal_val;

      public bool IsConstLiteral;
      public bool IsReadOnly;

      public Field system;

      public override void Init() {

      }

      protected override SystemType GetReturnType() {
         return system.FieldType;
      }
      public override string RefName => system.Name;

      public override string Declaration() {
         if (IsConstLiteral) {
            return $"const {DeclarationFlag}{ReturnType.Name} {RefName} = {literal_val};";
         }
         if (IsReadOnly) return $"readonly {DeclarationFlag}{ReturnType.Name} {RefName};";
         return $"{DeclarationFlag}{ReturnType.Name} {RefName};";
      }
      public override string DefaultFullReferenceWrapperImplementation(string reference_object) {
         if (IsConstLiteral) {
            return $"const {DeclarationFlag}{ReturnType.Name} {RefName} = {reference_object}.{RefName};";
         }
         if (IsReadOnly)
            return $"{DeclarationFlag}{ReturnType.Name} {RefName} {{ get => {reference_object}.{RefName};}}";
         return $"{DeclarationFlag}{ReturnType.Name} {RefName} {{ get => {reference_object}.{RefName}; set => {reference_object}.{RefName}; }}";
      }
   }

   public static string AsCSharpCode(object o) {
      if (o == null) return "null";
      if (o is string s) {
         return s.EscapeCode();
      }

      return o.ToString();
   }

   public class ParameterInfo : InfoClass {
      public ParameterInfo(Parameter system) {
         this.system = system;
         IsPublic = false;
         ParamArray = system.GetCustomAttribute<System.ParamArrayAttribute>() != null;
      }
      public Parameter system;

      public bool ParamArray;

      public TypeInfo ParameterType;

      public bool HasDefaultValue;
      public object DefaultValue;


      public bool IsOut;
      public bool IsRef;

      public string arg_mod = "";
      public string arg_right_mod = "";

      public override void Init() {

         HasDefaultValue = system.HasDefaultValue;

         DefaultValue = system.DefaultValue;

         var base_parameter_type = ReturnType;

         if (system.IsOut) {
            IsOut = true;
            arg_mod = "out ";
            base_parameter_type = base_parameter_type.GetElementType();
         } else if (base_parameter_type.IsByRef) {
         }

         if (ParamArray) {
            arg_mod = "params " + arg_mod;
         }

         if (HasDefaultValue) {
            arg_right_mod = $" = " + AsCSharpCode(DefaultValue);
         }

         ParameterType = Parse(base_parameter_type);

      }

      protected override SystemType GetReturnType() {
         return system.ParameterType;
      }
      public override string RefName => system.Name;

      public override string Declaration() {
         return $"{DeclarationFlag}{arg_mod}{ParameterType.Name} {RefName}{arg_right_mod}";
      }

   }

   public const string not_implemented_str = "throw new System.NotImplementedException();";

   public class MethodInfo : InfoClass {
      public MethodInfo(Method system) {
         this.system = system;
         IsStatic = system.IsStatic;
         IsPublic = system.IsPublic;
      }
      public Method system;
      public ParameterInfo[] parameters;


      public TypeInfo ParsedReturnType;


      string parameter_string;
      string parameter_pack;

      public override void Init() {

         ParsedReturnType = Parse(ReturnType);
         parameters = system.GetParameters().ArrayMap(Parse);

         parameter_string = parameters.Map(x => x.Declaration()).Join(", ");

         parameter_pack = parameters.Map(x => x.RefName).Join(", ");
      }
      protected override SystemType GetReturnType() {
         return system.ReturnType;
      }
      public override string RefName => system.Name;

      public override string Declaration() {

         return $"{DeclarationFlag}{ParsedReturnType.Name} {RefName} ({parameter_string})";
      }

      public override string DefaultImplementation() {
         return $" {{\n{not_implemented_str}\n}}";
      }
      public override string ReferenceWrapperImplementation(string reference_object) {
         if (ParsedReturnType.IsVoid) return $" {{\n  {reference_object}.{RefName}({parameter_pack});\n}}";
         return $" {{\n  return {reference_object}.{RefName}({parameter_pack});\n}}";
      }
   }
   public static bool IsStatic(this SystemType t) {
      var c = t.GetConstructors();
      return (t.IsAbstract && t.IsSealed && c.Length == 0);
   }



   public static List<string> WrapIndent(this List<string> s) {
      return s.Indent("  ").WrapBrackets();
   }
   public static string WrapIndentJoin(this List<string> s) {
      return s.Indent("  ").WrapBrackets().Join("\n");
   }

   public static List<string> WrapBrackets(this List<string> s) {

      s.Add("}");
      s[0] = "{\n" + s[0];
      return s;
   }

   public static List<string> Indent(this List<string> s, string steps) {
      var ns = "\n" + steps;
      for (int i = 0; i < s.Count; ++i) {
         string a = s[i].Replace("\n", ns);
         s[i] = steps + a;
      }
      return s;
   }

   public class PropertyInfo : InfoClass {
      public PropertyInfo(Property system) {
         this.system = system;
      }


      TypeInfo ReturnTypeInfo;

      public MethodInfo getter;
      public MethodInfo setter;


      public Property system;
      public override void Init() {
         getter = Parse(system.GetGetMethod());
         setter = Parse(system.GetSetMethod());
         IsStatic = (getter ?? setter).IsStatic;

         IsPublic = getter.IsPublic || setter.IsPublic;

         ReturnTypeInfo = Parse(ReturnType);
      }


      protected override SystemType GetReturnType() {
         return system.PropertyType;
      }
      public override string RefName => system.Name;

      static bool a => throw new System.Exception();

      public override string Declaration() {

         List<string> args = new List<string>();

         if (setter) {
            args.Add($"set => {not_implemented_str}");
         }
         if (getter) {
            args.Add($"get => {not_implemented_str}");
         }

         return $"{DeclarationFlag}{ReturnTypeInfo.Name} {RefName} {args.WrapIndentJoin()}";
      }
      public override string DefaultFullReferenceWrapperImplementation(string reference_object) {

         List<string> args = new List<string>();

         if (setter) {
            args.Add($"set => {reference_object}.{RefName} = value;");
         }
         if (getter) {
            args.Add($"get => {reference_object}.{RefName};");
         }

         return $"{DeclarationFlag}{ReturnTypeInfo.Name} {RefName} {args.WrapIndentJoin()}";
      }
   }

}