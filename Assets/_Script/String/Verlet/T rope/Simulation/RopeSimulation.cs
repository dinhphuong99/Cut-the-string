using UnityEngine;
[DisallowMultipleComponent] 
public sealed class RopeSimulation : MonoBehaviour, IRopeModule 
{ 
    private IRopeSimulationData data; 
    private bool initialized = false; 
    private float currentSegmentLength; 
    public void Initialize(IRopeDataProvider rp) { 
        if (initialized) return; 
        // prefer direct cast to mutation-capable interface
        data = rp as IRopeSimulationData; 
        if (data == null) { 
            Debug.LogWarning("[RopeSimulation] IRopeSimulationData not available on provided rope. Disabling simulation.", this); 
            enabled = false; 
            return; 
        } 
        initialized = true; 
    } 
    
    private void FixedUpdate() { 
        if (!initialized) return; 
        if (!data.Simulate || !data.IsReady) return; 
        var nodes = data.Nodes; 
        if (nodes == null || nodes.Count < 2) return; 
        Tick(Time.fixedDeltaTime); 
    } 
    
    // ------------------------------------------------------------ 
    // Public tick – cho test, replay, sub-step 
    // ------------------------------------------------------------
    public void Tick(float dt) { 
        if (data.IsTaut && data.CurentLength == data.IdealLength*1.135f) { 
            currentSegmentLength = ComputeCurrentSegmentLength(); 
        } else { 
            currentSegmentLength = data.SegmentLength; 
        } 
        
        SimulateVerlet(dt); 
        SolveDistanceConstraints(); 
    } 
    
    // ------------------------------------------------------------ 
    // Verlet integration 
    // ------------------------------------------------------------
    private void SimulateVerlet(float dt) { 
        Vector2 gravity = Vector2.down * data.Gravity; 
        float damping = data.Damping; 
        float dtSq = dt * dt; 
        var nodes = data.Nodes; 
        for (int i = 0; i < nodes.Count; i++) { 
            RopeNode4 n = nodes[i]; 
            if (n.isPinned) continue; 
            Vector2 velocity = (n.position - n.oldPosition) * damping; 
            n.oldPosition = n.position; n.position += velocity + gravity * dtSq; nodes[i] = n; 
        } 
    } 
    
    // ------------------------------------------------------------ 
    // Distance constraints (pin-aware, no anchor logic) 
    // ------------------------------------------------------------
    private void SolveDistanceConstraints() { 
        var nodes = data.Nodes; 
        float restLength = currentSegmentLength; 
        int iterations = data.ConstraintIterations; 
        for (int iter = 0; iter < iterations; iter++) { 
            for (int i = 0; i < nodes.Count - 1; i++) { 
                RopeNode4 a = nodes[i]; 
                RopeNode4 b = nodes[i + 1]; 
                Vector2 delta = b.position - a.position; 
                float dist = delta.magnitude; 
                if (dist <= Mathf.Epsilon) continue; 
                float error = (dist - restLength) / dist; 
                Vector2 correction = delta * error; 
                if (a.isPinned && b.isPinned) { 
                } else if (a.isPinned) { 
                    b.position -= correction; 
                } else if (b.isPinned) { 
                    a.position += correction; 
                } else { 
                    correction *= 0.5f; 
                    a.position += correction; 
                    b.position -= correction; 
                } 
                nodes[i] = a; 
                nodes[i + 1] = b; 
            } 
        } 
    } 
    
    private float ComputeCurrentSegmentLength() { 
        var nodes = data.Nodes; 
        int count = nodes.Count; 
        if (count < 2) 
            return data.SegmentLength; 
        float totalLength = 0f; 
        
        for (int i = 0; i < count - 1; i++) { 
            totalLength += Vector2.Distance(nodes[i].position, nodes[i + 1].position); 
        } 
        
        float maxStraightLength = data.SegmentLength * (count - 1);
        
        // 0 = hoàn toàn chùng, 1 = căng thẳng hoàn toàn
        float tension01 = Mathf.Clamp01(totalLength / maxStraightLength); 
        // Khi tension tăng → segment ngắn lại
        float current = Mathf.Lerp( data.SegmentLength, data.SegmentLength/3f, tension01 ); 
        return current; 
    } 
}