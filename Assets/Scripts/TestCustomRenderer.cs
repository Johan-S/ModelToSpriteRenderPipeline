using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TestCustomRenderer : MonoBehaviour {

   public Mesh mesh;
   
   private void OnRenderObject() {
      // Ensure we are in the correct rendering context
      if (Camera.current == Camera.main || Camera.current.name == "SceneCamera") {
         // Set up the material and shader
         Material material = new Material(Shader.Find("Unlit/Color"));
         material.color = Color.blue;

         // Apply the material
         material.SetPass(0);
         
         Debug.Log($"on render {Camera.current.name}");

         // Draw a simple shape (e.g., a quad)
         GL.PushMatrix();
         GL.MultMatrix(transform.localToWorldMatrix);
         GL.Begin(GL.QUADS);
         GL.Color(Color.white);
         GL.Vertex3(-0.5f, -0.5f, 0);
         GL.Vertex3(0.5f, -0.5f, 0);
         GL.Vertex3(0.5f, 0.5f, 0);
         GL.Vertex3(-0.5f, 0.5f, 0);
         GL.End();
         GL.PopMatrix();
         
         
         Graphics.DrawMeshNow(mesh, transform.position + new Vector3(2, 0), transform.rotation);
      }
   }
}