using UnityEngine;
using System.Collections.Generic;

public class CutRopeBehavior : MonoBehaviour
{
    [Header("References")]
    public RopeVerletVisual targetRope;      // Rope ban đầu muốn cắt
    public int cutIndex = 10;

    [Header("Debug Output")]
    public List<RopeVerletVisual.Node> leftNodes = new List<RopeVerletVisual.Node>();
    public List<RopeVerletVisual.Node> rightNodes = new List<RopeVerletVisual.Node>();

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            TryCutRope();
        }
    }

    void TryCutRope()
    {
        if (targetRope == null)
        {
            Debug.LogWarning("Missing targetRope reference.");
            return;
        }

        // Lấy node list gốc
        var nodesField = targetRope.GetType().GetField("nodes",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (nodesField == null)
        {
            Debug.LogWarning("Cannot access nodes array in RopeVerletVisual.");
            return;
        }

        RopeVerletVisual.Node[] originalNodes = (RopeVerletVisual.Node[])nodesField.GetValue(targetRope);

        if (originalNodes == null || originalNodes.Length < 3)
        {
            Debug.LogWarning("Invalid rope nodes.");
            return;
        }

        if (cutIndex <= 0 || cutIndex >= originalNodes.Length - 1)
        {
            Debug.LogWarning("Cut index out of range.");
            return;
        }

        // Cắt danh sách node
        int leftCount = cutIndex + 1;
        int rightCount = originalNodes.Length - cutIndex;

        var left = new RopeVerletVisual.Node[leftCount];
        var right = new RopeVerletVisual.Node[rightCount];

        leftNodes = new(originalNodes[..(cutIndex + 1)]);
        rightNodes = new(originalNodes[cutIndex..]);

        Debug.Log($"Cut success. Left={left.Length}, Right={right.Length}");
    }

   
}