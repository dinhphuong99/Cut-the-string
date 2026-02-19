using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RopeSimulation : MonoBehaviour, IRopeModule
{
    private IRopeSimulationDataProvider simData; // local minimal adapter
    private bool initialized = false;
    private float currentSegmentLength;

    // accumulator pattern & fixed-step
    [Header("Simulation tuning")]
    [Tooltip("Fixed step for internal substepping (seconds)")]
    [SerializeField] private float fixedStep = 0.02f;
    [SerializeField] private int maxSubSteps = 4;

    private float accumulator = 0f;

    public void Initialize(IRopeDataProvider provider)
    {
        if (initialized) return;
        // prefer concrete provider that exposes simulation internals
        if (provider is IRopeSimulationDataProvider p)
        {
            simData = p;
            initialized = true;
        }
        else
        {
            Debug.LogWarning("[RopeSimulation] provider does not implement IRopeSimulationDataProvider. Disabling.", this);
            enabled = false;
        }
    }

    private void FixedUpdate()
    {
        if (!initialized) return;
        if (!simData.Simulate || !simData.IsReady) return;

        accumulator += Time.fixedDeltaTime;
        int loops = 0;
        while (accumulator >= fixedStep && loops < maxSubSteps)
        {
            Step(fixedStep);
            accumulator -= fixedStep;
            loops++;
        }
    }

    private void Step(float dt)
    {
        var nodes = simData.Nodes;
        if (nodes == null || nodes.Count < 2) return;

        currentSegmentLength = ComputeCurrentSegmentLength();

        SimulateVerlet(dt);
        SolveDistanceConstraints();
    }

    private void SimulateVerlet(float dt)
    {
        Vector2 gravity = Vector2.down * simData.Gravity * dt * dt;
        float damping = simData.Damping;
        var nodes = simData.Nodes;
        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            if (n.isPinned) continue;
            Vector2 vel = (n.position - n.oldPosition) * damping;
            n.oldPosition = n.position;
            n.position += vel + gravity;
            nodes[i] = n;
        }
    }

    private void SolveDistanceConstraints()
    {
        var nodes = simData.Nodes;
        float rest = currentSegmentLength;
        int iterations = simData.ConstraintIterations;
        for (int iter = 0; iter < iterations; iter++)
        {
            for (int i = 0; i < nodes.Count - 1; i++)
            {
                var a = nodes[i];
                var b = nodes[i + 1];
                Vector2 delta = b.position - a.position;
                float dist = delta.magnitude;
                if (dist <= Mathf.Epsilon) continue;
                float error = (dist - rest) / dist;
                Vector2 corr = delta * error;
                if (a.isPinned && b.isPinned) { }
                else if (a.isPinned) { b.position -= corr; }
                else if (b.isPinned) { a.position += corr; }
                else
                {
                    corr *= 0.5f;
                    a.position += corr;
                    b.position -= corr;
                }
                nodes[i] = a;
                nodes[i + 1] = b;
            }
        }
    }

    private float ComputeCurrentSegmentLength()
    {
        var nodes = simData.Nodes;
        int count = nodes.Count;
        if (count < 2)
            return simData.SegmentLength;
        float totalLength = 0f;

        for (int i = 0; i < count - 1; i++)
        {
            totalLength += Vector2.Distance(nodes[i].position, nodes[i + 1].position);
        }

        float maxStraightLength = Vector2.Distance(nodes[0].position, nodes[count - 1].position);

        // 0 = hoàn toàn chùng, 1 = căng thẳng hoàn toàn
        float tension01 = Mathf.Clamp01(totalLength / maxStraightLength);
        // Khi tension tăng → segment ngắn lại
        float current = Mathf.Lerp(simData.SegmentLength, simData.SegmentLength / 3f, tension01);
        return current;
    }
}