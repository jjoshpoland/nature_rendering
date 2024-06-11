using Godot;
using System;
using System.Collections.Generic;

[Tool]
public partial class QuadTreeNode : Node3D
{
    public int Level { get; private set; }
    public Rect2 Bounds { get; private set; }
    public List<QuadTreeNode> Children { get; private set; }
    public MeshInstance3D MeshInstance { get; private set; }
    public List<Mesh> LodMeshes { get;  set; }
    public List<bool> LodGenerated { get; private set; }
    public float LODDist {  get; private set; }

    public QuadTreeNode(int level, float lodDist, Rect2 bounds)
    {
        Level = level;
        Bounds = bounds;
        Children = new List<QuadTreeNode>();
        LodMeshes = new List<Mesh>();
        MeshInstance = new MeshInstance3D();
        LodGenerated = new List<bool>();
        LODDist = lodDist;
    }

    public QuadTreeNode()
    {
        Level = 4;
        Bounds = new Rect2();
        LODDist = 100;
        Children = new List<QuadTreeNode>();
        LodMeshes = new List<Mesh>();
        LodGenerated = new List<bool>();
        MeshInstance = new MeshInstance3D();
    }

    public void Subdivide()
    {
        if (Children.Count == 0)
        {
            var halfSize = Bounds.Size / 2.0f;
            for (int i = 0; i < 4; i++)
            {
                var offset = new Vector2(i % 2, i / 2) * halfSize;
                var childBounds = new Rect2(Bounds.Position + offset, halfSize);
                QuadTreeNode childNode = new QuadTreeNode(Level + 1, LODDist, childBounds);
                Children.Add(childNode);
                AddChild(childNode);
                childNode.Owner = this;
            }
        }
    }



    
}
