using UnityEngine;

public class PendulumSpringSolver
{
    private RopePhysicsProfile profile;

    public PendulumSpringSolver(RopePhysicsProfile profile)
    {
        this.profile = profile;
    }

    public void SimulateStep(Rigidbody2D rb, Vector2 anchorPos, float restLength, float mass, float dt)
    {
        Vector2 bobPos = rb.position;

        Vector2 dir = bobPos - anchorPos;
        float dist = dir.magnitude;
        if (dist < 0.0001f) return;

        dir /= dist;

        float x = dist - restLength;

        // Stretch limit
        float maxStretch = restLength * profile.maxStretchFactor;
        if (dist > maxStretch)
        {
            dist = maxStretch;
            x = dist - restLength;
        }

        // Spring force
        bool canPush = profile.bidirectionalElasticity || x > 0f;
        Vector2 springForce = canPush ? (-profile.k * x * dir) : Vector2.zero;

        // Gravity (internal, not rb.gravityScale)
        Vector2 gravityForce = new Vector2(0, -profile.gravity) * mass;

        // Damping multiplicative
        rb.linearVelocity *= profile.damping;

        // Total force
        Vector2 totalForce = springForce + gravityForce;

        rb.AddForce(totalForce, ForceMode2D.Force);

        // Velocity clamp
        float vMag = rb.linearVelocity.magnitude;
        if (vMag > profile.maxVelocity)
            rb.linearVelocity = rb.linearVelocity.normalized * profile.maxVelocity;
    }

    public Vector2 GetCurrentTension(Rigidbody2D rb, Vector2 anchorPos, float restLength)
    {
        Vector2 bobPos = rb.position;

        Vector2 dir = bobPos - anchorPos;
        float dist = dir.magnitude;
        if (dist < 1e-6f) return Vector2.zero;

        dir /= dist;
        float x = dist - restLength;
        if (x <= 0f) return Vector2.zero;

        return -profile.k * x * dir;
    }
}