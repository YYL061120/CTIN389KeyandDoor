using System.Collections.Generic;
using UnityEngine;

namespace OOLaboratories.Microwave
{
    /// <summary>
    /// Helper class to generate realistic cable meshes.
    /// </summary>
    internal class CableMeshHelper
    {
        /// <summary>
        /// Describes an object that can be copied into a unique instance.
        /// </summary>
        /// <typeparam name="T">The type of the object to be copied.</typeparam>
        private interface IDeepCopyable<T>
        {
            /// <summary>
            /// Creates a deep copy of the <typeparamref name="T"/>.
            /// </summary>
            /// <returns>A deep copy of the <typeparamref name="T"/></returns>
            T DeepCopy();
        }

        /// <summary>
        /// A vertex (plural vertices) describes the position of a point in 3D space. With additional
        /// attributes for lighting, texturing and coloring.
        /// </summary>
        private class Vertex : IDeepCopyable<Vertex>
        {
            /// <summary>The position of the <see cref="Vertex"/>.</summary>
            public Vector3 Position;

            /// <summary>
            /// The normal direction of the <see cref="Vertex"/>. Used in lighting calculations to
            /// determine the direction a <see cref="Polygon"/> is facing.
            /// </summary>
            public Vector3 Normal;

            /// <summary>
            /// The uv coordinates of the <see cref="Vertex"/>. Determines how a texture is mapped onto a
            /// <see cref="Polygon"/>.
            /// </summary>
            public Vector2 TextureCoordinates;

            /// <summary>
            /// Initializes a new instance of the <see cref="Vertex"/> class.
            /// </summary>
            /// <param name="position">The position.</param>
            /// <param name="normal">The normal.</param>
            /// <param name="textureCoordinates">The texture coordinates.</param>
            public Vertex(Vector3 position, Vector3 normal, Vector2 textureCoordinates)
            {
                Position = position;
                Normal = normal;
                TextureCoordinates = textureCoordinates;
            }

            /// <summary>
            /// Creates a deep copy of the <typeparamref name="T"/>.
            /// </summary>
            /// <returns>A deep copy of the <typeparamref name="T"/></returns>
            public Vertex DeepCopy()
            {
                return new Vertex(Position, Normal, TextureCoordinates);
            }

            /// <summary>
            /// Multiplies the vertex with the specified matrix.
            /// </summary>
            /// <param name="matrix">The matrix to be multiplied.</param>
            /// <returns>The 'this' reference for chaining.</returns>
            public Vertex MultiplyMatrix(Matrix4x4 matrix)
            {
                Position = matrix.MultiplyVector(Position);
                Normal = matrix.MultiplyVector(Normal);
                return this;
            }
        }

        /// <summary>
        /// A plane shape (two-dimensional) with 3 or more straight sides (see <see cref="Edge"/>) e.g.
        /// triangles, rectangles and pentagons.
        /// </summary>
        private class Polygon : IDeepCopyable<Polygon>
        {
            /// <summary>
            /// The vertices (see <see cref="Vertex"/>) that make up this polygonal shape.
            /// </summary>
            public List<Vertex> Vertices = new List<Vertex>(3);

            /// <summary>
            /// Used externally to speed up calculations by not calculating a plane for every operation
            /// (see <see cref="CalculatePlane"/>).
            /// <para>
            /// A plane that approximately resembles the polygon. Most useful for calculations involving
            /// the normal of the polygon.
            /// </para>
            /// </summary>
            private Plane? cachedPlane = null;

            /// <summary>
            /// Gets a plane that approximately resembles the polygon. Most useful for calculations
            /// involving the normal of the polygon.
            /// <para>
            /// This plane is cached for performance reasons, you may have to call <see
            /// cref="CalculatePlane"/> if you modified the polygon.
            /// </para>
            /// </summary>
            /// <value>A plane that approximately resembles the polygon.</value>
            public Plane Plane
            {
                get
                {
                    // if we never calculated a plane for this polygon before we do so now:
                    if (!cachedPlane.HasValue)
                        CalculatePlane();

                    // return the cached plane instead of calculating one every time.
                    return cachedPlane.Value;
                }
            }

            /// <summary>
            /// Calculates a plane that approximately resembles the polygon (see <see cref="Plane"/>).
            /// </summary>
            public void CalculatePlane()
            {
                cachedPlane = new Plane(Vertices[0].Position, Vertices[1].Position, Vertices[2].Position);

                // hack: if the plane's normal is zero and there's more than 3 vertices,
                // try using alternative vertices to construct the plane.
                if (cachedPlane.Value.normal == Vector3.zero && Vertices.Count > 3)
                {
                    // we use the first two vertices.
                    Vector3 pos1 = Vertices[0].Position;
                    Vector3 pos2 = Vertices[1].Position;

                    // iterate through the available vertices.
                    for (int i = 3; i < Vertices.Count; i++)
                    {
                        // use this vertex to construct a new plane.
                        cachedPlane = new Plane(pos1, pos2, Vertices[i].Position);
                        // stop once we found a valid normal.
                        if (cachedPlane.Value.normal != Vector3.zero)
                            return;
                    }
                }
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Polygon"/> class.
            /// </summary>
            /// <param name="vertices">The vertices that make up this polygon.</param>
            public Polygon(IEnumerable<Vertex> vertices)
            {
                Vertices.AddRange(vertices);
            }

            /// <summary>
            /// Triangulates this polygon.
            /// </summary>
            /// <returns>The triangles of this polygon.</returns>
            public Polygon[] Triangulate()
            {
                int triangleCount = Vertices.Count - 2;
                Polygon[] triangles = new Polygon[triangleCount];

                for (int j = 0; j < triangleCount; j++)
                {
                    triangles[j] = new Polygon
                    (
                        new Vertex[]
                        {
                            Vertices[0],
                            Vertices[j+1],
                            Vertices[j+2]
                        }
                    );
                }

                return triangles;
            }

            /// <summary>
            /// Resets the vertex normals to the normal of the <see cref="Plane"/> so they all face outside.
            /// <para>You may have to call <see cref="CalculatePlane"/> first if you modified the polygon.</para>
            /// </summary>
            public void ResetVertexNormals()
            {
                // iterate through all vertices:
                for (int i = 0; i < Vertices.Count; i++)
                    // set the vertex normal to the plane's normal.
                    Vertices[i].Normal = Plane.normal;
            }

            /// <summary>
            /// Creates a deep copy of the <typeparamref name="T"/>.
            /// </summary>
            /// <returns>A deep copy of the <typeparamref name="T"/></returns>
            public Polygon DeepCopy()
            {
                Vertex[] vertices = new Vertex[Vertices.Count];
                for (int i = 0; i < vertices.Length; i++)
                    vertices[i] = Vertices[i].DeepCopy();
                return new Polygon(vertices);
            }
        }

        /// <summary>
        /// An internal mesh representation.
        /// </summary>
        private class MyMesh
        {
            public List<Polygon> Polygons = new List<Polygon>();

            /// <summary>
            /// Transforms the internal mesh representation into a Unity mesh.
            /// </summary>
            /// <returns>The Unity mesh.</returns>
            public Mesh ToMesh()
            {
                Mesh mesh = new Mesh();

                int i = 0;
                List<int> triangles = new List<int>();
                List<Vector2> uvs = new List<Vector2>();
                List<Vector3> vertices = new List<Vector3>();
                List<Vector3> normals = new List<Vector3>();
                foreach (Polygon ngon in Polygons)
                {
                    if (ngon.Vertices.Count < 3) continue;
                    foreach (Polygon face in ngon.Triangulate())
                    {
                        foreach (Vertex vertex in face.Vertices)
                        {
                            vertices.Add(vertex.Position);
                            uvs.Add(vertex.TextureCoordinates);
                            normals.Add(vertex.Normal);
                            triangles.Add(i);
                            i++;
                        }
                    }
                }

                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.SetVertices(vertices);
                mesh.SetTriangles(triangles, 0);
                mesh.SetUVs(0, uvs);
                mesh.SetNormals(normals);

                return mesh;
            }
        }

        /// <summary>
        /// Provides methods for calculating bezier segments.
        /// </summary>
        public static class Bezier
        {
            /// <summary>
            /// Gets the point on a 3-point curve.
            /// </summary>
            /// <param name="p0">The start point.</param>
            /// <param name="p1">The pivot point.</param>
            /// <param name="p2">The end point.</param>
            /// <param name="t">The interpolant along the curve.</param>
            /// <returns>The point on the curve.</returns>
            public static Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
            {
                t = Mathf.Clamp01(t);
                float oneMinusT = 1f - t;
                return
                    oneMinusT * oneMinusT * p0 +
                    2f * oneMinusT * t * p1 +
                    t * t * p2;
            }

            public static Vector3 GetFirstDerivative(Vector3 p0, Vector3 p1, Vector3 p2, float t)
            {
                return
                    2f * (1f - t) * (p1 - p0) +
                    2f * t * (p2 - p1);
            }
        }

        /// <summary>
        /// Represents a spline that uses 3 points unlike bezier splines with 4 points.
        /// </summary>
        public class BezierSpline3
        {
            /// <summary>
            /// The points of the spline.
            /// </summary>
            public Vector3[] points;

            /// <summary>
            /// Initializes a new instance of the <see cref="BezierSpline3"/> class.
            /// </summary>
            /// <param name="points">The points that make up the spline.</param>
            public BezierSpline3(Vector3[] points)
            {
                this.points = points;
            }

            /// <summary>
            /// Gets a point on the spline.
            /// </summary>
            /// <param name="t">The interpolant on the spline to get the position for.</param>
            /// <returns>The point on the spline.</returns>
            public Vector3 GetPoint(float t)
            {
                int i;
                if (t >= 1f)
                {
                    t = 1f;
                    i = points.Length - 3;
                }
                else
                {
                    t = Mathf.Clamp01(t) * CurveCount;
                    i = (int)t;
                    t -= i;
                    i *= 2;
                }
                return Bezier.GetPoint(points[i], points[i + 1], points[i + 2], t);
            }

            /// <summary>
            /// Gets the velocity on the spline.
            /// </summary>
            /// <param name="t">The interpolant on the spline to get the velocity for.</param>
            /// <returns>The velocity on the spline.</returns>
            public Vector3 GetVelocity(float t)
            {
                int i;
                if (t >= 1f)
                {
                    t = 1f;
                    i = points.Length - 3;
                }
                else
                {
                    t = Mathf.Clamp01(t) * CurveCount;
                    i = (int)t;
                    t -= i;
                    i *= 2;
                }
                return Bezier.GetFirstDerivative(points[i], points[i + 1], points[i + 2], t);
            }

            /// <summary>
            /// Gets the direction the spline is moving in.
            /// </summary>
            /// <param name="t">The interpolant on the spline to get the direction for.</param>
            /// <returns>The direction on the spline.</returns>
            public Vector3 GetDirection(float t)
            {
                return GetVelocity(t).normalized;
            }

            /// <summary>
            /// Gets the amount of curves in this spline.
            /// </summary>
            /// <value>
            /// The amount of curves.
            /// </value>
            public int CurveCount
            {
                get
                {
                    return (points.Length - 1) / 2;
                }
            }

            public Vector3 GetRight(float t)
            {
                var A = GetPoint(t - 0.001f);
                var B = GetPoint(t + 0.001f);
                var delta = (B - A);
                return new Vector3(-delta.z, 0, delta.x).normalized;
            }

            public Vector3 GetForward(float t)
            {
                var A = GetPoint(t - 0.001f);
                var B = GetPoint(t + 0.001f);
                return (B - A).normalized;
            }

            public Vector3 GetUp(float t)
            {
                var A = GetPoint(t - 0.001f);
                var B = GetPoint(t + 0.001f);
                var delta = (B - A).normalized;
                return Vector3.Cross(delta, GetRight(t));
            }
        }

        /// <summary>
        /// Generates a cylinder of height and radius 2, unlike a prism sides have smooth normals.
        /// </summary>
        /// <returns>Polygons to be supplied to a brush.</returns>
        /// <param name="sideCount">Side count for the cylinder.</param>
        private static Polygon[] GenerateCylinder(int sideCount, float radius, Vector3 point1, Matrix4x4 matrix1, Vector3 point2, Matrix4x4 matrix2)
        {
            Polygon[] polygons = new Polygon[sideCount];

            float angleDelta = Mathf.PI * 2 / sideCount;

            for (int i = 0; i < sideCount; i++)
            {
                polygons[i] = new Polygon(new Vertex[]
                {
                    new Vertex(
                        new Vector3(Mathf.Sin((i+1) * angleDelta) * radius, Mathf.Cos((i+1) * angleDelta) * radius, 0),
                        new Vector3(Mathf.Sin((i+1) * angleDelta), Mathf.Cos((i+1) * angleDelta), 0),
                        new Vector2((i+1) * (1f/sideCount),0)).MultiplyMatrix(matrix1),

                    new Vertex(
                        new Vector3(Mathf.Sin(i * angleDelta) * radius, Mathf.Cos(i * angleDelta) * radius, 0),
                        new Vector3(Mathf.Sin(i * angleDelta), Mathf.Cos(i * angleDelta), 0),
                        new Vector2(i * (1f/sideCount),0)).MultiplyMatrix(matrix1),

                    new Vertex(
                        new Vector3(Mathf.Sin(i * angleDelta) * radius, Mathf.Cos(i * angleDelta) * radius, 0),
                        new Vector3(Mathf.Sin(i * angleDelta), Mathf.Cos(i * angleDelta), 0),
                        new Vector2(i * (1f/sideCount),1)).MultiplyMatrix(matrix2),

                    new Vertex(
                        new Vector3(Mathf.Sin((i+1) * angleDelta) * radius, Mathf.Cos((i+1) * angleDelta) * radius, 0),
                        new Vector3(Mathf.Sin((i+1) * angleDelta), Mathf.Cos((i+1) * angleDelta), 0),
                        new Vector2((i+1) * (1f/sideCount),1)).MultiplyMatrix(matrix2),
                });

                polygons[i].Vertices[0].Position += point1;
                polygons[i].Vertices[1].Position += point1;
                polygons[i].Vertices[2].Position += point2;
                polygons[i].Vertices[3].Position += point2;
            }

            return polygons;
        }

        public static Mesh CreateMesh(Transform transform, BezierSpline3 spline, out Matrix4x4 endMatrix, int sides = 20, float radius = 0.01f, int segments = 32)
        {
            MyMesh myMesh = new MyMesh();
            endMatrix = Matrix4x4.identity;

            float uvTravelDistance = 0.0f;
            Polygon[] lastCylinder = null;

            for (int i = 1; i <= segments; i++)
            {
                Vector3 point1 = spline.GetPoint((i - 1) / (float)segments);
                Vector3 direction1 = spline.GetDirection((i - 1) / (float)segments);
                Vector3 point2 = spline.GetPoint(i / (float)segments);
                Vector3 direction2 = spline.GetDirection(i / (float)segments);

                Quaternion first = Quaternion.LookRotation(direction1, point1);
                Quaternion second = Quaternion.LookRotation(direction2, first * Vector3.up);

                Matrix4x4 matrix1 = Matrix4x4.Rotate(first);
                Matrix4x4 matrix2 = Matrix4x4.Rotate(second);

                endMatrix = Matrix4x4.Translate(point2);
                endMatrix *= matrix2;

                Polygon[] cylinder = GenerateCylinder(sides, radius, point1, matrix1, point2, matrix2);

                // make a copy of the cylinder used as the 'lastCylinder' in the next iteration.
                Polygon[] cylinderBackup = new Polygon[cylinder.Length];
                for (int cbi = 0; cbi < cylinder.Length; cbi++)
                    cylinderBackup[cbi] = cylinder[cbi].DeepCopy();

                float distance = Vector3.Distance(point1, point2) * 8.0f;

                for (int k = 0; k < cylinder.Length; k++)
                {
                    Polygon polygon = cylinder[k];
                    polygon.Vertices[0].TextureCoordinates.y = uvTravelDistance;
                    polygon.Vertices[1].TextureCoordinates.y = uvTravelDistance;
                    polygon.Vertices[2].TextureCoordinates.y = uvTravelDistance + distance;
                    polygon.Vertices[3].TextureCoordinates.y = uvTravelDistance + distance;

                    // hide t-junction artefacts and fix uvs.
                    if (lastCylinder != null)
                    {
                        Polygon lastPolygon = lastCylinder[k];
                        polygon.Vertices[0].Position = lastPolygon.Vertices[3].Position;
                        polygon.Vertices[1].Position = lastPolygon.Vertices[2].Position;

                        polygon.Vertices[0].Normal = lastPolygon.Vertices[3].Normal;
                        polygon.Vertices[1].Normal = lastPolygon.Vertices[2].Normal;
                    }
                }

                uvTravelDistance += distance;

                myMesh.Polygons.AddRange(cylinder);
                lastCylinder = cylinderBackup;
            }

            Mesh mesh = myMesh.ToMesh();
            mesh.name = "Microwave Cable";
            mesh.RecalculateTangents();

#if UNITY_EDITOR
            UnityEditor.MeshUtility.Optimize(mesh);
#endif

            return mesh;
        }
    }
}