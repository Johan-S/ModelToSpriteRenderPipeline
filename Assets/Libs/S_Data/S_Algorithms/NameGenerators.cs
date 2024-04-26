using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static partial class NameGenerators {


   public class NameClass : Named {

      public NameClass(string source_name, string[] source_data) {
         name = source_name;
         names = source_data.Map(x => x.Trim());

         shuffled_names = new List<string>(names);
         shuffled_names.Shuffle();
      }

      List<string> names;
      List<string> shuffled_names;

      int cur_index = 0;

      public string name {
         get;set;
      }

      public string GetRandomName_Deterministic( System.Func<int, int, int> Rand) {
         return names.Random(Rand);
      }

      public string GetRandomName() {
         cur_index++;
         if (cur_index >= shuffled_names.Count) {
            shuffled_names.Shuffle();
            cur_index = 0;
         }
         return shuffled_names[cur_index];
      }

      public IEnumerable<string> AllRandomNames() {
         int n = shuffled_names.Count;
         for (int i = 0; i < n; ++i) {
            yield return GetRandomName();
         }
      }
      
   }


   public static List<string> human_names => NameLists.commander_names;
   public static List<string> location_names => NameLists.province_names;


   static string SomethingOfSomething() {

      List<string> part1 = new List<string> {
         "Age",
         "Realm",
         "World",
         "Era",
         "Domain",
         "A Tale",
         "A Story",
         "Legends",
         "A Legend",
      };

      List<string> part2 = new List<string> {
         "Magic",
         "War",
         "Battle",
         "Chaos",
         "Kingdoms",
         "Empires",
         "Wizards",
         "Monsters",
         "Steel",
         "Plunder",
         "Glory",
         "Fame",
         "Rulers",
         "Might",
         "Heroes",
         "Emperors",
         "Kings",
         "Rivalry",
         "Masters",
         "Cults",
         "Gods",
      };
      string n1 = part1.Random();
      return n1 + " of " + part2.Random();
   }
   static string PersonsOfTraits() {

      List<string> part1 = new List<string> {
         "Lords",
         "Wizards",
         "Kings",
         "Emperors",
         "Heroes",
         "Soldiers",
         "Monsters",
         "Rulers",
         "Leaders",
         "Enemies",
         "Allies",
         "Masters",
      };

      List<string> part2 = new List<string> {
         "Magic",
         "War",
         "Battle",
         "Chaos",
         "Steel",
         "Plunder",
         "Glory",
         "Fame",
         "Might",
         "Legends",
      };
      string n1 = part1.Random();
      part2.AddRange(part2.Where(x => x[0] == n1[0]));
      return n1 + " of " + part2.Random();
   }
   static string AdjectiveVerb() {

      List<string> part1 = new List<string> {
         "Chaotic",
         "Ferocious",
         "Calm",
         "Determined",
         "Tactical",
         "Wise",
         "Heroic",
         "Noble",
         "Monstrous",
         "Forbidden",
      };

      List<string> part2 = new List<string> {
         "Warfare",
         "Conquest",
         "Diplomacy",
         "Expansions",
         "Exploration",
         "Exploitation",
         "Wizardry",
         "Sorcery",
         "Chivalry",
         "Mastery",
      };
      string n1 = part1.Random();
      part2.AddRange(part2.Where(x => x[0] == n1[0]));
      return n1 + " " + part2.Random();
   }
   static string ThingAndThing() {

      List<string> part1 = new List<string> {
         "Dungeons",
         "Heroes",
         "Empires",
         "Wizards",
         "Sorcerers",
         "Mages",
         "Monsters",
         "Cities",
         "Towns",
         "Armies",
         "Soldiers",
         "Rulers",
         "Nobles",
         "Artifacts",
         "Spells",
         "Kingdoms",
         "Masters",
      };

      string n1 = part1.Random();
      part1.Remove(n1);

      part1.AddRange(part1.Where(x => x[0] == n1[0]));

      return n1 + " and " + part1.Random();
   }
   static string VerbAndVerb() {

      List<string> part1 = new List<string> {
         "Explore",
         "Plunder",
         "Pillage",
         "Adventure",

         "Expand",
         "Command",
         "Build",
         "Rule",

         "Exploit",
         "Research",
         "Discover",
         "Extract",

         "Conquer",
         "Dominate",
         "Compete",
      };

      List<string> extra_part_2 = new List<string> {
         "Win",
         "Succeed",
         "Prosper",
      };

      string n1 = part1.Random();
      part1.Remove(n1);
      part1.AddRange(extra_part_2);

      part1.AddRange(part1.Where(x => x[0] == n1[0]));
      return n1 + " and " + part1.Random();
   }

   public static string RandomGameName() {
      List<System.Func<string>> gens = new List<System.Func<string>> {
         SomethingOfSomething,
         PersonsOfTraits,
         AdjectiveVerb,
         ThingAndThing,
         VerbAndVerb,
      };

      return gens.Random().Invoke();
   }
}