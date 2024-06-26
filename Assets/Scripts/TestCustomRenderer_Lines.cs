using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TestCustomRenderer_Lines : MonoBehaviour {
   public int lineCount = 100;
   public float radius = 3.0f;

   static Material lineMaterial;

   static void CreateLineMaterial() {
      if (!lineMaterial) {
         // Unity has a built-in shader that is useful for drawing
         // simple colored things.
         Shader shader = Shader.Find("Hidden/Internal-Colored");
         lineMaterial = new Material(shader);
         lineMaterial.hideFlags = HideFlags.HideAndDontSave;
         // Turn on alpha blending
         lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
         lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
         // Turn backface culling off
         lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
         // Turn off depth writes
         lineMaterial.SetInt("_ZWrite", 0);
      }
   }


   private void OnRenderObject() {
      // Debug.Log($"on render {Camera.current.name}");
      CreateLineMaterial();


      // Set up the material and shader

      // Apply the material
      lineMaterial.SetPass(0);


      // Draw a simple shape (e.g., a quad)
      GL.PushMatrix();
      GL.MultMatrix(transform.localToWorldMatrix);


      // Draw lines
      GL.Begin(GL.LINES);
      for (int i = 0; i < lineCount; ++i) {
         float a = i / (float)lineCount;
         float angle = a * Mathf.PI * 2;
         // Vertex colors change from red to green
         GL.Color(new Color(0, 1 - a, a, 0.8F));
         // One vertex at transform position
         GL.Vertex3(0, 0, 0);
         // Another vertex at edge of circle
         GL.Vertex3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
      }

      GL.End();

      GL.PopMatrix();
   }
}