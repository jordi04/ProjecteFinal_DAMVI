#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PopUpTrigger))]
public class PopUpTriggerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PopUpTrigger trigger = (PopUpTrigger)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Testing", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Test PopUp"))
        {
            trigger.TriggerPopUp();
        }

        if (GUILayout.Button("Reset Trigger"))
        {
            trigger.ResetTrigger();
        }

        EditorGUILayout.EndHorizontal();
    }
}
#endif
