using UnityEngine;

public class PendulumSpringForceProfiled1 : MonoBehaviour
{
    public RopePhysicsProfile profile;

    [Header("Spring Settings")]
    public float restLength = 5f;
    public float mass = 1f;
    public Transform anchor { get; private set; }
    public Rigidbody2D rb { get; private set; }

    private PendulumSpringSolver solver;

    private bool dirty = true;

    public void SetProfile(RopePhysicsProfile newProfile)
    {
        profile = newProfile;
        dirty = true;
    }

    public void SetAnchor(Transform newAnchor)
    {
        anchor = newAnchor;
        dirty = true;
    }

    public void SetRigidbody(Rigidbody2D newRb)
    {
        rb = newRb;
        dirty = true;
    }

    void FixedUpdate()
    {
        if (dirty)
        {
            Reinitialize();
            dirty = false;
        }

        if (solver == null)
            return;

        RunSubsteppedSimulation();
    }

    void Reinitialize()
    {
        if (profile == null || anchor == null || rb == null)
        {
            solver = null;
            return;
        }

        rb.gravityScale = 0;
        rb.linearDamping = 0;
        rb.angularDamping = 0;

        solver = new PendulumSpringSolver(profile);
    }


    private void RunSubsteppedSimulation()
    {
        float dt = Time.fixedDeltaTime;
        Vector2 anchorPos = anchor.position;

        PhysicsSubstepRunner.Run(profile.substeps, dt, stepDt =>
        {
            solver.SimulateStep(rb, anchorPos, restLength, mass, stepDt);
        });
    }

    public Vector2 GetCurrentTension()
    {
        if (solver == null || anchor == null || rb == null)
            return Vector2.zero;

        return solver.GetCurrentTension(rb, anchor.position, restLength);
    }

    public void ClearRigidbody()
    {
        rb = null;
        dirty = true;
    }

    public void ClearSolver()
    {
        solver = null;
        dirty = true;
    }

    public void SetExternalParams(Transform anchor, Rigidbody2D rb, float L)
    {
        this.anchor = anchor;
        this.rb = rb;
        restLength = L;
    }

}