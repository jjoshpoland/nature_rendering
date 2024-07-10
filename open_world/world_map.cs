using Godot;
using System;
using System.Collections.Generic;
using System.Data.Common;

[Tool]
public partial class world_map : MapPreprocessor
{

	[Export]
	public bool NeedsUpdate;
    [Export]
    public TerrainPathfinder TerrainPathfinder;


    protected override void DoUpdate(ref MapPoint[,] mapPoints)
    {
        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Randomize();
        Vector2I poiA = new Vector2I(rng.RandiRange(0, mapPoints.GetLength(0) - 1), rng.RandiRange(0, mapPoints.GetLength(1) - 1));
        Vector2I poiB = new Vector2I(rng.RandiRange(0, mapPoints.GetLength(0) - 1), rng.RandiRange(0, mapPoints.GetLength(1) - 1));
        float ratio = map.GetHeight() / (mapPoints.GetLength(0) / 2f); //assuming square. need two ratios if not
        List<Vector2I> path = TerrainPathfinder.FindPath(mapPoints, poiA, poiB);

        if (path != null)
        { 
            if (path.Count == 0)
            {
                GD.PrintErr("could not find path between " + poiA + " and " + poiB);
            }
            
            foreach(Vector2I point in path)
            {
                UpdateImage(point.X, point.Y, ratio, Colors.Yellow);
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

}
