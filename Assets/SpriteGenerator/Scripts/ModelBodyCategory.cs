using UnityEngine;

[CreateAssetMenu(fileName = "New ModelBody", menuName = "Pipeline Model Body", order = 0)]
public class ModelBodyCategory : ScriptableObject {

   public Animator model_root_prefab;


   public Material material;

   public string bodyTypeName;

   public string animationTypeName;

   public bool no_gear;

   public Vector3 model_offset;

}