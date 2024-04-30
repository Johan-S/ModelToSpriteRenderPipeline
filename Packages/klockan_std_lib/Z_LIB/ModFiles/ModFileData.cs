using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

public static partial class ModFile {
   public class TopNameSymbol : Named {
      public string name { get; set; }
      public string object_name;
      public string type_name;
      public string number_pattern;
      public string ref_name;
      public string ref_type;
      public TypeExpression value;
      public string source_text;

      public string FullTopName() {
         string n = type_name ?? name;

         return $"{n}[{object_name}]";
      }

      public FileMetadata file_metadata;

      public bool merge;
      public TopNameSymbol Clone() {
         var ttc = (TopNameSymbol)MemberwiseClone();

         if (value != null) value = value.Clone();
         return ttc;
      }
   }
   public class TypeExpression {
      public string name;
      public List<(string, TypeExpression)> arguments = new List<(string, TypeExpression)> { };

      public string source_file;
      public int? value;
      public string string_value;
      public string pattern_key;
      public string object_ref;
      public TypeExpression pattern_math;

      public bool top_name;
      public string full_top_name;

      public bool math;
      public List<TypeExpression> math_operands = new List<TypeExpression> { };
      public List<TypeExpression> math_operators = new List<TypeExpression> { };

      public string math_function_name;
      public List<TypeExpression> math_function_arguments = new List<TypeExpression> { };

      public TypeExpression WithType(string t) {
         TypeExpression res = (TypeExpression)MemberwiseClone();
         res.name = t;
         return res;
      }

      public string ReferenceName() {
         return $"{name}[{object_ref}]";
      }

      public string ReferenceKey() {
         if (pattern_key != null) {
            return $"{name}[{object_ref}, {pattern_key}]";
         }
         if (pattern_math != null) {
            return $"{name}[{object_ref}, {pattern_math}]";
         }
         return $"{name}[{object_ref}]";
      }
      public TypeExpression Clone() {
         var ttc = (TypeExpression)MemberwiseClone();

         arguments = new List<(string, TypeExpression)>(arguments);

         return ttc;
      }
   }

}
