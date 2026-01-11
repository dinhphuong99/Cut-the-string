using UnityEngine;

/// <summary>
/// Mỗi node đại diện cho một điểm vật lý trên dây.
/// Node có thể chịu lực từ các node lân cận và trọng lực.
/// </summary>
[System.Serializable]
public class RopeNode
{
    public Vector3 position;      // Vị trí hiện tại
    public Vector3 velocity;      // Vận tốc hiện tại
    public float priority;        // Độ ưu tiên (0–1) dùng cho hai đầu dây

    public RopeNode head;         // Node phía trên (hoặc trước)
    public RopeNode headAdjacent; // Node phía trên nữa (i-2)
    public RopeNode tail;         // Node phía dưới (hoặc sau)
    public RopeNode tailAdjacent; // Node phía dưới nữa (i+2)

    public bool isFixed;          // Node cố định hoàn toàn (ví dụ đầu dây buộc cố định)
    private RopeNodeSettings settings; // Tham chiếu cấu hình vật lý

    public RopeNode(Vector3 pos, RopeNodeSettings settings, bool fixedNode = false, float priority = 0f)
    {
        position = pos;
        velocity = Vector3.zero;
        this.isFixed = fixedNode;
        this.priority = priority;
        this.settings = settings;
    }

    /// <summary>
    /// Tính tổng hợp tất cả các lực tác dụng lên node hiện tại.
    /// Bao gồm: trọng lực, lực đàn hồi, lực kéo, lực uốn, và lực damping.
    /// </summary>
    public Vector3 ComputeForces()
    {
        if (isFixed) return Vector3.zero; // node cố định thì không chịu lực

        Vector3 totalForce = Vector3.zero;

        // 1. Trọng lực
        totalForce += settings.gravity * settings.gravityScale * settings.mass;

        // 2. Lực đàn hồi từ node lân cận (Spring)
        if (head != null) totalForce += ComputeSpringForce(head);
        if (tail != null) totalForce += ComputeSpringForce(tail);

        // 3. Lực uốn (bending) từ node i-2 và i+2
        if (settings.useBendI2)
        {
            if (headAdjacent != null) totalForce += ComputeBendForce(headAdjacent);
            if (tailAdjacent != null) totalForce += ComputeBendForce(tailAdjacent);
        }

        // 4. Lực cản (Damping)
        totalForce += -velocity * (1f - settings.damping);

        return totalForce;
    }

    // -------------------------------------------------------------

    private Vector3 ComputeSpringForce(RopeNode neighbor)
    {
        Vector3 dir = neighbor.position - position;
        float dist = dir.magnitude;
        if (dist < 1e-5f) return Vector3.zero;

        dir.Normalize();
        float extension = dist - settings.restLength;

        // Lực đàn hồi theo Hooke + pretension
        float forceMag = -settings.stiffness * extension;

        // Thêm lực căng (tension) nếu dây bị kéo quá xa
        if (extension > 0f)
            forceMag -= settings.tensionStiffness * extension - settings.pretension;

        return dir * forceMag;
    }

    private Vector3 ComputeBendForce(RopeNode neighbor2)
    {
        Vector3 dir = neighbor2.position - position;
        float dist = dir.magnitude;
        if (dist < 1e-5f) return Vector3.zero;

        dir.Normalize();
        float extension = dist - 2f * settings.restLength;
        float forceMag = -settings.bend2Stiffness * extension;

        return dir * forceMag * 0.5f; // giảm nhẹ để tránh dao động mạnh
    }
}