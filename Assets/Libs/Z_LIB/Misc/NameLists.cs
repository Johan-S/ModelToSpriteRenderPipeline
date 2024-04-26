using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NameLists {

   public static List<string> province_names = ProvinceNames().ToList();
   public static List<string> commander_names = CommanderNames().ToList();

   public static string ProvinceName() {
      return province_names[Random.Range(0, province_names.Count)];
   }
   public static string CommanderName() {
      return commander_names[Random.Range(0, commander_names.Count)];
   }
   static IEnumerable<string> CommanderNames() {
      foreach (var row in commander_names_raw.Split('\n')) {
         var s = row.Trim();
         if (s.Length > 0) yield return s;
      }
   }

   static IEnumerable<string> ProvinceNames() {
      foreach (var row in province_names_raw.Split('\n')) {
         var s = row.Trim();
         if (s.Length > 0) yield return s;
      }
   }
   static string commander_names_raw => @"Michael
Christopher
Jessica
Matthew
Ashley
Jennifer
Joshua
Amanda
Daniel
David
James
Robert
John
Joseph
Andrew
Ryan
Brandon
Jason
Justin
Sarah
William
Jonathan
Stephanie
Brian
Nicole
Nicholas
Anthony
Heather
Eric
Elizabeth
Adam
Megan
Melissa
Kevin
Steven
Thomas
Timothy
Christina
Kyle
Rachel
Laura
Lauren
Amber
Brittany
Danielle
Richard
Kimberly
Jeffrey
Amy
Crystal
Michelle
Tiffany
Jeremy
Benjamin
Mark
Emily
Aaron
Charles
Rebecca
Jacob
Stephen
Patrick
Sean
Erin
Zachary
";


   static string province_names_raw => @"Aberdeen
Abilene
Akron
Albany
Albuquerque
Alexandria
Allentown
Amarillo
Anaheim
Anchorage
Ann Arbor
Antioch
Apple Valley
Appleton
Arlington
Arvada
Asheville
Athens
Atlanta
Atlantic City
Augusta
Aurora
Austin
Bakersfield
Baltimore
Barnstable
Baton Rouge
Beaumont
Bel Air
Bellevue
Berkeley
Bethlehem
Billings
Birmingham
Bloomington
Boise
Boise City
Bonita Springs
Boston
Boulder
Bradenton
Bremerton
Bridgeport
Brighton
Brownsville
Bryan
Buffalo
Burbank
Burlington
Cambridge
Canton
Cape Coral
Carrollton
Cary
Cathedral City
Cedar Rapids
Champaign
Chandler
Charleston
Charlotte
Chattanooga
Chesapeake
Chicago
Chula Vista
Cincinnati
Clarke County
Clarksville
Clearwater
Cleveland
College Station
Colorado Springs
Columbia
Columbus";
}