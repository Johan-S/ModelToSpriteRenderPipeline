public interface IUnitTypeForRender {



   public string ExportName {
      get;
   }

   public string AnimationType {
      get;
   }

   public string ModelBodyName {
      get => ModelBody.bodyTypeName;
   }
   public ModelBodyCategory ModelBody {
      get;
   }

   
}