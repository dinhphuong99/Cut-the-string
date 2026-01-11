using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stateless builder: từ profile + start/end -> trả về List<RopeNode4>.
/// Không phụ thuộc vào GameObject hoặc MonoBehaviour.
/// </summary>
public interface IRopeBuilder
{
    List<RopeNode4> Build(RopeProfile profile, Vector3 start, Vector3 end, bool pinStart = true, bool pinEnd = true);
}
