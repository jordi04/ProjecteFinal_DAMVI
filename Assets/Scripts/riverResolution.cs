using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RiverResolution : MonoBehaviour
{
    [SerializeField, Range(2, 100)] private int resolution = 10;
    [SerializeField] private RiverScript m_splineSampler;
    [SerializeField] private float m_worldSizePerUV = 2f;

    private Mesh m_mesh;

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            GenerateMesh();
#endif
    }

    public void GenerateMesh()
    {
        if (m_splineSampler == null || resolution < 2)
            return;

        var vertsP1 = new List<Vector3>();
        var vertsP2 = new List<Vector3>();
        float step = 1f / (resolution - 1);

        for (int i = 0; i < resolution; i++)
        {
            float t = step * i;
            m_splineSampler.SampleSplineWidth(t, out Vector3 p1, out Vector3 p2);
            vertsP1.Add(transform.InverseTransformPoint(p1));
            vertsP2.Add(transform.InverseTransformPoint(p2));
        }

        BuildMesh(vertsP1, vertsP2);
    }

    private void BuildMesh(List<Vector3> p1List, List<Vector3> p2List)
    {
        if (m_mesh == null)
        {
            m_mesh = new Mesh { name = "River Mesh" };
        }
        else
        {
            m_mesh.Clear();
        }

        var verts = new List<Vector3>();
        var indices = new List<int>();
        var uvs = new List<Vector2>();
        var normals = new List<Vector3>();

        float[] segmentLengths = new float[resolution - 1];
        List<float> worldDistances = new List<float> { 0f };

        for (int i = 0; i < resolution; i++)
        {
            Vector3 p1 = p1List[i];
            Vector3 p2 = p2List[i];
            verts.Add(p1);
            verts.Add(p2);

            Vector3 across = (p2 - p1).normalized;
            Vector3 normal = Vector3.Cross(Vector3.forward, across).normalized;
            normals.Add(normal);
            normals.Add(normal);

            if (i > 0)
            {
                Vector3 prevMid = (p1List[i - 1] + p2List[i - 1]) * 0.5f;
                Vector3 currMid = (p1 + p2) * 0.5f;
                float segLen = Vector3.Distance(prevMid, currMid);
                segmentLengths[i - 1] = segLen;
                worldDistances.Add(worldDistances[i - 1] + segLen);
            }
        }

        for (int i = 0; i < resolution; i++)
        {
            float v = worldDistances[i] / m_worldSizePerUV;

            float width = Vector3.Distance(p1List[i], p2List[i]);
            float u0 = 0f;
            float u1 = width / m_worldSizePerUV;

            uvs.Add(new Vector2(u0, v));
            uvs.Add(new Vector2(u1, v));
        }

        for (int i = 0; i < resolution - 1; i++)
        {
            int idx = i * 2;
            indices.Add(idx); indices.Add(idx + 2); indices.Add(idx + 1);
            indices.Add(idx + 1); indices.Add(idx + 2); indices.Add(idx + 3);
        }

        m_mesh.SetVertices(verts);
        m_mesh.SetTriangles(indices, 0);
        m_mesh.SetUVs(0, uvs);
        m_mesh.SetNormals(normals);
        m_mesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = m_mesh;
    }
}
