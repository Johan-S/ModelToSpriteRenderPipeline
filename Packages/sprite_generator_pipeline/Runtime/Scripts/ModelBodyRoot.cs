using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[SelectionBase]
public class ModelBodyRoot : MonoBehaviour {


   [Header("Slots")]
   
   public Transform Mouth;
   public Transform Main_Hand;
   public Transform Off_Hand_Shield;
   public Transform Off_Hand;
   public Transform NewHelmet;


   public Transform[] mounting_points;
   

   [Header("Renderers")]
   public Renderer[] renderers;

}