using System.Collections.Generic;
using UnityEngine;

namespace OOLaboratories.Microwave
{
    /// <summary>
    /// A realistic dynamically generated cable mesh that follows a spline.
    /// </summary>
    /// <seealso cref="UnityEngine.MonoBehaviour"/>
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    [ExecuteInEditMode]
    public class MicrowaveCable : MonoBehaviour
    {
        /// <summary>
        /// The generated cable mesh radius.
        /// </summary>
        [Range(0.001f, 0.04f)]
        public float radius = 0.02f;

        /// <summary>
        /// The generated cable mesh segments (more curvature detail in the spline).
        /// </summary>
        [Range(1, 1024)]
        public int segments = 32;

        /// <summary>
        /// The generated cable mesh sides (cylinder sides).
        /// </summary>
        [Range(3, 128)]
        public int sides = 20;

        /// <summary>
        /// The cable end connector mesh that is concatenated onto the generated cable mesh.
        /// </summary>
        public Mesh connectorMesh;

        /// <summary>
        /// The cable end connector mesh angle, useful for the power socket connector.
        /// </summary>
        [Range(-180, 180)]
        public float connectorAngle;

        /// <summary>
        /// The spline points that the cable mesh follows.
        /// </summary>
        public List<Vector3> splinePoints;

        private void OnEnable()
        {
            // this is likely called in the editor when a cable is created by the user.
            if (splinePoints == null)
            {
                // some example points:
                splinePoints = new List<Vector3>();
                splinePoints.Add(new Vector3(0.1f, -0.1f, 0));
                splinePoints.Add(new Vector3(0.25f, 0, -0.1f));
                splinePoints.Add(new Vector3(0.5f, 0, 0));

                // generate the cable mesh:
                UpdateMesh();
            }

            // if there is no mesh yet we generate it now.
            if (!GetComponent<MeshFilter>().sharedMesh)
            {
                // generate the cable mesh.
                UpdateMesh();
            }
        }

        /// <summary>
        /// Called in the editor when the mesh has to be updated.
        /// </summary>
        public void UpdateMesh()
        {
            // must have at least 3 points.
            if (splinePoints.Count < 3) return;

            // generate the cable mesh:
            Matrix4x4 endMatrix;
            Mesh cableMesh = CableMeshHelper.CreateMesh(transform, new CableMeshHelper.BezierSpline3(splinePoints.ToArray()), out endMatrix, sides, radius, segments);
            Mesh finalMesh = cableMesh;

            // concatenate the connector mesh if specified:
            if (connectorMesh != null)
            {
                // create a new final mesh:
                finalMesh = new Mesh();
                finalMesh.name = "Microwave Cable";
                finalMesh.subMeshCount = 2;

                finalMesh.CombineMeshes(new CombineInstance[]
                {
                    new CombineInstance { mesh = cableMesh, transform = Matrix4x4.identity },
                    new CombineInstance { mesh = connectorMesh, transform = endMatrix * Matrix4x4.Rotate(Quaternion.Euler(0.0f, 0.0f, connectorAngle)) },
                }, false, true);
            }

            // assign the new mesh to this cable.
            GetComponent<MeshFilter>().mesh = finalMesh;
        }
    }
}