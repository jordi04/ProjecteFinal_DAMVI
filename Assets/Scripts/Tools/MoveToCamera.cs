using UnityEditor;
using UnityEngine;

public static class MoveSelectedToSceneCameraShortcut
{
    [MenuItem("Tools/Move Selected Object To Scene Camera %#m")]
    public static void MoveSelectedToSceneCamera()
    {
        GameObject selectedObject = Selection.activeGameObject;

        if (selectedObject == null)
        {
            Debug.LogWarning("No GameObject selected.");
            return;
        }

        if (SceneView.lastActiveSceneView == null)
        {
            Debug.LogWarning("No active Scene view.");
            return;
        }

        SceneView sceneView = SceneView.lastActiveSceneView;
        Camera sceneCam = sceneView.camera;

        Undo.RecordObject(selectedObject.transform, "Move To Scene Camera");
        selectedObject.transform.position = sceneCam.transform.position;
        selectedObject.transform.rotation = sceneCam.transform.rotation;
    }
}
