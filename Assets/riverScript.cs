using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[ExecuteInEditMode]
public class RiverScript : MonoBehaviour
{
    [SerializeField] private SplineContainer m_splineContainer;
    [SerializeField] private float m_width = 1f;
    [SerializeField] private AnimationCurve m_widthCurve = AnimationCurve.Linear(0, 1, 1, 1);

    public void SampleSplineWidth(float t, out Vector3 p1, out Vector3 p2)
    {
        m_splineContainer.Evaluate(0, t, out float3 pos, out float3 tangent, out float3 up);
        float3 right = math.normalize(math.cross(tangent, up));
        float widthMult = m_widthCurve.Evaluate(t);
        float3 offset = right * m_width * widthMult * 0.5f;

        p1 = pos + offset;
        p2 = pos - offset;
    }
}