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
    public float LODDist = 256f;
    [Export]
    public int LODLevels = 3;
    [Export]
    public Node3D Player;
    [Export]
    public NoiseMap HeightMap;
    [Export]
    public ShaderMaterial TerrainMaterial;
    [Export]
    public Image[] TerrainTextures;
    private Texture2DArray TerrainTextureArray;
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
        TerrainTextureArray = new Texture2DArray();
        TerrainTextureArray.CreateFromImages(new Godot.Collections.Array<Image>(TerrainTextures));
        TerrainMaterial.SetShaderParameter("texture_array", TerrainTextureArray);
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
            if(child is MeshInstance3D || child is QuadTreeNode)
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
        surfaceTool.SetCustomFormat(0, SurfaceTool.CustomFormat.RgbaFloat);

        int width = baseResolution / resolution;
        int height = baseResolution / resolution;

        float[,] heightCache = new float[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float fx = x / (float)(width - 2);
                float fy = y / (float)(height - 2);
                //if (resolution <= 2)
                //{
                //    fx = x / (float)(width - 1);
                //    fy = y / (float)(height - 1);
                //}
                heightCache[x, y] = HeightMap.SampleMap(bounds.Position.X + fx * bounds.Size.X, bounds.Position.Y + fy * bounds.Size.Y, Size);
                //Vertex vertex = new Vertex(new Vector3(bounds.Position.X + fx * bounds.Size.X, heightValue * MaxHeight, bounds.Position.Y + fy * bounds.Size.Y), new Vector2I(x, y));
                //vertices.Add(vertex);
            }
        }

        for (int y = 0; y < height - 1; y++)
        {
            for (int x = 0; x < width - 1; x++)
            {
                float fx = x / (float)(width - 2);
                float fy = y / (float)(height - 2);

                Vector3 v0 = new Vector3(bounds.Position.X + fx * bounds.Size.X, heightCache[x, y] * MaxHeight, bounds.Position.Y + fy * bounds.Size.Y);
                Vector3 v1 = new Vector3(bounds.Position.X + (fx + 1.0f / (width - 2)) * bounds.Size.X, heightCache[x + 1, y] * MaxHeight, bounds.Position.Y + fy * bounds.Size.Y);
                Vector3 v2 = new Vector3(bounds.Position.X + fx * bounds.Size.X, heightCache[x, y + 1] * MaxHeight, bounds.Position.Y + (fy + 1.0f / (height - 2)) * bounds.Size.Y);
                Vector3 v3 = new Vector3(bounds.Position.X + (fx + 1.0f / (width - 2)) * bounds.Size.X, heightCache[x + 1, y + 1] * MaxHeight, bounds.Position.Y + (fy + 1.0f / (height - 2)) * bounds.Size.Y);

                //Get the texture at each vertex
                //int tex0 = texCache[v0.x, v0.z]
                //int tex1 = texCache...

                int tex0 = 0;
                int tex1 = 0;
                int tex2 = 0;   
                int tex3 = 0;

                Vector2 uv0 = new Vector2(v0.X / bounds.Size.X, v0.Z / bounds.Size.Y);
                Vector2 uv1 = new Vector2(v1.X / bounds.Size.X, v1.Z / bounds.Size.Y);
                Vector2 uv2 = new Vector2(v2.X / bounds.Size.X, v2.Z / bounds.Size.Y);
                Vector2 uv3 = new Vector2(v3.X / bounds.Size.X, v3.Z / bounds.Size.Y);

                // First triangle of the quad
                AddTriangle(surfaceTool, v0, v1, v2, new Color(1, 0, 0), new Color(0, 0, 1), new Color(0, 1, 0), 
                    uv0, uv1, uv2, 
                    new Color(tex0, tex1, tex2));

                // Second triangle of the quad
                AddTriangle(surfaceTool, v2, v1, v3, new Color(1, 0, 0), new Color(0, 0, 1), new Color(0, 1, 0), 
                    uv2, uv1, uv3, 
                    new Color(tex2, tex1, tex3));
            }
        }
        surfaceTool.GenerateNormals();
        var mesh = surfaceTool.Commit();
        mesh.SurfaceSetMaterial(0, TerrainMaterial);
        return mesh; 
    }

    private void AddTriangle(SurfaceTool surfaceTool, Vector3 v1, Vector3 v2, Vector3 v3, 
        Color c1, Color c2, Color c3, 
        Vector2 uv0, Vector2 uv1, Vector2 uv2,
        Color texIndexes)
    {
        surfaceTool.SetColor(c1);
        surfaceTool.SetUV(uv0);
        surfaceTool.SetCustom(0, texIndexes);
        surfaceTool.AddVertex(v1);
        
        surfaceTool.SetColor(c2);
        surfaceTool.SetUV(uv1);
        surfaceTool.SetCustom(0, texIndexes);
        surfaceTool.AddVertex(v2);

        surfaceTool.SetColor(c3);
        surfaceTool.SetUV(uv2);
        surfaceTool.SetCustom(0, texIndexes);
        surfaceTool.AddVertex(v3);
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
                //node.MeshInstance.Owner = node;
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
