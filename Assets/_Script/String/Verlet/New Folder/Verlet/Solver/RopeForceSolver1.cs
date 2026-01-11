using UnityEngine;

/// <summary>
/// Bộ giải lực cho dây hoặc lò xo.
/// Mô phỏng bằng semi-implicit Euler, có hỗ trợ hai chế độ:
/// - Rope: chỉ kéo khi giãn.
/// - Spring: kéo và đẩy khi nén hoặc giãn.
/// </summary>
public class RopeForceSolver1
{
    private readonly RopePhysicsProfile profile;

    public RopeForceSolver1(RopePhysicsProfile profile)
    {
        this.profile = profile;
    }

    /// <summary>
    /// Tính và tích hợp lực (Hooke + Gravity) bằng semi-implicit Euler.
    /// </summary>
    public void IntegrateForce(ref Vector2 pos, ref Vector2 vel, float mass, 
        Vector2 pivot, float restLength, float dampingPerSub, float dt)
    {
        Vector2 dir = pos - pivot;
        float dist = dir.magnitude;

        // Nếu trùng vị trí thì ép hướng tạm để tránh NaN
        if (dist < 1e-6f)
        {
            dir = Vector2.right * 1e-4f;
            dist = dir.magnitude;
        }

        Vector2 dirNorm = dir / dist;

        // Lực tổng
        Vector2 force = Vector2.down * profile.gravity * mass; // Gravity luôn có

        // Hooke’s Law
        float stretch = dist - restLength;
        if (profile.bidirectionalElasticity || stretch > 0f)
        {
            Vector2 hooke = -profile.k * stretch * dirNorm;
            force += hooke;
        }
        else if (!profile.bidirectionalElasticity && stretch < 0f)
        {
            // Giảm nhẹ vận tốc hướng vào trục khi dây bị chùng
            float inwardVel = Vector2.Dot(vel, dirNorm);
            if (inwardVel < 0f)
                vel -= inwardVel * dirNorm * 0.25f; // hệ số 0.25f tránh rung
        }

        // Semi-implicit Euler
        vel += (force / mass) * dt;
        vel *= dampingPerSub;
        pos += vel * dt;

        // Giới hạn vận tốc để ổn định
        float vSqr = vel.sqrMagnitude;
        float maxV = profile.maxVelocity;
        if (vSqr > maxV * maxV)
            vel = vel.normalized * maxV;
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
            pos = Vector2.Lerp(pos, pivot + dirNorm * maxDist, 0.5f); // clamp mềm

            float radial = Vector2.Dot(vel, dirNorm);
            vel -= radial * dirNorm * 0.8f; // giữ lại một phần vận tốc tiếp tuyến
        }
    }
}