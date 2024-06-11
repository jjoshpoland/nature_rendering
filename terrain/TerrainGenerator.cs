using Godot;
using System;
using System.Collections.Generic;

[Tool]
public partial class TerrainGenerator : Node3D
{
    [Export]
    public bool needsUpdate = false;
    [Export]
    public bool needsRegenerate = false;
    [Export]
    public int Size = 1024;
    [Export]
    public float MaxHeight = 250f;
    [Export]
    public int maxLevel = 4;
    [Export]
    public int baseResolution = 64;
    [Export]
    public float LODDist = 250f;
    [Export]
    public int LODLevels = 3;
    [Export]
    public Node3D Player;
    [Export]
    public NoiseMap HeightMap;
    private QuadTreeNode quadTreeRoot;
    private Vector3 prevPlayerPos;
    
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        if (needsRegenerate)
        {
            Clear();
            needsRegenerate = false;
        }
        if (!needsUpdate) return;
        
        if (quadTreeRoot == null) {
            Generate();
        }
        if (Player != null && prevPlayerPos != Player.GlobalPosition)
        {
            UpdateLod(Player.GlobalPosition);
            prevPlayerPos = Player.GlobalPosition;
        }
	}

    public void Generate()
    {
        quadTreeRoot = new QuadTreeNode(0, LODDist, new Rect2(Vector2.Zero, new Vector2(Size, Size)));
        AddChild(quadTreeRoot);
        quadTreeRoot.Owner = this;
        GenerateQuadtree(quadTreeRoot);
    }

    public void Clear()
    {
        if(quadTreeRoot != null)
        {
            quadTreeRoot.QueueFree();
            quadTreeRoot = null;
        }
        
        foreach(var child in GetChildren())
        {
            if(child is MeshInstance3D)
            {
                child.QueueFree();
            }
        }
    }

    private void GenerateQuadtree(QuadTreeNode node)
    {
        if (node.Level < maxLevel)
        {
            node.Subdivide();
            foreach (var child in node.Children)
            {
                GenerateQuadtree(child);
            }
        }
        else
        {
            //node.LodMeshes = new List<Mesh>(4);
            for (int i = 0; i < LODLevels; i++)
            {
                node.LodMeshes.Add(null);
                node.LodGenerated.Add(false);
            }
        }
    }

    private List<Mesh> GenerateLodMeshes(Rect2 bounds)
    {
        var lodMeshes = new List<Mesh>();
        int lodLevels = LODLevels;  // Number of LOD levels
        for (int lod = 0; lod < lodLevels; lod++)
        {
            int resolution = (int)Math.Pow(2, lod);
            var mesh = GenerateMesh(bounds, resolution);
            
            lodMeshes.Add(mesh);
        }
        return lodMeshes;
    }

    private Mesh GenerateLodMesh(Rect2 bounds, int lod)
    {
        int resolution = (int)Math.Pow(2, lod + 1);  // Ensure resolution is at least 2x2
        return GenerateMesh(bounds, resolution);
    }

    private Mesh GenerateMesh(Rect2 bounds, int resolution)
    {
        var surfaceTool = new SurfaceTool();
        var dataTool = new MeshDataTool();
        List<Vertex> vertices = new List<Vertex>();
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

        int width = baseResolution / resolution;
        int height = baseResolution / resolution;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float fx = x / (float)(width - 2);
                float fy = y / (float)(height - 2);
                float heightValue = HeightMap.SampleMap(bounds.Position.X + fx * bounds.Size.X, bounds.Position.Y + fy * bounds.Size.Y, Size);
                Vertex vertex = new Vertex(new Vector3(bounds.Position.X + fx * bounds.Size.X, heightValue * MaxHeight, bounds.Position.Y + fy * bounds.Size.Y), new Vector2I(x, y));
                vertices.Add(vertex);
            }
        }

        foreach(Vertex vertex in vertices)
        {
            int x = vertex.Coords.X;
            int y = vertex.Coords.Y;
            surfaceTool.AddVertex(vertex.Position);

            if (x < width - 1 && y < height - 1)
            {
                surfaceTool.AddIndex(y * width + x);
                surfaceTool.AddIndex(y * width + x + 1);
                surfaceTool.AddIndex((y + 1) * width + x);

                surfaceTool.AddIndex(y * width + x + 1);
                surfaceTool.AddIndex((y + 1) * width + x + 1);
                surfaceTool.AddIndex((y + 1) * width + x);
            }
        }
        surfaceTool.GenerateNormals();


        return surfaceTool.Commit(); 
    }

    public void UpdateLod(Vector3 cameraPosition)
    {
        UpdateLodRecursively(quadTreeRoot, cameraPosition);
    }

    private void UpdateLodRecursively(QuadTreeNode node, Vector3 cameraPosition)
    {
        if (node.Children.Count == 0)
        {
            var distance = cameraPosition.DistanceTo(new Vector3(node.Bounds.Position.X + node.Bounds.Size.X / 2.0f, 0, node.Bounds.Position.Y + node.Bounds.Size.Y / 2.0f));
            int lodLevel = (int)(distance / LODDist);
            lodLevel = Mathf.Clamp(lodLevel, 0, node.LodMeshes.Count - 1);
            // Generate the LOD mesh if it hasn't been generated yet
            if (!node.LodGenerated[lodLevel])
            {
                node.LodMeshes[lodLevel] = GenerateLodMesh(node.Bounds, lodLevel);
                node.LodGenerated[lodLevel] = true;
            }

            if (node.MeshInstance.Mesh != node.LodMeshes[lodLevel])
            {
                node.MeshInstance.Mesh = node.LodMeshes[lodLevel];
            }

            if (node.MeshInstance.GetParent() == null)
            {
                AddChild(node.MeshInstance);
            }
        }
        else
        {
            foreach (var child in node.Children)
            {
                UpdateLodRecursively(child, cameraPosition);
            }
        }
    }

    public struct Vertex
    {
        public Vector3 Position;
        public Vector2I Coords;

        public Vertex(Vector3 position, Vector2I coords)
        {
            Position = position;
            Coords = coords;
        }
    }
}
