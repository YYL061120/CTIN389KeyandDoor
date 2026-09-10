using UnityEngine;

namespace OOLaboratories.Microwave
{
    public static class GLUtilities
    {
        /// <summary>Calls the specified action with the textured GUI shader between GL begin and GL end.</summary>
        /// <param name="action">The action be called to draw primitives.</param>
        public static void DrawGuiTextured(Texture texture, System.Action action)
        {
            var guiMaterial = MicrowaveResources.temporaryGuiMaterial;
            guiMaterial.mainTexture = texture;
            guiMaterial.SetPass(0);

            GL.Begin(GL.QUADS);
            action();
            GL.End();
        }

        /// <summary>Draws a rectangle with the current texture (see <see cref="DrawGuiTextured"/>).</summary>
        public static void DrawFlippedUvRectangle(float x, float y, float w, float h, Color color)
        {
            GL.Color(color);
            w += x;
            h += y;
            GL.TexCoord2(0, 1);
            GL.Vertex3(x, y, 0);
            GL.TexCoord2(0, 0);
            GL.Vertex3(x, h, 0);
            GL.TexCoord2(1, 0);
            GL.Vertex3(w, h, 0);
            GL.TexCoord2(1, 1);
            GL.Vertex3(w, y, 0);
        }
    }
}