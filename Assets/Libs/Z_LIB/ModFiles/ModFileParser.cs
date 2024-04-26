using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

public static partial class ModFile {

   static TypeExpression IntExp(int i) {
      return new TypeExpression { value = i };
   }
   static TypeExpression Pattern(string i) {
      return new TypeExpression { pattern_key = i };
   }
   static TypeExpression FunctionCall(string function, List<TypeExpression> args) {
      return new TypeExpression { math = true, math_function_name = function, math_function_arguments = args };
   }

   public static List<TopNameSymbol> ParseFile(string file_name) {

      var t = File.ReadAllText(file_name);


      ModFileSyntax parser = new ModFileSyntax();
      parser.parse = new StringParser(t);

      parser.ParseFile(file_name);

      return parser.res;
   }
   public static List<TopNameSymbol> ParseFiles(IEnumerable<string> file_names) {

      ModFileSyntax parser = new ModFileSyntax();
      foreach (var file_name in file_names) {
         var t = File.ReadAllText(file_name);

         parser.parse = new StringParser(t);

         parser.ParseFile(file_name);
      }

      return parser.res;
   }

   public class FileMetadata {

      public string top_file_name;

      public int load_order;

      public string mod_name;
      public List<string> tags = new List<string>();

      public List<string> description = new List<string>();
   }


   // file structure:

   // mod_name "Klockans Debug Mod"
   // mod_order 100
   // mod_tag "visual_only"
   // mod_description "Add debug rules"
   // mod_description "Useful for testing!"

   // file => top_object | top_object file


   // top_object => top_name pattern = type_expression | top_name = type_expression
   // top_name => symbol | symbol top_name

   // type_expression => symbol ( arguments ) | symbol | number | pattern
   // infered_expression => ( arguments ) | type_expression
   // arguments => arg argument_continuation
   // argument_continuation => ,+ arguments | ,*
   // arg = symbol: type_expression | symbol(arguments) | symbol: infered_expression | symbol

   // number: digit+
   // digit: [0-9]



   // Note: every single type is reducable.

   // normal integers are summed
   // min integers are mined
   // max integers are maxed
   // objects are a collection unique by name
   // multiple objects of same name gets reduced

   // object reduction types:
   // unique, drop extra values
   // max, these have a numeric identifier, take highest of them and drop rest.
   // merge, all sub fields gets reduced together

   // example:
   // UnitBonus, merge
   // DefenseBonus, merge, keyed by filter
   class ModFileSyntax {


      public StringParser parse;

      public List<TopNameSymbol> res = new List<TopNameSymbol>();

      public string current_source_file;

      public void ParseFile(string file_name) {
         current_source_file = file_name;
         parse.cur_file = file_name;
         parse.commented_line_pattern = "//";

         var fm = ParseFileMetadata();
         fm.top_file_name = System.IO.Path.GetFileName(file_name);


         while (!parse.Done()) {
            parse.SkipWhitespace();
            if (parse.Done()) break;
            bool merge = false;

            int i_start = parse.GetPointer();

            if (parse.MaybeDropWord("merge")) {
               merge = true;
            }


            var a = ParseTopObject();
            a.file_metadata = fm;
            a.merge = merge;
            a.value.source_file = current_source_file;
            a.value.top_name = true;
            a.value.full_top_name = a.FullTopName();
            res.Add(a);
            int i_end = parse.GetPointer();
            a.source_text = parse.GetStringBetween(i_start, i_end);
         }
      }

      public FileMetadata ParseFileMetadata() {

         FileMetadata cur_file = new FileMetadata();

         while (true) {
            if (parse.MaybeDropWord("mod_order")) {
               if (parse.MaybeReadInt(out int order)) {
                  cur_file.load_order = order;
               }
            } else if (parse.MaybeDropWord("mod_name")) {
               var sl = ParseStringLiteral();
               cur_file.mod_name = sl;
            } else if (parse.MaybeDropWord("mod_description")) {
               var sl = ParseStringLiteral();
               cur_file.description.Add(sl);
            } else if (parse.MaybeDropWord("mod_tag")) {
               var sl = ParseStringLiteral();
               cur_file.tags.Add(sl);
            } else {
               break;
            }
         }

         return cur_file;
      }

      System.Exception GetError() {
         int object_c = res.Count;
         string extra_message = $"at object {object_c}";
         if (object_c > 0) {
            extra_message = $"after {object_c} ({res[object_c - 1].name})";
         }
         var state = parse.GetPositionDescriptor();
         return new System.Exception(state.Message(extra_message));
      }
      System.Exception GetErrorExtraDetails(string extra_message) {
         int object_c = res.Count;
         if (object_c > 0) {
            extra_message = $"{extra_message}!\n  after {object_c} ({res[object_c - 1].name})";
         }
         var state = parse.GetPositionDescriptor();
         return new System.Exception(state.Message(extra_message));
      }

      // top_object => top_name pattern = type_expression | top_name = type_expression
      // type_expression => symbol ( arguments ) | symbol | number | pattern

      string current_top_name;

      public TopNameSymbol ParseTopObject() {
         var state_in = parse.GetState();

         string top_name;
         var o_ref = ParseObjectReference();
         if (o_ref != null) {
            top_name = o_ref.ReferenceName();
            current_top_name = top_name;
            if (top_name == null) throw GetError();
            var ex = ParseKnownTypeExpression(o_ref.name);
            if (ex == null) throw GetError();
            return new TopNameSymbol {
               name = top_name,
               object_name = o_ref.object_ref,
               type_name = o_ref.ReferenceKey(),
               number_pattern = o_ref.pattern_key,
               ref_name = o_ref.object_ref,
               ref_type = o_ref.name,
               value = ex,
            };
         } else {
            // throw GetError();
            throw GetErrorExtraDetails("Tried to parse new top name symbol but failed!");
         }
      }
      public TypeExpression ParseUntypedObjectReference() {
         var state_in = parse.GetState();
         TypeExpression res = new TypeExpression();

         if (BeginParen('[', ']')) {
            var object_ref = ParseTopName();
            if (object_ref == null) {
               parse.SetState(state_in);
               return null;
            }
            if (parse.MaybeDropWord(",")) {
               var math = ParseMath();
               if (math != null) {
                  res.pattern_math = math;
               } else {
                  res.pattern_key = parse.ReadWord();
               }
            }

            if (EndParen('[', ']')) {
            }
            res.object_ref = object_ref;

            return res;
         }
         parse.SetState(state_in);
         return null;
      }

      public TypeExpression ParseTypedObjectReference(string symbol) {
         TypeExpression res = ParseUntypedObjectReference();
         if (res != null) {
            res.name = symbol;
         }
         return res;
      }

      public TypeExpression ParseObjectReference() {
         var state_in = parse.GetState();

         var symnbol = ParseSymbol();
         if (symnbol == null) {
            parse.SetState(state_in);
            return null;
         }
         var res = ParseTypedObjectReference(symnbol);
         if (res == null) {
            parse.SetState(state_in);
            return null;
         }
         return res;
      }
      public string ParseRefName() {
         var state_in = parse.GetState();
         var symbol = ParseSymbol();

         if (symbol == null) {
            // drop state when fail.
            parse.SetState(state_in);
            return null;
         }

         var continuation = ParseTopName();
         if (continuation != null) return symbol + " " + continuation;
         return symbol;
      }

      // top_name => symbol | symbol top_name
      public string ParseTopName() {
         var state_in = parse.GetState();
         var symbol = parse.ReadComplexWord();

         if (symbol == null) {
            // drop state when fail.
            parse.SetState(state_in);
            return null;
         }

         var continuation = ParseTopName();
         if (continuation != null) return symbol + " " + continuation;
         return symbol;
      }
      public void ParseNumericName() {
      }
      // type_expression => symbol ( arguments ) | symbol | number | pattern
      // infered_expression => ( arguments ) | type_expression
      public TypeExpression ParseTypeExpression() {
         var state_in = parse.GetState();
         TypeExpression res = new TypeExpression();

         if (parse.MaybeReadInt(out int integer_expression)) {
            return IntExp(integer_expression);
         }

         string type_name = ParseSymbol();

         if (type_name == null) {
            parse.SetState(state_in);
            return null;
         }
         return ParseKnownTypeExpression(type_name);
      }

      public TypeExpression ParseKnownTypeExpression(string type_name) {
         var state_in = parse.GetState();
         if (BeginParen('(', ')')) {
            List<(string, TypeExpression)> arguments = ParseArguments();

            if (arguments == null) {
               parse.SetState(state_in);
               return null;
            }

            if (!EndParen('(', ')')) {
               parse.SetState(state_in);
               return null;
            }
            TypeExpression res = new TypeExpression();
            res.name = type_name;
            res.arguments = arguments;

            return res;
         }

         var oref = ParseTypedObjectReference(type_name);

         if (oref != null) {
            return oref;
         }

         {
            TypeExpression res = new TypeExpression();
            res.string_value = type_name;
            return res;
         }

      }

      public bool BeginParen(char left_paren, char right_paren) {
         var state_in = parse.GetState();
         var b = parse.ReadNextVisible();
         if (b != left_paren) {
            parse.SetState(state_in);
            return false;
         }
         return true;
      }
      public bool EndParen(char left_paren, char right_paren) {
         var state_in = parse.GetState();
         var b = parse.ReadNextVisible();
         if (b != right_paren) {
            parse.PopChar();
            throw GetErrorExtraDetails($"Missing closing symbol '{right_paren}' for object \"{current_top_name}\"");
         }
         return true;
      }

      public void SkipParenStructure(char left_paren, char right_paren) {
         var b = parse.ReadNextVisible();
         if (b != left_paren) {
            throw GetError();
         }
         int i = 1;
         while (i > 0) {
            char n = parse.ReadNextVisible();
            if (n == left_paren) {
               i++;
            }
            if (n == right_paren) {
               i--;
            }
         }
      }

      public void ParseInferredExpression() {
      }
      // arguments => arg argument_continuation
      // argument_continuation => ,+ arguments | ,*
      public List<(string, TypeExpression)> ParseArguments() {
         var state_in = parse.GetState();
         List<(string, TypeExpression)> res = new List<(string, TypeExpression)>();

         var sub_state = parse.GetState();

         bool going = true;
         while (going) {
            if (parse.MaybeDropWord(",")){
            } else {
               var arg = ParseArg();
               if (arg.Item1 == null) {
                  going = false;
               } else {
                  if (arg.Item2 != null) {
                     arg.Item2.source_file = current_source_file;
                  }
                  res.Add(arg);
               }
            }
         }
         return res;
      }

      public string ParseStringLiteral() {
         if (BeginParen('"', '"')) {
            var str = parse.ReadUntilSeparator("\"");
            return str;
         }
         return null;
      }

      // arg = symbol: type_expression | symbol(arguments) | symbol: infered_expression | symbol
      public (string, TypeExpression) ParseArg() {
         var state_in = parse.GetState();

         string symbol = ParseSymbol();

         if (symbol == null) {
            parse.SetState(state_in);
            return (null, null);
         }
         if (parse.MaybeDropWord(".")) {
            var sub_arg = ParseArg();
            if (sub_arg.Item1 == null) {
               throw GetErrorExtraDetails($"Invalid sub expression '{symbol}.' for object \"{current_top_name}\"");
            }
            return ($"{symbol}.{sub_arg.Item1}", sub_arg.Item2);
         }

         if (parse.MaybeDropWord(":")) {
            var res = ParseTypeExpression();
            if (res != null) {
               return (symbol, res);
            }
         }
         parse.MaybeDropWord(":");
         if (BeginParen('(', ')')) {
            var args = ParseArguments();
            if (args == null) {
               parse.SetState(state_in);
               return (null, null);
            }
            if (!EndParen('(', ')')) {
               parse.SetState(state_in);
               return (null, null);
            }
            return (symbol, new TypeExpression {
               arguments = args,
            });
         }
         {
            var oref = ParseUntypedObjectReference();
            if (oref != null) {
               return (symbol, oref);
            }
         }
         if (BeginParen('"', '"')) {
            var str = parse.ReadUntilSeparator("\"");
            TypeExpression res = new TypeExpression();
            res.string_value = str;
            return (symbol, res);
         }

         {
            var m = ParseMath();
            if (m != null) {
               return (symbol, m);
            }
         }

         return (symbol, null);
      }
      // symbol => word_letter+
      // word_letter => [A-Z, a-z, _]

      public string ParseSymbol() {
         return parse.ReadSymbol();
      }
      // number: digit+
      // digit: [0-9]

      // math => { math_expression }

      // math_expression => value | value op math_expression
      // math_value => symbol(math_arguments) | (math_expression) | digit | pattern
      // math_arguments => math_value | math_value , math_arguments
      // math_op => +-/*
      public TypeExpression ParseMath() {
         if (!BeginParen('{', '}')) {
            return null;
         }
         var res = ParseMathExpression();
         if (!EndParen('{', '}')) {
            return null;
         }
         return res;
      }
      public TypeExpression ParseMathExpression() {

         var val  = ParseMathValue();

         if (val == null) {
            throw GetErrorExtraDetails($"Invalid math expression for object \"{current_top_name}\"");
         }
         List<TypeExpression> operands = new List<TypeExpression> { val };
         List<TypeExpression> operators = new List<TypeExpression>();

         while (true) {
            var op = ParseMathOp();

            if (op == null) {
               return new TypeExpression {
                  name = "Expression",
                  math = true,
                  math_operands = operands,
                  math_operators = operators,
               };
            }
            var val2 = ParseMathValue();
            if (val2 == null) {
               throw GetErrorExtraDetails($"Missing right side value of operator for object \"{current_top_name}\"");
            }
            operands.Add(val2);
            operators.Add(op);
         }
      }
      public List<TypeExpression> ParseMathArguments() {

         List<TypeExpression> res = new List<TypeExpression>();
         if (parse.PeekChar() == ')') return res;

         do {
            var val = ParseMathExpression();
            if (val == null) {
               throw GetError();
            }
            res.Add(val);
         } while (parse.MaybeDropWord(","));
         return res;
      }
      public TypeExpression ParseMathValue() {
         if (BeginParen('(', ')')) {
            var te = ParseMathExpression();
            if (!EndParen('(', ')')) {
               return null;
            }
            return te;
         }

         if (parse.MaybeReadInt(out int integer_expression)) {
            return IntExp(integer_expression);
         }
         if (parse.MaybeReadSymbol(out string symbol)) {
            if (BeginParen('(', ')')) {
               var args = ParseMathArguments();
               if (!EndParen('(', ')')) {
                  return null;
               }
               return FunctionCall(symbol, args);
            }

            return Pattern(symbol);
         }

         return null;
      }

      public TypeExpression GetMathOp(char c) {
         if (c == '+' || c == '-' || c == '/' || c == '*') {
            return new TypeExpression {
               name = $"{c}",
               math = true,
            };
         }
         return null;
      }

      public TypeExpression ParseMathOp() {
         if (parse.Done()) return null;
         char c = parse.ReadNextVisible();
         var op = GetMathOp(c);
         if (op == null) {
            parse.PopChar();
            return null;
         }
         return op;
      }

   }
}
