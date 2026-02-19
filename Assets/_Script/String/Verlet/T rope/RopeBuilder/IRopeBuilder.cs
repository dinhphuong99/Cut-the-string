using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Minimal builder contract that returns a list of runtime RopeNode4.
/// </summary>
public interface IRopeBuilder
{
    List<RopeNode4> Build(RopeProfile profile, Vector3 start, Vector3 end, bool pinStart = true, bool pinEnd = true);
}
