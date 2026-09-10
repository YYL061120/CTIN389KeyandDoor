#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace OOLaboratories.Microwave
{
    /// <summary>
    /// Custom editor for the <see cref="MicrowaveCable"/>.
    /// </summary>
    /// <seealso cref="UnityEditor.Editor"/>
    [CustomEditor(typeof(MicrowaveCable))]
    public class MicrowaveCableEditor : Editor
    {
        private SerializedProperty radius;
        private SerializedProperty segments;
        private SerializedProperty sides;
        private SerializedProperty connectorMesh;
        private SerializedProperty connectorAngle;
        private SerializedProperty splinePoints;

        /// <summary>
        /// The currently selected index or -1 if nothing is selected.
        /// </summary>
        private int selectedIndex = -1;

        private void OnEnable()
        {
            // hide the default tools from the scene view.
            Tools.hidden = true;

            // find the serialized properties.
            splinePoints = serializedObject.FindProperty("splinePoints");
            radius = serializedObject.FindProperty("radius");
            segments = serializedObject.FindProperty("segments");
            sides = serializedObject.FindProperty("sides");
            connectorMesh = serializedObject.FindProperty("connectorMesh");
            connectorAngle = serializedObject.FindProperty("connectorAngle");
        }

        private void OnDisable()
        {
            // show the default tools in the scene view.
            Tools.hidden = false;
        }

        public override void OnInspectorGUI()
        {
            MicrowaveCable cable = (MicrowaveCable)target;

            EditorGUILayout.PropertyField(radius);
            EditorGUILayout.PropertyField(segments);
            EditorGUILayout.PropertyField(sides);
            EditorGUILayout.PropertyField(connectorMesh);
            EditorGUILayout.PropertyField(connectorAngle);

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button((selectedIndex == 0 || selectedIndex == splinePoints.arraySize - 1) ? "Add Segment" : "Insert Segment"))
                {
                    if (selectedIndex == -1 || selectedIndex == splinePoints.arraySize - 1)
                    {
                        Vector3 direction = (splinePoints.GetArrayElementAtIndex(splinePoints.arraySize - 1).vector3Value - splinePoints.GetArrayElementAtIndex(splinePoints.arraySize - 2).vector3Value).normalized;

                        splinePoints.InsertArrayElementAtIndex(splinePoints.arraySize - 1);
                        splinePoints.GetArrayElementAtIndex(splinePoints.arraySize - 1).vector3Value += (direction * 0.0625f);
                        splinePoints.InsertArrayElementAtIndex(splinePoints.arraySize - 1);
                        splinePoints.GetArrayElementAtIndex(splinePoints.arraySize - 1).vector3Value += (direction * 0.0625f);

                        // update selection.
                        selectedIndex = splinePoints.arraySize - 1;
                    }
                    else if (selectedIndex == 0)
                    {
                        Vector3 direction = (splinePoints.GetArrayElementAtIndex(0).vector3Value - splinePoints.GetArrayElementAtIndex(1).vector3Value).normalized;

                        splinePoints.InsertArrayElementAtIndex(0);
                        splinePoints.GetArrayElementAtIndex(0).vector3Value += (direction * 0.0625f);
                        splinePoints.InsertArrayElementAtIndex(0);
                        splinePoints.GetArrayElementAtIndex(0).vector3Value += (direction * 0.0625f);
                    }
                    else
                    {
                        Vector3 direction = (splinePoints.GetArrayElementAtIndex(selectedIndex - 1).vector3Value - splinePoints.GetArrayElementAtIndex(selectedIndex + 1).vector3Value).normalized;
                        float length = (splinePoints.GetArrayElementAtIndex(selectedIndex - 1).vector3Value - splinePoints.GetArrayElementAtIndex(selectedIndex).vector3Value).magnitude;

                        splinePoints.InsertArrayElementAtIndex(selectedIndex);
                        splinePoints.GetArrayElementAtIndex(selectedIndex).vector3Value += (direction * (length * 0.25f));
                        splinePoints.InsertArrayElementAtIndex(selectedIndex);
                        splinePoints.GetArrayElementAtIndex(selectedIndex).vector3Value += (direction * (length * 0.25f));
                    }
                }
                GUI.enabled = (selectedIndex != -1 && splinePoints.arraySize > 3);
                if (GUILayout.Button("Remove Segment"))
                {
                    if (selectedIndex == 0)
                    {
                        splinePoints.DeleteArrayElementAtIndex(selectedIndex);
                        splinePoints.DeleteArrayElementAtIndex(selectedIndex);
                    }
                    // bezier pivot point.
                    else if (selectedIndex % 2 == 1)
                    {
                        splinePoints.DeleteArrayElementAtIndex(selectedIndex);
                        splinePoints.DeleteArrayElementAtIndex(selectedIndex);
                    }
                    else
                    {
                        splinePoints.DeleteArrayElementAtIndex(selectedIndex);
                        splinePoints.DeleteArrayElementAtIndex(selectedIndex - 1);
                    }
                    // make sure something is selected.
                    if (selectedIndex > splinePoints.arraySize - 1)
                        selectedIndex = splinePoints.arraySize - 1;
                }
                GUI.enabled = true;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(splinePoints, true);

            // detect modified properties:
            if (serializedObject.ApplyModifiedProperties())
                cable.UpdateMesh();
        }

        private void OnSceneGUI()
        {
            MicrowaveCable cable = (MicrowaveCable)target;
            Transform transform = cable.transform;

            // must have at least 3 points.
            // this can happen when the user manually deletes spline points.
            if (splinePoints.arraySize < 3)
            {
                // some example points:
                splinePoints.ClearArray();
                splinePoints.InsertArrayElementAtIndex(0);
                splinePoints.InsertArrayElementAtIndex(0);
                splinePoints.InsertArrayElementAtIndex(0);
                splinePoints.GetArrayElementAtIndex(0).vector3Value = new Vector3(0.1f, -0.1f, 0);
                splinePoints.GetArrayElementAtIndex(1).vector3Value = new Vector3(0.25f, 0, -0.1f);
                splinePoints.GetArrayElementAtIndex(2).vector3Value = new Vector3(0.5f, 0, 0);

                // generate the cable mesh:
                cable.UpdateMesh();
            }

            for (int i = 0; i < splinePoints.arraySize; i++)
            {
                // get orientation point for the circle handles.
                Vector3 orientationPoint = Vector3.zero;
                if (i + 1 != splinePoints.arraySize)
                    orientationPoint = splinePoints.GetArrayElementAtIndex(i + 1).vector3Value;
                else
                    orientationPoint = splinePoints.GetArrayElementAtIndex(i - 1).vector3Value;

                // get current point.
                SerializedProperty point = splinePoints.GetArrayElementAtIndex(i);

                // currently selected point.
                if (i == selectedIndex)
                {
                    Vector3 newPosition = transform.InverseTransformPoint(Handles.DoPositionHandle(transform.TransformPoint(point.vector3Value), Quaternion.identity));
                    if (Vector3.Distance(point.vector3Value, newPosition) > 0.0001f)
                        point.vector3Value = newPosition;
                }
                else
                {
                    // bezier pivot point.
                    if (i % 2 == 1)
                    {
                        Handles.color = Color.red;
                        if (Handles.Button(transform.TransformPoint(point.vector3Value), Quaternion.identity, 0.01f, 0.01f, Handles.DotHandleCap))
                        {
                            selectedIndex = i;
                            Repaint();
                        }
                    }
                    // regular point.
                    else
                    {
                        Quaternion quaternion = Quaternion.LookRotation(orientationPoint - point.vector3Value);

                        Handles.color = Color.white;
                        if (Handles.Button(transform.TransformPoint(point.vector3Value), quaternion, radius.floatValue, radius.floatValue, Handles.CircleHandleCap))
                        {
                            selectedIndex = i;
                            Repaint();
                        }
                    }
                }

                // bezier helper line.
                if (i % 2 == 1)
                {
                    Handles.color = Color.red;
                    Handles.DrawLine(transform.TransformPoint(splinePoints.GetArrayElementAtIndex(i - 1).vector3Value), transform.TransformPoint(point.vector3Value));
                }
            }

            // detect modified properties:
            if (serializedObject.ApplyModifiedProperties())
                cable.UpdateMesh();

            // detect undo and redo:
            if (Event.current.type == EventType.ValidateCommand && Event.current.commandName == "UndoRedoPerformed")
                cable.UpdateMesh();
        }
    }
}

#endif