using UnityEngine;

public class RopeForceSolver
{
    private readonly RopePhysicsProfile profile;

    public RopeForceSolver(RopePhysicsProfile profile)
    {
        this.profile = profile;
    }

    /// <summary>
    /// Tính và tích hợp lực (Hooke + Gravity) bằng semi-implicit Euler.
    /// </summary>
    public void IntegrateForce(ref Vector2 pos, ref Vector2 vel, Rigidbody2D bob, 
        Vector2 pivot, float restLength, float dampingPerSub, float dt)
    {
        Vector2 dir = pos - pivot;
        float dist = dir.magnitude;
        if (dist < 1e-5f)
        {
            dir = Vector2.right * 1e-4f;
            dist = dir.magnitude;
        }

        Vector2 dirNorm = dir / dist;

        // Gravity
        Vector2 force = Vector2.down * profile.gravity * bob.mass;

        // Hooke (chỉ khi giãn)
        float stretch = dist - restLength;
        if (stretch > 0f)
            force += -profile.k * stretch * dirNorm;

        // Semi-implicit Euler integration
        vel += (force / bob.mass) * dt;
        vel *= dampingPerSub;
        pos += vel * dt;
    }

    /// <summary>
    /// Giới hạn độ giãn tối đa, loại bỏ vận tốc hướng ra ngoài.
    /// </summary>
    public void ClampStretch(ref Vector2 pos, ref Vector2 vel, Vector2 pivot, float restLength)
    {
        float maxDist = restLength * profile.maxStretchFactor;
        Vector2 fromPivot = pos - pivot;
        float fromPivotMag = fromPivot.magnitude;

        if (fromPivotMag > maxDist)
        {
            Vector2 dirNorm = fromPivot / fromPivotMag;
            pos = pivot + dirNorm * maxDist;

            float radial = Vector2.Dot(vel, dirNorm);
            vel -= radial * dirNorm; // giữ thành phần tiếp tuyến
        }
    }
}