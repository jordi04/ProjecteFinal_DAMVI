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

    // Mesh baking logic
    public void GenerateMeshCollider()
    {
        SplineContainer splineContainer = GetComponent<SplineContainer>();
        Spline spline = GetComponent<SplineContainer>().Spline;
        Mesh mesh = new Mesh();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int i = 0; i <= resolution; i++)
        {
            float t = i / (float)resolution;
            splineContainer.Evaluate(0, t, out float3 pos, out float3 tangent, out float3 up);

            Vector3 center = spline.EvaluatePosition(t);
            Vector3 right = math.normalize(math.cross(tangent, up));

            Vector3 offsetRight = right * (width * 0.5f);
            Vector3 offsetUp = (Vector3)(-up * height);

            vertices.Add(center + offsetRight);
            vertices.Add(center - offsetRight);

            vertices.Add(center + offsetUp);
            vertices.Add(center - offsetUp);
        }

        for (int i = 0; i < resolution; i++)
        {
            int vi = i * 4;

            // Top face
            triangles.Add(vi);
            triangles.Add(vi + 4);
            triangles.Add(vi + 1);

            triangles.Add(vi + 1);
            triangles.Add(vi + 4);
            triangles.Add(vi + 5);

            // Bottom face (inverted winding)
            triangles.Add(vi + 2);
            triangles.Add(vi + 3);
            triangles.Add(vi + 6);

            triangles.Add(vi + 3);
            triangles.Add(vi + 7);
            triangles.Add(vi + 6);

            // Left side
            triangles.Add(vi + 1);
            triangles.Add(vi + 5);
            triangles.Add(vi + 3);

            triangles.Add(vi + 3);
            triangles.Add(vi + 5);
            triangles.Add(vi + 7);

            // Right side
            triangles.Add(vi + 2);
            triangles.Add(vi + 6);
            triangles.Add(vi);

            triangles.Add(vi);
            triangles.Add(vi + 6);
            triangles.Add(vi + 4);
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
            river.GenerateMeshCollider();
            EditorUtility.SetDirty(river);
        }
    }
}
#endif
