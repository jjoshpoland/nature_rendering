using Godot;
using System;
using System.Collections.Generic;
using System.Data.Common;

[Tool]
public partial class world_map : Node
{

	[Export]
	public bool NeedsUpdate;
    [Export]
    public int Size = 256;
    [Export]
    public Image map;
	[Export]
	public TerrainGenerator Terrain;
    [Export]
    public TerrainPathfinder TerrainPathfinder;
    public Color[,] mapData;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(NeedsUpdate)
		{
            UpdateMap();
			NeedsUpdate = false;
		}
	}

    void UpdateMap()
    {
        map = Image.Create(Size, Size, false, Image.Format.Rgbaf);
        GD.Print(map.GetWidth());
        if (Terrain != null)
        {
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    map.SetPixel(x, y, Colors.Black);
                }
            }
        }
        GeneratePOILocations();
    }

    void GeneratePOILocations()
    {
        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Randomize();
        Vector2I poiA = new Vector2I(rng.RandiRange(0, Terrain.fullMap.GetLength(0) - 1), rng.RandiRange(0, Terrain.fullMap.GetLength(1) - 1));
        Vector2I poiB = new Vector2I(rng.RandiRange(0, Terrain.fullMap.GetLength(0) - 1), rng.RandiRange(0, Terrain.fullMap.GetLength(1) - 1));

        List<Vector2I> path = TerrainPathfinder.FindPath(Terrain.fullMap, poiA, poiB);

        if (path != null)
        { 
            if (path.Count == 0)
            {
                GD.PrintErr("could not find path between " + poiA + " and " + poiB);
            }
            float ratio = map.GetHeight() / (Terrain.Size / 2f); //assuming square. need two ratios if not
            foreach(Vector2I point in path)
            {
                map.SetPixel(Mathf.RoundToInt( point.X * ratio), Mathf.RoundToInt(point.Y * ratio), Colors.Yellow);
                GD.Print(point);
            }
        }
        else 
        {
            GD.PrintErr("could not find path between " + poiA + " and " + poiB);
        }
    }

    void GenerateCivilization()
    {

    }

    public Color SampleMap(float x, float y, int worldSize)
    {
        int srcWidth = mapData.GetLength(0);
        int srcHeight = mapData.GetLength(1);

        float gx = (x / worldSize) * (srcWidth - 1);
        float gy = (y / worldSize) * (srcHeight - 1);

        int gxi = (int)gx;
        int gyi = (int)gy;

        float r = 0.0f;
        float g = 0.0f;
        float b = 0.0f;

        for (int m = -1; m < 3; m++)
        {
            for (int n = -1; n < 3; n++)
            {
                int p = Mathf.Clamp(gxi + m, 0, srcWidth - 1);
                int q = Mathf.Clamp(gyi + n, 0, srcHeight - 1);

                float rp = mapData[p, q].R;
                float gp = mapData[p, q].G;
                float bp = mapData[p, q].B;

                float wx = BicubicKernel(gx - (gxi + m));
                float wy = BicubicKernel(gy - (gyi + n));

                r += rp * wx * wy;
                g += gp * wx * wy;
                b += bp * wx * wy;
            }
        }


        return new Color(r, g, b);
    }

    public static float BicubicKernel(float x)
    {
        float a = -0.5f;
        x = Mathf.Abs(x);
        if (x <= 1)
        {
            return (a + 2) * Mathf.Pow(x, 3) - (a + 3) * Mathf.Pow(x, 2) + 1;
        }
        else if (x < 2)
        {
            return a * Mathf.Pow(x, 3) - 5 * a * Mathf.Pow(x, 2) + 8 * a * x - 4 * a;
        }
        else
        {
            return 0;
        }
    }
}
