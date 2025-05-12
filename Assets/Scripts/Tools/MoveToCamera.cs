#if UNITY_EDITOR
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
    }

    [MenuItem("Tools/Move Player To Cinematic Position")]
    public static void MovePlayerToCinematic()
    {
        GameObject selectedObject = Selection.activeGameObject;

        if (selectedObject == null)
        {
            Debug.LogWarning("No GameObject selected.");
            return;
        }

        GameObject cinematicTarget = GameObject.Find("TimeLine"); // <- Replace with your actual object name

        if (cinematicTarget == null)
        {
            Debug.LogWarning("Cinematic target not found.");
            return;
        }

        Undo.RecordObject(selectedObject.transform, "Move To Cinematic Target");
        selectedObject.transform.position = cinematicTarget.transform.position;
        selectedObject.transform.rotation = cinematicTarget.transform.rotation;
        // Do NOT copy scale
    }
}
#endif