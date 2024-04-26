using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisplayHandle : MonoBehaviour {


   List<Sprite> sprites = new();


   public Image diplay;
   public Image diplay_small;

   public void DisplayTex(Texture2D t) {
      
      var sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), t.height / 2, 0,
         SpriteMeshType.FullRect);
      
      sprites.Add(sprite);

      diplay.sprite = sprite;
      diplay_small.sprite = sprite;

   }
   
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
