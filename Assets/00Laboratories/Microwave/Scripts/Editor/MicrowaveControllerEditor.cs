#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace OOLaboratories.Microwave
{
    /// <summary>
    /// Custom editor for the microwave demo.
    /// </summary>
    /// <seealso cref="UnityEditor.Editor"/>
    [CustomEditor(typeof(MicrowaveController))]
    public class WashingMachineControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            // add buttons to control the washing machine during play:
            MicrowaveController controller = (MicrowaveController)serializedObject.targetObject;

            EditorGUILayout.LabelField("Microwave Control" + (Application.isPlaying ? "" : " (Playmode Only)"), EditorStyles.boldLabel);

            GUI.enabled = Application.isPlaying;

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Start"))
                {
                    controller.StartMicrowave();
                }
                if (GUILayout.Button("Stop"))
                {
                    controller.StopMicrowave();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Note: Try opening the door with rotation Y.");
        }
    }
}

#endif