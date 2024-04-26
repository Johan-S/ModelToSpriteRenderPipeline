using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using System;

[System.Serializable]
public class SaveFileHeader {

   [Data(id = 1)]
   public string category_name;
   [Data(id = 2)]
   public string description;
   [Data(id = 3)]
   public string date;
   [Data(id = 4)]
   public bool quick_save;
   [Data(id = 5)]
   public bool auto_save;

   [Data(id = 6)]
   public string game_version;

   [Data(id = 7)]
   public List<string> mods = new List<string>();

   public int type_id;
}

public interface SaveFileHeaderContainer {
   SaveFileHeader file_header {
      get; set;
   }
}
