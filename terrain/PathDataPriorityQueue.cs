using Godot;
using System;
using System.Collections.Generic;

public class PathDataPriorityQueue
{
    List<PathData> list = new List<PathData>();

    int count = 0;
    int minimumPriority = int.MaxValue;

    public int Count => count;

    public void Enqueue(PathData data)
    {
        count += 1;
        int priority = data.Priority;
        if (priority < minimumPriority)
        {
            minimumPriority = priority;
        }
        while (priority >= list.Count)
        {
            list.Add(null);
        }
        data.nextEqualPriority = list[priority];
        list[priority] = data;
    }

    public PathData Dequeue()
    {
        count -= 1;
        for (; minimumPriority < list.Count; minimumPriority++)
        {
            PathData tile = list[minimumPriority];
            if (tile != null)
            {
                list[minimumPriority] = tile.nextEqualPriority;
                return tile;
            }
        }
        return null;
    }

    public void Change(PathData tile, int oldPriority)
    {
        PathData current = list[oldPriority];
        PathData next = tile.nextEqualPriority;
        if (current == tile)
        {
            list[oldPriority] = next;
        }
        else
        {

            while (next != tile)
            {
                current = next; //go to next
                next = current.nextEqualPriority; //set up the one after next for the comparison
            }


            current.nextEqualPriority = tile.nextEqualPriority;
        }

        Enqueue(tile);
        count -= 1;
    }

    public void Clear()
    {
        list.Clear();
        count = 0;
        minimumPriority = int.MaxValue;
    }
}
