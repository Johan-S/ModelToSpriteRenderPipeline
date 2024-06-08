using UnityEngine;



[CreateAssetMenu(fileName="New UnitType", menuName = "Pipeline Unit Type", order = 0)]
public class UnitTypeForRender : ScriptableObject, IUnitTypeForRender {
   
   public string export_name;
   public ModelBodyCategory body;

   public string animation_type;

   public Material material;

   public string ExportName => export_name;
   public string AnimationType => animation_type;
   public Material MaterialOverride { get => material; }
   public ModelBodyCategory ModelBody => body;
}