using UnityEngine;

[System.Serializable]
public class SecondOrderDynamics
{
    float f, z, r;
    Vector3 xp; // previous target
    Vector3 y, yd; // output position & velocity

    public SecondOrderDynamics(float frequency, float damping, float response, Vector3 initialValue)
    {
        f = frequency;
        z = damping;
        r = response;
        xp = initialValue;
        y = initialValue;
    }

    public Vector3 Update(float dt, Vector3 x)
    {
        if (dt == 0f) return y;

        float k1 = z / (Mathf.PI * f);
        float k2 = 1f / ((2f * Mathf.PI * f) * (2f * Mathf.PI * f));
        float k3 = r * z / (2f * Mathf.PI * f);

        Vector3 xd = (x - xp) / dt;
        xp = x;

        y += yd * dt;
        yd += (x + k3 * xd - y - k1 * yd) / k2 * dt;

        return y;
    }
}
