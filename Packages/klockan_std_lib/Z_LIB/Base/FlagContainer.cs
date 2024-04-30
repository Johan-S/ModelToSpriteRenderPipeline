using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct FlagContainer {
   public ulong flag_impl;

   public readonly FlagType type_impl;

   public FlagContainer(FlagType type_constructor) {

      flag_impl = 0;
      type_impl = type_constructor;
   }

   const ulong one = 1;

   public bool is_set(int id) {
      return (flag_impl & (one << id)) != 0;
   }
   public void set(int id) {
      flag_impl |= one << id;
   }

   public void set_all_from(FlagContainer id) {
      flag_impl |= id.flag_impl;
   }
   public bool is_all_aet_from(FlagContainer id) {
      return (flag_impl & id.flag_impl) == id.flag_impl;
   }
   public bool is_any_set_from(FlagContainer id) {
      return (flag_impl & id.flag_impl) != 0;
   }

   public void clear_flag(int id) {
      flag_impl &= ~(one << id);
   }

   public IEnumerable<int> all_set_flags {
      get {
         ulong res = flag_impl;
         int id = 0;
         while (res != 0) {
            if (res % 2 == 1) {
               yield return id;
            }
            id++;
            res >>= 1;
         }
      }
   }

   public IEnumerable<string> all_set_flag_names {
      get {
         ulong res = flag_impl;
         int id = 0;
         while (res != 0) {
            if (res % 2 == 1) {
               yield return type_impl.type_name[id];
            }
            id++;
            res >>= 1;
         }
      }
   }

   public class FlagType {

      public FlagType(List<string> names) {
         type_name = names;
         Debug.Assert(names.Count <= 64);
      }

      public List<string> type_name;
   }

}