using UnityEditor;
using UnityEngine;

namespace Game.RoomGeneration
{
    /// <summary>
    /// Editor class for the RoomGenerator class.
    /// </summary>
    [CustomEditor(typeof(RoomGenerator))]
    public class RoomGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            RoomGenerator script = (RoomGenerator)target;

            DrawDefaultInspector();

            if (GUILayout.Button("Generate")) 
            {
                if (!EditorApplication.isPlaying)
                {
                    Debug.LogWarning("Warning - Unable to generate rooms while the editor isn't in play mode!");
                    return;
                }

                script.RegenerateRooms();
            }
        }
    }
}
