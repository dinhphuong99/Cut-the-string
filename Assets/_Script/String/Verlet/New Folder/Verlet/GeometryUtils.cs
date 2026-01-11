using UnityEngine;

public static class GeometryUtils
{
    /// <summary>
    /// Tìm điểm C trên đoạn thẳng AB cách A một khoảng distance.
    /// Nếu distance > AB, C sẽ nằm tại B.
    /// </summary>
    public static Vector2 GetPointOnLine(Vector2 A, Vector2 B, float distance)
    {
        Vector2 AB = B - A;
        float lengthAB = AB.magnitude;

        if (lengthAB == 0f)
        {
            Debug.LogWarning("A và B trùng nhau, không xác định C.");
            return A;
        }

        // t = tỉ lệ để đi từ A đến C trên đoạn AB
        float t = Mathf.Min(1f, distance / lengthAB);
        Vector2 C = A + AB * t;
        return C;
    }
}