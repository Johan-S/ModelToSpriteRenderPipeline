using UnityEngine;

[CreateAssetMenu(fileName = "ModelBody_RENAME", menuName = "SpriteGEn Model Body", order = 0)]
public class ModelBodyCategory : ScriptableObject {

   public Animator model_root_prefab;


   public Material material;

   public string bodyTypeName;

   public string animationTypeName;

   public bool no_gear;

   public Vector3 model_offset;

}