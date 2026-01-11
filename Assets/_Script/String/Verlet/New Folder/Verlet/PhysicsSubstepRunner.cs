using System;
using UnityEngine;

public static class PhysicsSubstepRunner
{
    public static void Run(int substeps, float dt, Action<float> stepExecutor)
    {
        int steps = Mathf.Max(1, substeps);
        float stepDt = dt / steps;

        for (int i = 0; i < steps; i++)
            stepExecutor(stepDt);
    }
}