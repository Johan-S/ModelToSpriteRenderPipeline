using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading.Tasks;

public static class SaveData {

   public static class Local {
      public static void Save(string file_path, object data) {
         var type = data.GetType();
         using (FileStream fs = new FileStream(file_path, FileMode.Create)) {
            var bytes = SerializeData(data);
            bytes_written_to_file += bytes.Length;
            fs.Write(bytes, 0, bytes.Length);
            fs.Flush();
         }
      }
      // 50 mb
      const int max_file_read_size = (1 << 20) * 50;

      public static T Load<T>(string file_path) {

         var res = Load(file_path);
         if (res is T t) return t;

         throw new System.Exception($"Tried to load {typeof(T).Name} but got {res.GetType().Name}, in file: {file_path}");
      }
      public static object Load(string file_path) {
         if (!File.Exists(file_path)) return null;

         using (FileStream fs = new FileStream(file_path, FileMode.Open)) {

            if (fs.Length > max_file_read_size) {
               Debug.LogError($"Tried to read too large file!");
               return null;
            }

            bytes_read_from_file += fs.Length;
            return ParseData(fs);
         }
      }
      public static Task<object> LoadAsync(string file_path) {
         if (!File.Exists(file_path)) return null;

         using (FileStream fs = new FileStream(file_path, FileMode.Open)) {

            if (fs.Length > max_file_read_size) {
               Debug.LogError($"Tried to read too large file!");
               return null;
            }

            bytes_read_from_file += fs.Length;
            return ParseDataAsync(fs);
         }
      }
   }

   public static long bytes_read_from_file;
   public static long bytes_written_to_file;

   public static string GetBytesMessage() {

      long ram = System.GC.GetTotalMemory(false);

      var ram_str = ram.ToBigString_Scientific();

      return $"FileUsage{{ram: {ram_str}, read: {bytes_read_from_file}, written: {bytes_written_to_file}}}";
   }
   public static string GetRamMessage() {
      long ram = System.GC.GetTotalMemory(false);
      var ram_str = ram.ToBigString_Scientific();
      return $"Ram Usage: {ram_str}B";
   }
   public static void DeleteDirectory_Unsafe(string loc) {
      string file_path = InternalPath(top_save_directory, loc);
      try {
         if (Directory.Exists(file_path)) {
            Debug.Log($"Deleting {file_path} recursively!");
            Directory.Delete(file_path, recursive: true);
         } else {

            Debug.Log($"Tried but didn't find directory {file_path}!");
         }
      } catch (IOException e) {
         Debug.Log(e);
      }
   }

   public class SaveDirectory {
      public string directory_path;
   }

   public static SaveData.SaveDirectory top_save_directory;

   public static void SetTopSaveDirectory(SaveDirectory new_top) {
      if (top_save_directory?.directory_path != new_top?.directory_path) {
         top_save_directory = new_top;
         EnsureSaveDirectory(top_save_directory);
      }
   }

   public static IEnumerable<string> GetTopSaveDirectories() {
      var sd = Application.persistentDataPath + "/Saves";

      if (!Directory.Exists(sd)) {
         Directory.CreateDirectory(sd);
         yield break;
      }

      foreach (var d in Directory.GetDirectories(sd)) {
         yield return System.IO.Path.GetFileName(d);
      }
   }

   public static void DeleteTopSaveDirectory(SaveDirectory new_top) {
      if (new_top?.directory_path != null) {
         Debug.Log($"Deleting profile {new_top.directory_path}");
         var p = InternalPath(new_top);

         if (Directory.Exists(p)) {
            Directory.Delete(p, recursive: true);
         }
      }
   }

   public static string GetPathFor(this SaveDirectory sd, string file_name) {
      if (sd?.directory_path == null) return file_name;
      return sd.directory_path + "/" + file_name;
   }

   public static void SaveBaseModText(string loc, string file_type, string mod_data) {
      SaveModText_Generic("BaseMod", loc, file_type, mod_data);
   }

   public static void SaveModText(string loc, string file_type, string mod_data) {
      SaveModText_Generic("Mods", loc, file_type, mod_data);
   }
   public static void SaveModText_Generic(string folder_path, string loc, string file_type, string mod_data) {
      string file_path = Generic_ModPath(folder_path, loc, file_type);
      File.WriteAllText(file_path, mod_data);
   }

   public static void Save(string loc, object data) {
      var type = data.GetType();
      string file_path = Path(loc, type);
      using (FileStream fs = new FileStream(file_path, FileMode.Create)) {
         var bytes = SerializeData(data);
         bytes_written_to_file += bytes.Length;
         fs.Write(bytes, 0, bytes.Length);
      }
   }

   public static byte[] SerializeData(object data) {
      if (MySerializer.CanSerialize(data.GetType())) {
         var ds = MySerializer.SerializeTop(data);
         return ds.GetBytes();
      } else {
         throw new System.Exception($"Type {data.GetType()} lacks serializer!");
      }

   }
   public static T ParseData<T>(byte[] data, int data_size = -1) {

      var ds = MySerializer.AsNewData(data, data_size);
      if (ds != null) {
         if (MySerializer.DeserializeTop(ds, out object res)) {
            if (res is T tres) return tres;
            throw new System.Exception($"Got wrong serialized type, expected {typeof(T).Name}, got {res.GetType().Name}");
         } else {
            throw new System.Exception($"Bad new Data!");
         }
      }
      throw new System.Exception($"Bad serialized data for type {typeof(T)}!");
   }

   static void EnsureSaveDirectory(SaveDirectory dir) {
      if (dir?.directory_path == null) return;
      if (dir.directory_path.Contains(".") || dir.directory_path.Contains(":")) return;
      EnsureDir(Application.persistentDataPath, "Saves\\" + dir.GetPathFor("Dummy"));
   }

   public static void EnsureSubSaveDirectory(SaveDirectory dir) {
      EnsureDir(Application.persistentDataPath, "Saves\\" + "Dummy");
      if (dir?.directory_path == null) return;
      if (dir.directory_path.Contains(".") || dir.directory_path.Contains(":")) return;
      EnsureDir(Application.persistentDataPath, "Saves\\" + top_save_directory.GetPathFor(dir.GetPathFor("Dummy")));
   }


   public static void AsyncSave(string loc, object data) {
      var type = data.GetType();
      string file_path = Path(loc, type);


      if (MySerializer.CanSerialize(data.GetType())) {
         var t = new System.Threading.Thread(() => {
            System.Threading.Thread.CurrentThread.IsBackground = true;
            try {
               var ds = MySerializer.SerializeTop(data);
               var bytes = ds.GetBytes();
               bytes_written_to_file += bytes.Length;
               using (FileStream fs = new FileStream(file_path, FileMode.Create)) {
                  fs.Write(bytes, 0, bytes.Length);
               }
            } catch (IOException e) {
               Debug.Log(e);
            }
         });
         t.Start();
      } else {
         throw new System.Exception($"Type {type} lacks serializer!");
      }
   }

   public static string PathToObjectName(string p) {
      string fn = System.IO.Path.GetFileName(p);

      int i = fn.LastIndexOf('.');
      if (i >= 0) return fn.Substring(0, i);
      return fn;
   }

   public static int CountObjectsInSave(SaveDirectory top_save_directory) {
      var p = InternalPath(top_save_directory);
      int res = 0;
      foreach (var d in Directory.GetFiles(p)) {
         res++;
      }
      foreach (var d in Directory.GetDirectories(p)) {
         res++;
      }
      return res;
   }

   public static List<string> ListSaveDirectories() {
      var sd = Application.persistentDataPath + "/Saves";
      List<string> res = new List<string>();
      foreach (var d in Directory.GetDirectories(sd + "/" + top_save_directory.GetPathFor(""))) {

         res.Add(System.IO.Path.GetFileName(d));
      }
      return res;
   }

   public static List<string> ListFiles(System.Type type, bool include_sub_dir = false, string only_sub_dir = null) {
      var sd = Application.persistentDataPath + "/Saves";

      if (include_sub_dir) {
         List<string> res = new List<string>();
         foreach (var d in Directory.GetDirectories(sd + "/" + top_save_directory.GetPathFor(""))) {
            if (only_sub_dir != null) {
               if (!d.EndsWith(only_sub_dir)) continue;
            }
            var path_match = $"*.{type.Name}";
            res.AddRange(new List<string>(Directory.GetFiles(d, path_match))
               .Map(PathToObjectName)
               .Map(x => System.IO.Path.GetFileName(d) + "/" + x)
               .Sorted());
         }
         return res;
      } else {
         var path_match = top_save_directory.GetPathFor($"*.{type.Name}");
         EnsureDir(Application.persistentDataPath, "Saves\\Dummy");
         return new List<string>(Directory.GetFiles(sd, path_match))
            .Map(PathToObjectName)
            .Sorted();
      }
   }

   public static List<string> ListFiles<T>(bool include_sub_dir = false) {
      return ListFiles(typeof(T), include_sub_dir);
   }
   public static T Load<T>(string loc) {
      return (T)Load(loc, typeof(T));
   }

   public static SaveFileHeader LoadHeaderFor(string loc, System.Type type) {
      string file_path = Path(loc, type);
      using (FileStream fs = new FileStream(file_path, FileMode.Open)) {
         return ParseHeader(fs);
      }
   }

   static byte[] header_bytes_unsafe = new byte[2048];
   public static SaveFileHeader ParseHeader(Stream stream) {
      stream.Read(header_bytes_unsafe, 0, header_bytes_unsafe.Length);
      return MySerializer.ReadHeader(header_bytes_unsafe);
   }
   public static object Load(string loc, System.Type type) {
      string file_path = Path(loc, type);

      if (!File.Exists(file_path)) return null;

      using (FileStream fs = new FileStream(file_path, FileMode.Open)) {
         bytes_read_from_file += fs.Length;
         return ParseData(fs);
      }
   }
   public static MemoryStream LoadData(string loc, System.Type type) {
      string file_path = Path(loc, type);

      MemoryStream mem_stream = new MemoryStream();
      using (FileStream fs = new FileStream(file_path, FileMode.Open)) {
         fs.CopyTo(mem_stream);
      }
      Dev.LogInfo($"Loaded {mem_stream.Length} bytes");
      bytes_read_from_file += mem_stream.Length;
      mem_stream.Position = 0;
      return mem_stream;
   }
   public static object ParseData(Stream stream) {


      var ds = MySerializer.AsNewData(new DataStream(stream));
      if (ds != null) {

         if (MySerializer.DeserializeTop(ds, out object res)) {
            return res;
         } else {
            throw new System.Exception($"Bad new Data!");
         }
      }
      throw new System.Exception($"Bad serialized data!");
   }
   public static async Task<object> ParseDataAsync(Stream stream) {
      var ds = MySerializer.AsNewData(new DataStream(stream));
      if (ds != null) {

         var res = Task.Run(() => {
            if (MySerializer.DeserializeTop(ds, out object ores)) {
               return ores;
            } else {
               throw new System.Exception($"Bad new Data!");
            }
         });
         await res;
         return res.Result;
      }
      throw new System.Exception($"Bad serialized data!");

   }
   public static void DeleteDirectory_Safe(string loc) {
      string file_path = InternalPath(top_save_directory, loc);
      try {
         Directory.Delete(file_path, recursive: false);
      } catch (IOException e) {
         Debug.Log(e);
      }
   }
   public static void Delete(string loc, System.Type type) {
      string file_path = Path(loc, type);
      try {
         System.IO.File.Delete(file_path);
      } catch (IOException e) {
         Debug.Log(e);
      }
   }


   static string InternalPath(SaveDirectory top_save_directory) {
      if (top_save_directory?.directory_path == null) return Application.persistentDataPath + "/Saves";
      return Application.persistentDataPath + "/Saves/" + top_save_directory.directory_path;
   }

   static string InternalPath(SaveDirectory top_save_directory, string obj) {
      var sd = Application.persistentDataPath + "/Saves";
      var local_path = top_save_directory.GetPathFor($"{obj}");
      var res = sd + $"/{local_path}";
      EnsureDir(Application.persistentDataPath, "Saves\\a");
      return res;
   }

   static string InternalPath(SaveDirectory top_save_directory, string obj, System.Type t) {
      var sd = Application.persistentDataPath + "/Saves";
      var local_path = top_save_directory.GetPathFor($"{obj}.{t.Name}");
      var res = sd + $"/{local_path}";
      EnsureDir(Application.persistentDataPath, "Saves\\a");
      return res;
   }

   static string Path(string obj, System.Type t) {
      return InternalPath(top_save_directory, obj, t);
   }
   public static string UserModDir() {

      var src = Application.persistentDataPath;
      var i = src.LastIndexOf('\\');
      i = src.LastIndexOf('/');
      src = src.Substring(0, i);
      src = src + "/WoM/Mods";

      if (!Directory.Exists(src)) {
         Debug.Log($"Created {src}");
         Directory.CreateDirectory(src);
      }

      return src;
   }
   public static string ModDir() {

      var p = Application.dataPath;

      var src = Application.dataPath;
      var i = src.LastIndexOf('\\');
      i = src.LastIndexOf('/');
      src = src.Substring(0, i);
      src = src + "/Mods";
      return src;
   }
   public static string ModPath(string obj, string t) {
      return Generic_ModPath("Mods", obj, t);
   }
   static string Generic_ModPath(string folder_path, string obj, string t) {
      var sd = Application.persistentDataPath + $"/{folder_path}";
      EnsureDir(sd, obj);
      var res = sd + $"/{obj}.{t}";

      return res;
   }

   public static void EnsureDir(string b, string name) {

      var dn = b;

      var full_path = System.IO.Path.GetFullPath(dn);

      name = name.Replace('/', '\\');

      var dp = System.IO.Path.GetFullPath(Application.dataPath);
      var op = System.IO.Path.GetFullPath(Application.persistentDataPath);

      if (!full_path.StartsWith(dp) && !full_path.StartsWith(op)) {
         throw new System.Exception($"Bad path: {full_path}, should be either {dp} or {op}");
      }
      if (!Directory.Exists(dn)) Directory.CreateDirectory(dn);
      foreach (var s in name.Split('\\')) {
         if (!Directory.Exists(dn)) Directory.CreateDirectory(dn);
         dn += "\\" + s;
      }
   }


   static bool warn_binary_formatter;

   /*
   static BinaryFormatter GetBinaryFormatter() {
      throw new System.Exception($"Don't read deserialized data!");
      if (!warn_binary_formatter) {
         Debug.LogError($"Using binary formatter!");
         warn_binary_formatter = true;
      }
      BinaryFormatter formatter = new BinaryFormatter();
      SurrogateSelector surrogateSelector = new SurrogateSelector();
      surrogateSelector.AddSurrogate(typeof(Vector2Int), new StreamingContext(StreamingContextStates.All), new Vector2IntSerializationSurrogate());

      formatter.SurrogateSelector = surrogateSelector;

      return formatter;
   }
   class Vector2IntSerializationSurrogate : ISerializationSurrogate {

      // Method called to serialize a Vector3 object
      public void GetObjectData(System.Object obj, SerializationInfo info, StreamingContext context) {

         Vector2Int v3 = (Vector2Int)obj;
         info.AddValue("x", v3.x);
         info.AddValue("y", v3.y);
      }

      // Method called to deserialize a Vector3 object
      public System.Object SetObjectData(System.Object obj, SerializationInfo info,
                                         StreamingContext context, ISurrogateSelector selector) {
         Vector2Int v3 = (Vector2Int)obj;
         v3.x = (int)info.GetValue("x", typeof(int));
         v3.y = (int)info.GetValue("y", typeof(int));
         obj = v3;
         return obj;
      }
   }
   */
}