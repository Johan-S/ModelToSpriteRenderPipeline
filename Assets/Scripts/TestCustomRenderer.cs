using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TestCustomRenderer : MonoBehaviour {
   public Mesh mesh;

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

      // Not sure this check is needed?
      if (
         // Game view.
         Camera.current == Camera.main
         // Scene view.
         || Camera.current.name == "SceneCamera"
         // Renders the camera preview when you select a camera. Note the space.
         || Camera.current.name == "Preview Camera") {
         // Apply the material
         lineMaterial.SetPass(0);


         // Draw a simple shape (e.g., a quad)
         GL.PushMatrix();
         GL.MultMatrix(transform.localToWorldMatrix);
         GL.Begin(GL.QUADS);
         GL.Color(Color.white);
         GL.Vertex3(-0.5f, -0.5f, 0);
         GL.Color(Color.blue);
         GL.Vertex3(-0.5f, 0.5f, 0);
         GL.Color(Color.red);
         GL.Vertex3(0.5f, 0.5f, 0);
         GL.Color(Color.green);
         GL.Vertex3(0.5f, -0.5f, 0);
         GL.End();
         GL.PopMatrix();


         Graphics.DrawMeshNow(mesh, transform.position + new Vector3(2, 0), transform.rotation);
      }
   }
}