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
            DrawDefaultInspector();

            RoomGenerator script = (RoomGenerator)target;

            if (GUILayout.Button("Generate") && EditorApplication.isPlaying)
                script.RegenerateRooms();
        }
    }
}
