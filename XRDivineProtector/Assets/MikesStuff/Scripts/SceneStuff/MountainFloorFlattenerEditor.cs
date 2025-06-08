/*#if UNITY_EDITOR
[CustomEditor(typeof(MountainFloorFlattener))]
public class MountainFloorFlattenerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Draws the default variables

        MountainFloorFlattener flattener = (MountainFloorFlattener)target;
        if (GUILayout.Button("Flatten Mountain Meshes Now"))
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Error", "Flattening can only be done in Play Mode when MRUK data is available.", "OK");
                return;
            }
            if (!MRUK.Instance || !MRUK.Instance.IsSceneLoaded)
            {
                EditorUtility.DisplayDialog("Error", "MRUK Scene is not loaded. Please ensure Room Setup is complete and MRUK has loaded the scene in Play Mode.", "OK");
                return;
            }
            flattener.FlattenAllMountainMeshes();
        }
    }
}
#endif
*/