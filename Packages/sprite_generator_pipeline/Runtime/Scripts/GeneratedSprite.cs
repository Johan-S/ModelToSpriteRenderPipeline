public class GeneratedSprite : Named {
   public string name {
      get => _name;
   }

   string _name;

   public GeneratedSprite(string name, ParsedUnit pu, AnimationWrap an, SpriteRenderDetails shot_type) {
      _name = name;
      this.pu = pu;
      this.an = an;
      this.shot_type = shot_type;
   }

   public ParsedUnit pu;
   public AnimationWrap an;
   public SpriteRenderDetails shot_type;
}