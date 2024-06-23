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
    //public int GrassInstances;
    public DetailChunk[,] GrassChunks;
    public Image ClimateMap;

    public QuadTreeNode(int level, float lodDist, Rect2 bounds)
    {
        Level = level;
        Bounds = bounds;
        Children = new List<QuadTreeNode>();
        LodMeshes = new List<Mesh>();
        MeshInstance = new MeshInstance3D();
        LodGenerated = new List<bool>();
        LODDist = lodDist;
        GrassChunks = new DetailChunk[10,10];
        Vector2 detailChunkSize = bounds.Size / 10f;
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                Vector2 pos = bounds.Position + new Vector2(x * detailChunkSize.X, y * detailChunkSize.Y);
                GrassChunks[x, y] = new DetailChunk(new Rect2(pos, detailChunkSize));
                GrassChunks[x, y].DetailMultiMesh = new MultiMesh();
                GrassChunks[x, y].DetailMultiMeshInstance = new MultiMeshInstance3D();
            }
        }
    }

    public QuadTreeNode()
    {
        Level = 0;
        Bounds = new Rect2();
        LODDist = 100;
        Children = new List<QuadTreeNode>();
        LodMeshes = new List<Mesh>();
        LodGenerated = new List<bool>();
        GrassChunks = new DetailChunk[10, 10];
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

    public void AddDetailChunksToTree(Node3D root)
    {
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                AddChild(GrassChunks[x, y]);
                GrassChunks[x, y].Owner = this;
                GrassChunks[x, y].GlobalPosition = new Vector3(GrassChunks[x, y].Bounds.Position.X, 0, GrassChunks[x, y].Bounds.Position.Y);
                GrassChunks[x,y].DetailMultiMeshInstance.Multimesh = GrassChunks[x,y].DetailMultiMesh;
                GrassChunks[x, y].AddChild(GrassChunks[x, y].DetailMultiMeshInstance);
                GrassChunks[x, y].DetailMultiMeshInstance.Owner = GrassChunks[x, y];
                
            }
        }
    }

    
}
