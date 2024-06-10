using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;


// [CreateAssetMenu(fileName = "ModelPartsBundle", menuName = "ModelPartsBundle", order = 0)]
public class ModelPartsBundle : ScriptableObject {

   public Mesh[] body_parts;

   public Transform[] slotted_parts;
   

}
