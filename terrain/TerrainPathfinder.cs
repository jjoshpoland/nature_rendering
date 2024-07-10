using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

[GlobalClass]
[Tool]
public partial class TerrainPathfinder : Node
{
	int searchPhase = 0;
    public Vector2I[] Neighbors =
    {
        new Vector2I(0, 1),
        new Vector2I(1, 1),
        new Vector2I(1, 0),
        new Vector2I(1, -1),
        new Vector2I(0, -1),
        new Vector2I(-1, -1),
        new Vector2I(-1, 0),
        new Vector2I(-1, 1)
    };
	public List<Vector2I> FindPath(MapPoint[,] mapData, Vector2I origin, Vector2I destination)
	{
        searchPhase += 2;
        PathDataPriorityQueue openList = new PathDataPriorityQueue();
        Dictionary<Vector2I, PathData> knownPoints = new Dictionary<Vector2I, PathData>();
        List<Vector2I> path = new List<Vector2I>();

        PathData end = null;
        PathData start = new PathData();

        int tries = 0;
        start.distance = 0;
        start.SearchPhase = searchPhase;
        start.pos = origin;
        knownPoints.Add(origin, start);
        openList.Enqueue(start);

        while(openList.Count > 0 && tries < 100000)
        {
            PathData q = openList.Dequeue();
            q.SearchPhase += 1;

            if(q == null)
            {
                GD.Print("null path position found in queue");
                continue;
            }

            if(q.pos == destination)
            {
                if (knownPoints.TryGetValue(q.pos, out PathData current))
                {
                    end = current;
                    GD.Print("end point dequeued");
                }
                goto Exit;
            }

            for (int i = 0; i < 8; i++)
            {
                PathData neighbor;
                Vector2I neighborPos = q.pos + Neighbors[i];
                // Check out of bounds
                if (neighborPos.X < 0 || neighborPos.Y < 0 || neighborPos.X >= mapData.GetLength(0) || neighborPos.Y >= mapData.GetLength(1) )
                {
                    continue;
                }
                
                // if couldnt assign a known PathData to neighbor, make a new one and cache it
                if (knownPoints.TryGetValue(neighborPos, out PathData knownNeighbor))
                {
                    neighbor = knownNeighbor;
                }
                else
                {
                    neighbor = new PathData();
                    neighbor.pos = neighborPos;
                    knownPoints.Add(neighborPos, neighbor);
                }

                if (neighbor.SearchPhase > searchPhase)
                {
                    continue;
                }

                float heightDifference = Mathf.Abs( mapData[q.pos.X, q.pos.Y].Environment.R - mapData[neighbor.pos.X, neighbor.pos.Y].Environment.R);
                int distance = q.distance + 1 + (Mathf.RoundToInt(heightDifference) * 50);
                if (neighbor.SearchPhase < searchPhase)
                {
                    neighbor.SearchPhase = searchPhase;
                    neighbor.distance = distance;
                    neighbor.parent = q;
                    neighbor.heuristic = Mathf.RoundToInt((neighbor.pos - destination).Length());
                    openList.Enqueue(neighbor);
                }
                else if (distance < neighbor.distance)
                {
                    int oldPriority = neighbor.Priority;
                    neighbor.distance = distance;
                    neighbor.parent = q;
                    openList.Change(neighbor, oldPriority);
                }
            }

            tries++;
            //GD.Print(tries);
        }

        Exit:;

        if(end != null)
        {
            PathData current = end;
            while(current != start)
            {
                path.Add(current.pos);
                current = current.parent;
                if(current.parent == null)
                {
                    GD.PrintErr("PathData item had no parent, exiting");
                    break;
                }
            }

            path.Add(origin);
            path.Reverse();
        }

        if (tries >= 100000)
        {
            GD.Print("exceeded path try limit");
        }

        return path;
    }
}

/// <summary>
/// Data structure for holding A star pathfinding variables
/// </summary>
public class PathData
{
    public int SearchPhase;
    public Vector2I pos;
    /// <summary>
    /// Search priority
    /// </summary>
    int priority;
    public int Priority
    {
        get
        {
            return distance + heuristic;
        }

    }
    public PathData parent;
    /// <summary>
    /// Reference for a linked list
    /// </summary>
    public PathData nextEqualPriority;
    /// <summary>
    /// Distance
    /// </summary>
    public int distance;
    /// <summary>
    /// Heuristic
    /// </summary>
    public int heuristic;
}