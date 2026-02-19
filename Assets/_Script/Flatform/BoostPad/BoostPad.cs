using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BoostPad : MonoBehaviour
{
    [Tooltip("Impulse magnitude applied along contact normal (positive).")]
    public float impulseMagnitude = 5f;

    [Tooltip("Minimum dot(normal, up) to consider as 'hitting from above'. 0.5 ~= 60 degrees.")]
    [Range(-1f, 1f)]
    public float minNormalDotUp = 0.5f;

    [Tooltip("Seconds before this pad can boost the same Rigidbody again.")]
    public float cooldownSeconds = 0.15f;

    [Tooltip("Optional: clamp maximum resulting speed for safety.")]
    public float maxSpeed = 20f;

    // track last boost time per Rigidbody instance to avoid multi-impulse
    private System.Collections.Generic.Dictionary<Rigidbody2D, float> lastBoostTime =
        new System.Collections.Generic.Dictionary<Rigidbody2D, float>();

    void OnCollisionEnter2D(Collision2D collision)
    {
        // only consider dynamic rigidbodies
        Rigidbody2D rb = collision.rigidbody;
        if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic) return;

        // prevent boosting static geometry etc.
        float now = Time.time;
        if (lastBoostTime.TryGetValue(rb, out float t) && now - t < cooldownSeconds) return;

        // gather the contact normal that points away from the surface
        ContactPoint2D contact = collision.GetContact(0);
        Vector2 normal = contact.normal; // already normalized

        // check approach direction: we want the object to be coming from the side of the pad (e.g., above)
        float dot = Vector2.Dot(normal, Vector2.up);
        if (dot < minNormalDotUp) return; // reject shallow hits or hits from below

        // compute impulse vector. We apply opposite to normal so object is pushed away from surface.
        Vector2 impulse = normal * impulseMagnitude;

        // Apply impulse once (as impulse) at the Rigidbody
        rb.AddForce(impulse, ForceMode2D.Impulse);

        // clamp resulting speed to avoid runaway energy (safety)
        if (rb.linearVelocity.sqrMagnitude > maxSpeed * maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;

        lastBoostTime[rb] = now;
    }

    // optional gizmo: draw pad normal(s)
    void OnDrawGizmosSelected()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c == null) return;
        Gizmos.color = Color.cyan;

        // draw a short normal from collider center upwards for visualization
        Vector3 pos = transform.position;
        Gizmos.DrawLine(pos, pos + transform.up * 0.5f);
    }
}
