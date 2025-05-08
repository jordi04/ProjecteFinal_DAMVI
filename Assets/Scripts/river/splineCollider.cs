using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(SplineContainer))]
public class splineCollider : MonoBehaviour
{
    public float colliderWidth = 1f;
    public float colliderHeight = 0.1f;
    public int resolution = 20;

    private void Start()
    {
        GenerateColliders();
    }

    void GenerateColliders()
    {
        Spline spline = GetComponent<SplineContainer>().Spline;
        float step = 1f / resolution;

        for (int i = 0; i < resolution; i++)
        {
            float t0 = i * step;
            float t1 = (i + 1) * step;

            Vector3 p0 = spline.EvaluatePosition(t0);
            Vector3 p1 = spline.EvaluatePosition(t1);

            Vector3 direction = p1 - p0;
            float length = direction.magnitude;
            Vector3 center = (p0 + p1) / 2;

            GameObject colliderObj = new GameObject("SplineCollider_" + i);
            colliderObj.transform.parent = this.transform;
            colliderObj.transform.position = center;
            colliderObj.transform.rotation = Quaternion.LookRotation(direction);

            BoxCollider box = colliderObj.AddComponent<BoxCollider>();
            box.size = new Vector3(colliderWidth, colliderHeight, length);
        }
    }
}
