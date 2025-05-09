using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using UnityEditor;
using System.Collections.Generic;

[ExecuteInEditMode]
[RequireComponent(typeof(SplineContainer), typeof(MeshFilter), typeof(MeshCollider))]
public class RiverColliderTool : MonoBehaviour
{
    public float width = 1f;
    public float height = 1f;
    public int resolution = 50;

    public void Generate2DVerticalMesh()
    {
        SplineContainer splineContainer = GetComponent<SplineContainer>();
        Spline spline = splineContainer.Spline;
        Mesh mesh = new Mesh();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int i = 0; i <= resolution; i++)
        {
            float t = i / (float)resolution;
            splineContainer.Evaluate(0, t, out float3 pos, out float3 tangent, out float3 up);

            Vector3 center = spline.EvaluatePosition(t);

            // Always use world up for vertical orientation
            Vector3 upVec = Vector3.up * (height * 0.5f);

            // Right vector perpendicular to tangent and world up
            Vector3 right = Vector3.Cross(tangent, Vector3.up).normalized * (width * 0.5f);

            // Two vertices per segment: top and bottom
            vertices.Add(center + upVec); // Top
            vertices.Add(center - upVec); // Bottom
        }

        // Build triangles (quads between each segment)
        for (int i = 0; i < resolution; i++)
        {
            int vi = i * 2;

            // First triangle
            triangles.Add(vi);
            triangles.Add(vi + 2);
            triangles.Add(vi + 1);

            // Second triangle
            triangles.Add(vi + 1);
            triangles.Add(vi + 2);
            triangles.Add(vi + 3);
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();

        MeshFilter mf = GetComponent<MeshFilter>();
        mf.mesh = mesh;

        MeshCollider mc = GetComponent<MeshCollider>();
        mc.sharedMesh = mesh;
    }


}

#if UNITY_EDITOR
[CustomEditor(typeof(RiverColliderTool))]
public class RiverColliderToolEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RiverColliderTool river = (RiverColliderTool)target;

        GUILayout.Space(10);
        if (GUILayout.Button("Bake River Mesh"))
        {
            river.Generate2DVerticalMesh();
            EditorUtility.SetDirty(river);
        }
    }
}
#endif
