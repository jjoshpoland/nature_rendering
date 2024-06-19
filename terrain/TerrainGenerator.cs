using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

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
    public float localHeightDetailScale = .01f;
    [Export]
    public float specialHeightScale = 4f;
    [Export]
    public float LODDist = 256f;
    [Export]
    public int LODLevels = 3;
    [Export]
    public Node3D Player;
    [Export]
    public NoiseMap HeightMap;
    [Export]
    public NoiseMap SpecialHeightMap;
    [Export]
    public int GrassDensity = 256;
    [Export]
    public NoiseMap DetailMap;
    [Export]
    public ShaderMaterial TerrainMaterial;
    [Export]
    public int RockFaceTexture;
    [Export]
    public int SandTexture;
    [Export]
    public Image placeholder;
    [Export]
    public Image[] TerrainTextures;
    [Export]
    public Mesh GrassMesh;
    [Export]
    public Material GrassMaterial;
    private Texture2DArray TerrainTextureArray;
    private Image[] HeightMapTextures;
    private Image[] ClimateMapTextures;
    private Texture2DArray heightmaps;
    private Texture2DArray climatemaps;
    private QuadTreeNode quadTreeRoot;
    private Vector3 prevPlayerPos;
    private MultiMesh grassMultiMesh;
    private bool grassMultiMeshGenerated;
    private const int GRASS_BATCH_SIZE = 128;
    

    private int[,] climates = new int[10, 10] { 
        /*dry*/                         /*wet*/
/*cold*/{ 2, 2, 9, 9, 9, 9, 8, 8, 8, 7 }, 
        { 2, 2, 9, 9, 9, 8, 8, 8, 7, 7 },
        { 2, 2, 8, 8, 8, 8, 7, 6, 7, 7 },
        { 2, 2, 8, 4, 3, 3, 6, 6, 6, 6 },
        { 1, 5, 5, 4, 3, 6, 6, 6, 6, 6 },
        { 1, 5, 5, 4, 3, 6, 6, 6, 6, 6 },
        { 1, 1, 5, 4, 3, 3, 6, 6, 6, 6 },
        { 1, 1, 5, 4, 3, 3, 6, 6, 6, 6 },
        { 1, 1, 1, 5, 5, 5, 5, 6, 0, 0 },
/*hot*/ { 1, 1, 1, 1, 5, 5, 6, 6, 0, 0 } };
    
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
            Generate();
            UpdateLod(Player.GlobalPosition);
            needsRegenerate = false;
        }
        if (!needsUpdate) return;
        
        if (quadTreeRoot == null) {
            Generate();
        }
        UpdateLod(Player.GlobalPosition);
        if (Player != null && prevPlayerPos != Player.GlobalPosition)
        {
            
            prevPlayerPos = Player.GlobalPosition;
        }
	}

    public void Generate()
    {
        grassMultiMeshGenerated = false;
        TerrainTextureArray = new Texture2DArray();
        TerrainTextureArray.CreateFromImages(new Godot.Collections.Array<Image>(TerrainTextures));
        TerrainMaterial.SetShaderParameter("texture_array", TerrainTextureArray);
        TerrainMaterial.SetShaderParameter("rock_face_texture", RockFaceTexture);
        TerrainMaterial.SetShaderParameter("sand_texture", SandTexture);
        int nodesX = Mathf.CeilToInt(Size / baseResolution);
        int nodesY = Mathf.CeilToInt(Size / baseResolution);
        HeightMapTextures = new Image[nodesX * nodesY];
        ClimateMapTextures = new Image[nodesX * nodesY];


        for ( int i = 0; i < nodesX * nodesY; i++ ) 
        {
            HeightMapTextures[i] = placeholder;
            ClimateMapTextures[i] = placeholder;
        }

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
            if(child is MeshInstance3D || child is QuadTreeNode || child is MultiMeshInstance3D)
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


    private void InitializeGrassMultiMesh(QuadTreeNode node)
    {
        
        
        node.GrassMultiMeshInstance.MaterialOverride = GrassMaterial;
        node.GrassMultiMeshInstance.VisibilityRangeFadeMode = GeometryInstance3D.VisibilityRangeFadeModeEnum.Self;

    }

    private Mesh GenerateLodMesh(Rect2 bounds, int lod, QuadTreeNode node)
    {
        int resolution = (int)Math.Pow(2, lod + 1);  // Ensure resolution is at least 2x2
        return GenerateMesh(bounds, resolution, node);
    }

    private Mesh GenerateMesh(Rect2 bounds, int resolution, QuadTreeNode node)
    {
        var surfaceTool = new SurfaceTool();
        var dataTool = new MeshDataTool();
        List<Vertex> vertices = new List<Vertex>();
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
        surfaceTool.SetCustomFormat(0, SurfaceTool.CustomFormat.RgbaFloat);
        List<Transform3D> grassTransforms = new List<Transform3D>();


        int width = baseResolution / resolution;
        int height = baseResolution / resolution;

        float[,] heightCache = new float[width, height];
        float[,] heatCache = new float[width, height];
        float[,] moistureCache = new float[width, height];
        if (resolution == 2)
        {
            node.climateMap = Image.Create(width, height, false, Image.Format.Rgba8);
            node.heightMap = Image.Create(width, height, false, Image.Format.Rgba8);
        }
        

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

                Color env = HeightMap.SampleMap(bounds.Position.X + fx * bounds.Size.X, bounds.Position.Y + fy * bounds.Size.Y, Size);
                Color heightAdd = SpecialHeightMap.SampleMap(bounds.Position.X + fx * bounds.Size.X, bounds.Position.Y + fy * bounds.Size.Y, Size);
                heightCache[x, y] = (env.R + (heightAdd.R * specialHeightScale)) * (.75f - heightAdd.G);
                heatCache[x,y] = env.G * Mathf.Clamp(2f - heightCache[x,y], .25f, 1f);
                moistureCache[x,y] = (env.B * 0.75f) + (heightAdd.G * 0.25f);

                if (resolution == 2)
                {
                    node.heightMap.SetPixel(x, y, new Color(heightCache[x, y] / MaxHeight, heatCache[x,y], moistureCache[x,y]));
                }
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


                
                int tex0 = climates[Mathf.RoundToInt(heatCache[x, y] * 9),Mathf.RoundToInt(moistureCache[x, y] * 9)];
                
                int tex1 = climates[Mathf.RoundToInt(heatCache[x + 1, y] * 9), Mathf.RoundToInt(moistureCache[x + 1, y] * 9)];
                
                int tex2 = climates[Mathf.RoundToInt(heatCache[x, y + 1] * 9), Mathf.RoundToInt(moistureCache[x, y + 1] * 9)];
                
                int tex3 = climates[Mathf.RoundToInt(heatCache[x + 1, y + 1] * 9), Mathf.RoundToInt(moistureCache[x + 1, y + 1] * 9)];
                
                if(resolution == 2)
                {
                    node.climateMap.SetPixel(x, y, new Color((float)tex0 / 255f, 0, 0));
                    node.climateMap.SetPixel(x + 1, y, new Color((float)tex1 / 255f, 0, 0));
                    node.climateMap.SetPixel(x, y + 1, new Color((float)tex2 / 255f, 0, 0));
                    node.climateMap.SetPixel(x + 1, y + 1, new Color((float)tex3 / 255f, 0, 0));
                }

                Vector2 uv0 = new Vector2(v0.X / bounds.Size.X, v0.Z / bounds.Size.Y);
                Vector2 uv1 = new Vector2(v1.X / bounds.Size.X, v1.Z / bounds.Size.Y);
                Vector2 uv2 = new Vector2(v2.X / bounds.Size.X, v2.Z / bounds.Size.Y);
                Vector2 uv3 = new Vector2(v3.X / bounds.Size.X, v3.Z / bounds.Size.Y);

                // First triangle of the quad
                AddTriangle(surfaceTool, v0, v1, v2, new Color(1, 0, 0), new Color(0, 1, 0), new Color(0, 0, 1), 
                    uv0, uv1, uv2, 
                    new Color(tex0, tex1, tex2));

                // Second triangle of the quad
                AddTriangle(surfaceTool, v2, v1, v3, new Color(1, 0, 0), new Color(0, 1, 0), new Color(0, 0, 1), 
                    uv2, uv1, uv3, 
                    new Color(tex2, tex1, tex3));
            }
        }

        //if (resolution == 2)
        //{
        //    ScatterGrass(ref grassTransforms, climateMap, bounds);
        //}
        if (resolution == 2)
        {
            int nodeX = (int)(bounds.Position.X) / baseResolution;
            int nodeY = ((int)(bounds.Position.Y) / baseResolution);
            GD.Print("size / node size: " + Size / bounds.Size.X + ", vs world size / base res: " + Size / baseResolution);
            GD.Print("x: " + nodeX + ", y: " + nodeY);
            GD.Print("total map array x dimension length: " + HeightMapTextures.Length / (Size / baseResolution));
            HeightMapTextures[(nodeY * (Size / baseResolution)) + nodeX] = node.heightMap;
            ClimateMapTextures[(nodeY * (Size / baseResolution)) + nodeX] = node.climateMap;
            heightmaps = new Texture2DArray();
            heightmaps.CreateFromImages(new Godot.Collections.Array<Image>(HeightMapTextures));
            node.GrassMultiMeshInstance.SetInstanceShaderParameter("map_index", (nodeY * (Size / baseResolution)) + nodeX);
            RenderingServer.GlobalShaderParameterSet("height_maps", heightmaps);
        }

        //UpdateGrassMultiMesh(grassTransforms);

        surfaceTool.GenerateNormals();
        var mesh = surfaceTool.Commit();
        mesh.SurfaceSetMaterial(0, TerrainMaterial);
        return mesh; 
    }

    private void ScatterGrass(Rect2 bounds)
    {
        grassMultiMesh = new MultiMesh();
        grassMultiMesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
        
        List<Transform3D> grassTransforms = new List<Transform3D>();
        int grassResolution = GrassDensity; // Higher resolution for grass scattering
        for (int y = 0; y < grassResolution; y++)
        {
            for (int x = 0; x < grassResolution; x++)
            {
                float fx = x / (float)(grassResolution - 1);
                float fy = y / (float)(grassResolution - 1);
                float sampleX = fx * bounds.Size.X;
                float sampleY = fy * bounds.Size.Y;

                Color grassProbability = DetailMap.SampleMap(sampleX, sampleY, Size);
                Vector3 grassPosition = new Vector3(sampleX, 0, sampleY);
                Transform3D grassTransform = new Transform3D(Basis.Identity, grassPosition);
                //grassTransform = grassTransform.RotatedLocal(new Vector3(0, 1f, 0), Mathf.DegToRad(grassProbability.G * 180));
                grassTransforms.Add(grassTransform);

            }
        }
        grassMultiMesh.Mesh = GrassMesh;
        grassMultiMesh.InstanceCount = grassTransforms.Count;
        grassMultiMesh.VisibleInstanceCount = grassTransforms.Count;

        for (int j = 0; j < grassTransforms.Count; j++)
        {
            grassMultiMesh.SetInstanceTransform(j, grassTransforms[j]);
        }

    }


    private float SampleHeightmap(float sampleX, float sampleY, int size) 
    {
        Color env = HeightMap.SampleMap(sampleX, sampleY, size);
        Color heightAdd = SpecialHeightMap.SampleMap(sampleX, sampleY, size);
        env.R = (env.R + (heightAdd.R * specialHeightScale)) * (.75f - heightAdd.G);
        return env.R;
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
            var distance = cameraPosition.DistanceTo(new Vector3(node.Bounds.Position.X + (node.Bounds.Size.X / 2.0f), cameraPosition.Y, node.Bounds.Position.Y + (node.Bounds.Size.Y / 2.0f)));
            int lodLevel = (int)(distance / LODDist);
            lodLevel = Mathf.Clamp(lodLevel, 0, node.LodMeshes.Count - 1);
            // Generate the LOD mesh if it hasn't been generated yet
            if (!node.LodGenerated[lodLevel])
            {
                node.LodMeshes[lodLevel] = GenerateLodMesh(node.Bounds, lodLevel, node);
                if (lodLevel == 0)
                {
                    
                    GD.Print("doing grass");
                    if(!grassMultiMeshGenerated)
                    {
                        
                        ScatterGrass(node.Bounds);
                        grassMultiMeshGenerated = true;
                    }
                    
                    InitializeGrassMultiMesh(node);
                    node.GrassMultiMeshInstance.Multimesh = grassMultiMesh;
                    node.GrassMultiMeshInstance.Transform = new Transform3D(Basis.Identity, new Vector3(-(node.Bounds.Size.X / 2.0f), 0, -(node.Bounds.Size.Y / 2.0f)));
                    //node.GrassMultiMeshInstance.CustomAabb = new Aabb(new Vector3(node.Bounds.Position.X + (node.Bounds.Size.X / 2.0f), 0, node.Bounds.Position.Y + (node.Bounds.Size.Y / 2.0f)), new Vector3(node.Bounds.Size.X, MaxHeight, node.Bounds.Size.Y));
                    
                    node.AddChild(node.GrassMultiMeshInstance);

                    node.GrassMultiMeshInstance.Owner = node;
                    
                }

                node.LodGenerated[lodLevel] = true;
                node.MeshInstance.Mesh = node.LodMeshes[lodLevel];
                if(lodLevel == 0)
                {
                    
                    node.MeshInstance.CreateTrimeshCollision();
                    
                }
                node.GrassMultiMeshInstance.Visible = lodLevel == 0;

            }
            else if (node.MeshInstance.Mesh != node.LodMeshes[lodLevel])
            {
                node.MeshInstance.Mesh = node.LodMeshes[lodLevel];
                node.GrassMultiMeshInstance.Visible = lodLevel == 0;
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

    private enum Climate
    {
        None = 0, Sand = 1, Rock = 2, Grass = 3, Grass2 = 4, DryGrass = 5, Forest = 6, Taiga = 7, Tundra = 8, Snow = 9
    }
}
