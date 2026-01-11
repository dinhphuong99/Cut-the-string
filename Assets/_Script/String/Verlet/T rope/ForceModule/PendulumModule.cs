using UnityEngine;

[DisallowMultipleComponent]
public class PendulumModule : MonoBehaviour, IRopeModule
{
    private IPendulumData data;
    private bool initialized = false;
    private bool canSimulate = false;
    private float dampingCComputed = 0f;

    public void Initialize(IRopeDataProvider rp)
    {
        if (initialized) return;

        if (rp == null)
        {
            Debug.LogError("[PendulumModule] IRopeDataProvider is null", this);
            initialized = true;
            canSimulate = false;
            return;
        }

        // prefer provider implementing IPendulumData; if not, try to find an adapter component on provider
        data = rp as IPendulumData;
        if (data == null)
        {
            var comp = rp as Component;
            if (comp != null)
            {
                data = comp.GetComponent<IPendulumData>();
                if (data == null)
                    data = comp.GetComponentInChildren<IPendulumData>(true);
            }
        }

        if (data == null)
        {
            Debug.LogWarning("[PendulumModule] No IPendulumData found on provider. Pendulum disabled.", this);
            initialized = true;
            canSimulate = false;
            return;
        }

        // basic checks
        if (!data.IsReady || data.Bob == null || data.Anchor == null || data.IdealLength <= 0f)
        {
            initialized = true;
            canSimulate = false;
            return;
        }

        // compute damping coefficient c (based on profile & bob mass)
        var profile = data.PendulumPhysics;
        if (profile == null)
        {
            Debug.LogWarning("[PendulumModule] PendulumPhysicsProfile is null. Pendulum disabled.", this);
            initialized = true;
            canSimulate = false;
            return;
        }

        float mass = Mathf.Max(0.0001f, data.Bob.mass);
        if (profile.useCriticalDamping)
        {
            float cCritical = 2f * Mathf.Sqrt(profile.k * mass);
            dampingCComputed = cCritical * profile.dampingZeta;
        }
        else
        {
            dampingCComputed = profile.dampingC_Fixed;
        }

        // if designer set gravityScale = 0 unintentionally, set once and warn
        if (Mathf.Approximately(data.Bob.gravityScale, 0f))
        {
            data.Bob.gravityScale = 1f;
            Debug.LogWarning("[PendulumModule] Bob.gravityScale was 0 - set to 1 (only once). Prefer setting in prefab.", data.Bob);
        }

        canSimulate = true;
        initialized = true;
    }

    private void FixedUpdate()
    {
        if (!initialized || !canSimulate) return;
        if (data == null || !data.IsReady) return;
        if (data.Anchor == null || data.Bob == null) return;

        var profile = data.PendulumPhysics;
        int steps = Mathf.Max(1, data.Substeps);

        // split total force across substeps so per-FixedUpdate behavior is stable
        for (int i = 0; i < steps; i++)
            SimulateStep(1f / steps);
    }

    private void SimulateStep(float stepFraction)
    {
        Vector2 bobPos = data.Bob.position;
        Vector2 anchorPos = data.Anchor.position;

        Vector2 delta = bobPos - anchorPos;
        float dist = delta.magnitude;
        if (dist < 0.0001f) return;
        Vector2 dir = delta / dist;

        float restLength = data.IdealLength;
        float extension = dist - restLength;

        var profile = data.PendulumPhysics;

        // clamp extension by material limit to prevent explosion
        float maxStretchLength = restLength * Mathf.Max(1f, profile.maxStretchFactor);
        if (dist > maxStretchLength)
        {
            extension = maxStretchLength - restLength;
            dir = (bobPos - anchorPos).normalized;
        }

        Vector2 totalForce = Vector2.zero;

        // spring force: only when extension > 0 or when bidirectional enabled
        if (extension > 0f || profile.bidirectionalElasticity)
        {
            float x = extension;
            if (extension < 0f && !profile.bidirectionalElasticity) x = 0f;

            float springScalar = -profile.k * x;
            Vector2 springForce = dir * springScalar;
            totalForce += springForce;

            // directional viscous damping along rope
            float vAlong = Vector2.Dot(data.Bob.linearVelocity, dir);
            Vector2 dampingForce = -dampingCComputed * vAlong * dir;
            totalForce += dampingForce;
        }

        // air drag
        if (profile.airDragC > 0f)
        {
            Vector2 airForce = -profile.airDragC * data.Bob.linearVelocity;
            totalForce += airForce;
        }

        // apply scaled per substep
        data.Bob.AddForce(totalForce * stepFraction, ForceMode2D.Force);
    }
}
