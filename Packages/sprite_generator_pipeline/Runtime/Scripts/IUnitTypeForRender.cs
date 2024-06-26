using UnityEngine;

public interface IUnitTypeForRender {



   public string ExportName {
      get;
   }

   public string AnimationType {
      get;
   }

   Material MaterialOverride {
      get;
   }

   public string ModelBodyName {
      get => ModelBody.bodyTypeName;
   }
   public ModelBodyCategory ModelBody {
      get;
   }

   public SpriteRenderDetails[] ShotTypes { get; }
   
}