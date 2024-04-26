using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class StringParser {

   public class PositionDescriptor {
      public List<string> rows = new List<string>();
      public int row;
      public int column;
      public string file;

      public string clipped_row;

      public bool end_of_file;
      string LocMessage() {
         if (file != null) {
            return $"{file}, row: {row}, column: {column}";
         }
         return $"row: {row}, column: {column}";
      }


      public string Message() {
         return $"Error at {LocMessage()}: \"{rows[row]}\"";
      }
      public string Message(string sub_msg) {

         if (end_of_file) {
            return $"Error End of File: {sub_msg}";
         }
         string s = rows[row];
         string remaining = s.Substring(column);
         s = s.Trim();
         return $"Error at {LocMessage()}:\n  {sub_msg}\n    around: {remaining}\"{s}\"";
      }
   }

   public static StringParser Parse(string s) {
      return new StringParser(s);
   }

   public class ParserState {
      public string cur;
      public int i;
   }

   public string commented_line_pattern;

   public string cur_file;

   public List<string> comment_lines = new List<string>();

   public ParserState GetState() {
      return new ParserState {
         cur = cur,
         i = i,
      };
   }
   public void SetState(ParserState state) {
      i = state.i;
      cur = state.cur;
   }

   public PositionDescriptor GetPositionDescriptor() {
      PositionDescriptor desc = new PositionDescriptor();

      desc.file = cur_file;

      string cur_row = "";
      bool ifound = false;
      for (int a = 0; a < cur.Length; ++a) {
         char c = cur[a];
         cur_row += c;
         if (i == a) {
            desc.row = desc.rows.Count;
            desc.column = cur_row.Length - 1;
            ifound = true;
         }
         if (c == '\n') {
            desc.rows.Add(cur_row);
            cur_row = "";
         }
      }
      desc.rows.Add(cur_row);
      if (!ifound) {
         desc.end_of_file = true;
         desc.row = desc.rows.Count - 1;
         desc.column = cur_row.Length - 1;
      }
      return desc;
   }

   public StringParser(string s) {
      cur = s;
   }

   string cur;
   int i;

   string remaining => cur.Substring(i);

   int FindSeparator(string separator) {
      int next = cur.IndexOf(separator, i);
      if (next == -1) {
         throw new Exception($"Couldn't find \"{separator}\" in \"{remaining}\"");
      }
      return next;
   }

   public string ReadUntilSeparator(string separator) {
      SkipWhitespace();
      int next = FindSeparator(separator);
      var res = cur.Substring(i, next - i);
      i = next + separator.Length;
      return res;
   }
   public string ReadUntil(string separator) {
      SkipWhitespace();
      int next = FindSeparator(separator);
      var res = cur.Substring(i, next - i);
      return res;
   }

   public void DropSeparator(string separator) {
      SkipWhitespace();
      int next = FindSeparator(separator);
      if (next != i) throw new Exception($"Expected to find sepaeator \"{separator}\" at \"{remaining}\"");
      i = next + separator.Length;
   }
   public bool MaybeDropWord(string separator) {
      int i_in = i;
      SkipWhitespace();
      if (i + separator.Length > cur.Length) {
         i = i_in;
         return false;
      }
      if (cur.Substring(i, separator.Length) == separator) {
         i += separator.Length;
         return true;
      }
      i = i_in;
      return false;
   }

   public string ReadRemaining() {
      SkipWhitespace();
      string res = remaining;
      i = cur.Length + 1;
      return res;
   }
   static bool IsSymbolStartChar(char c) {
      return char.IsLetter(c) || c == '_';
   }

   static bool IsSymbolChar(char c) {
      return char.IsLetterOrDigit(c) || c == '_' || c == '-';
   }
   static bool IsComplexChar(char c) {
      return char.IsLetterOrDigit(c) || c == '_' || c == '\'' || c == '"' || c == '-' || c == '+';
   }

   public string ReadWord() {
      SkipWhitespace();
      int next = i;
      while (next < cur.Length && IsSymbolChar(cur[next])) next++;
      var res = cur.Substring(i, next - i);
      i = next;
      return res;
   }
   public string ReadComplexWord() {
      SkipWhitespace();
      int next = i;
      if (next >= cur.Length) return null;
      char start_char = cur[next];
      if (!IsComplexChar(start_char)) {
         return null;
      }
      while (next < cur.Length && IsComplexChar(cur[next])) next++;
      var res = cur.Substring(i, next - i);
      i = next;
      return res;
   }

   public string ReadSymbol() {
      SkipWhitespace();
      int next = i;
      if (next >= cur.Length) return null;
      char start_char = cur[next];
      if (!IsSymbolStartChar(start_char)) {
         return null;
      }
      while (next < cur.Length && IsSymbolChar(cur[next])) next++;
      var res = cur.Substring(i, next - i);
      i = next;
      return res;
   }

   public T ReadEnum<T>() {
      var w = ReadWord();
      return (T)Enum.Parse(typeof(T), w);
   }

   public void PopChar() {
      i--;
   }
   public char PeekChar() {
      SkipWhitespace();
      if (i >= cur.Length) return '\0';
      return cur[i];
   }

   public char ReadNextVisible() {
      SkipWhitespace();
      if (i >= cur.Length) return '\0';
      return cur[i++];
   }

   public int ReadInt() {
      if (MaybeReadInt(out int res)) {
         return res;
      }
      throw new Exception($"Invalid integer '{res}' in string '{cur}'");
   }
   static bool IsNumberStartChar(char c) {
      return (char.IsDigit(c) || c == '-');
   }

   static bool IsNumberChar(char c) {
      return char.IsDigit(c);
   }

   public bool MaybeReadInt(out int ires) {
      int start = i;
      SkipWhitespace();
      int next = i;
      if (next >= cur.Length) {
         i = start;
         ires = -1;
         return false;
      }
      char start_char = cur[next];
      if (!IsNumberStartChar(start_char)) {
         i = start;
         ires = -1;
         return false;
      }
      next++;
      while (next < cur.Length && IsNumberChar(cur[next])) next++;
      var res = cur.Substring(i, next - i);
      i = next;
      if (int.TryParse(res, out ires)) {
         return true;
      }
      i = start;
      ires = -1;
      return false;
   }
   public bool MaybeReadSymbol(out string ires) {
      int start = i;
      SkipWhitespace();
      int next = i;
      if (next >= cur.Length) {
         i = start;
         ires = null;
         return false;
      }
      char start_char = cur[next];
      if (!IsSymbolStartChar(start_char)) {
         i = start;
         ires = null;
         return false;
      }
      while (next < cur.Length && IsSymbolChar(cur[next])) next++;
      ires = cur.Substring(i, next - i);
      i = next;
      return true;
   }

   bool StartsWithIndex(string big, int i, string small) {
      for (int x = 0; x < small.Length; ++x) {
         int ii = i + x;
         if (ii >= big.Length) return false;
         if (big[ii] != small[x]) return false;
      }
      return true;
   }

   public void SkipWhitespace() {
      while (i < cur.Length && char.IsWhiteSpace(cur[i])) i++;
      if (commented_line_pattern != null) {
         if (StartsWithIndex(cur, i, commented_line_pattern)) {
            i += commented_line_pattern.Length;
            int start = i;

            while (i < cur.Length && cur[i] != '\n' && cur[i] != '\r') i++;

            // comment_lines.Add(cur.Substring(start, i - start));

            SkipWhitespace();
         }
      }
   }
   public string GetStringBetween(int istart, int iend) {
      return cur.Substring(istart, iend - istart);

   }
   public int GetPointer() => i;

   public bool Done() {
      return i >= cur.Length;
   }

}
