public class GeneratedSprite : Named {
   public string name {
      get => _name;
   }

   string _name;

   public GeneratedSprite(string name, int frame, ParsedUnit pu, AnimationWrap an, SpriteRenderDetails shot_type) {
      _name = name;
      this.pu = pu;
      this.frame = frame;
      this.an = an;
      this.shot_type = shot_type;
   }

   public int frame;

   public ParsedUnit pu;
   public AnimationWrap an;
   public SpriteRenderDetails shot_type;
}