using System.Collections.Generic;

public interface IRopeSimulationDataProvider : IRopeDataProvider
{
    // Note: Do NOT re-declare IsReady here. IRopeDataProvider should own IsReady if needed.
    // This interface focuses on simulation-specific data the RopeSimulation module will mutate/read.

    /// <summary>Mutable node list used by simulation modules.</summary>
    IList<RopeNode4> Nodes { get; }

    /// <summary>Gravity magnitude (used by simulation integrator).</summary>
    float Gravity { get; }

    /// <summary>Global damping multiplier used by the integrator.</summary>
    float Damping { get; }

    /// <summary>Rest segment length used by the distance constraint solver.</summary>
    float SegmentLength { get; }

    /// <summary>Number of constraint relaxation iterations.</summary>
    int ConstraintIterations { get; }

    /// <summary>Whether the rope should be simulated (allows runtime toggle).</summary>
    bool Simulate { get; }

    bool IsTaut { get; }

    float GetCurrentRopeLength();

    float ComputeIdealLength();
}